# CLAUDE.md — Identity service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Buyer phone+OTP authentication, acting as a lightweight OIDC token issuer so
the 5 existing services (Catalog, Inventory, Ordering, Payments, Ticketing)
validate its tokens through their existing JWT validation code path with
**zero code changes**. Bounded context: **Identity** — the buyer-identity
half of [ADR-0016](../../docs/adr/0016-buyer-identity-and-notifications.md).
Organizer auth (Entra External ID) is separately deferred and not this
service's concern.

## Owns

- **Data store:** PostgreSQL `identity` DB (this service only) —
  `PhoneVerification` (OTP challenges), `BuyerAccount` (durable buyer
  identity), `SigningKey` (persisted RSA signing key).
- **Public API:** `POST /v1/otp/request`, `POST /v1/otp/verify`
  (gateway-routed, buyer-facing, both `.AllowAnonymous()`);
  `GET /.well-known/openid-configuration`, `GET /.well-known/jwks.json`
  (public, but fetched server-to-server by the 5 existing services' own
  `Authority`-based discovery — **not** gateway-routed, never called by a
  browser).
- **Events published:** none.
- **Events consumed:** none — this is a purely synchronous request/verify
  flow. Its one Dapr use is an *outbound* service-invocation call (to
  Communication, for OTP SMS delivery), not a pub/sub subscription.

## Design notes

- **Access-token-only — no refresh tokens.** Explicit decision. Identity
  mints a single JWT (~7 day lifetime, `Identity:Jwt:AccessTokenLifetimeDays`)
  at OTP verification; a buyer re-verifies via OTP once it expires. No
  refresh-token entity, no rotation, no revoke endpoint.
- **Claims: `sub`, `iss`, `aud`, `iat`/`nbf`/`exp`, `role: "buyer"` — never
  `tenant_id`.** Per [ADR-0022](../../docs/adr/0022-buyer-tenant-derivation.md),
  a buyer isn't tenant-scoped. `POST /v1/holds` and `POST /v1/checkout`
  were already reworked to derive tenant from the resource instead of
  requiring this claim, specifically so a token like this one works.
- **`AuthenticationExtensions.cs`'s Production branch already does
  everything needed** — plain `AddJwtBearer` + `options.Authority`/`Audience`,
  which triggers ASP.NET Core's automatic OIDC discovery. Identity's whole
  job is to be a correct RS256 issuer at that URL. The Dev HS256 path
  (`Jwt:DevSigningKey`) is untouched and unrelated — Identity does not plug
  into it.
- **OTP hashing: keyed HMAC-SHA256** (`Identity:Otp:HmacKey`), not
  bcrypt/Argon2/PBKDF2 — a 6-digit code bounded by a 5-minute TTL and a
  5-attempt lockout (`PhoneVerification.MaxAttempts`) gets no real benefit
  from a slow KDF; the only requirement is "don't store the raw code,"
  which a keyed MAC already satisfies.
- **Signing-key persistence: generated once, persisted in Postgres,
  config-gated for a future Key Vault swap that is NOT implemented yet.**
  `Identity:Jwt:SigningKeySource` mirrors Communication's config-gate shape
  (raw `IConfiguration` indexer, never `IsDevelopment()`) but **fails fast**
  on `"KeyVault"` rather than silently falling back — unlike a vendor gate
  where "unset" safely degrades, a wrong key-custody model for token
  signing should never be a silent degrade. `PersistedRsaSigningKeyProvider`
  caches on an **instance field**, not `static` — it's registered
  `AddSingleton`, so an instance field already gives the "once per process"
  guarantee; every service in this repo deploys `replicas: 1`, so this is
  safe without a real insert-if-not-exists retry pattern (needed only if
  replica count ever increases).
- **Hand-rolled discovery/JWKS, not a framework.** No IdentityServer/
  OpenIddict/Duende — just enough to satisfy `AddJwtBearer`'s automatic
  discovery (ADR-0009's lean-over-framework precedent). Both DTOs
  (`OidcDiscoveryDocument`, `JsonWebKeyDto`/`JsonWebKeySetDto`) use explicit
  `[JsonPropertyName]` on every property — OIDC/JWK field names are
  spec-fixed (`jwks_uri`, `kty`, `n`, `e`, …), and relying on the service-wide
  camelCase policy would silently break if that policy ever changed.
- **OTP delivery reuses Communication unchanged.** `DaprOtpSender` calls
  Communication's existing `POST /v1/notifications/send` (`Channel: Sms`,
  `Recipient` = E.164 phone, `Body` = the OTP text — SMS has no
  template-key mechanism, so Identity composes the message itself).
  `TenantId` on that call is a documented platform-pseudo-tenant placeholder
  (delivery-log reporting only on Communication's side, never
  authorization) — an OTP send has no natural buyer tenant.
- **No transactional outbox.** Like Communication, Identity never publishes
  an integration event.
- **No background reaper for expired challenges.** Unlike Inventory's
  `ExpiredHoldReaper` (needed because something must *react* to an expiry —
  release the seat, emit an event), an expired `PhoneVerification` has no
  side effect to react to: `VerifyOtpHandler` lazily marks it expired on
  read. Don't add one without a real need (storage hygiene is a separate,
  deferred concern).
- **Backend-only.** The frontend's `AuthContext` swap from dev-login to real
  Identity-based buyer login is a separate, later step — not built here.

## Structure

`Identity.Api` (host + endpoints — no Dapr pub/sub, so no `Dapr.AspNetCore`
reference) · `Identity.Application` (`Otp/` request+verify slices,
`Abstractions/` ports) · `Identity.Domain` (`PhoneVerification`,
`BuyerAccount`, `SigningKey` + invariants) · `Identity.Infrastructure`
(EF Core + Postgres, `Otp/` — HMAC hasher + Dapr sender, `Signing/` — RSA
key persistence + JWT issuance — no outbox) · `tests/Identity.Tests`.

## Local run

```bash
dapr run --app-id identity \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/identity/Identity.Api
```

Run Communication too (same Dapr setup) so `POST /v1/otp/request` can reach
it by app-id. With no SMS vendor configured on Communication, every OTP send
uses its dev/logging sender — check the console output or the
`communication.delivery_log` table to read the code during local testing.

**Known local-dev limitation**: every one of the 5 existing services'
`appsettings.Development.json` still sets `Jwt:DevSigningKey`, so they
validate via the HS256 dev branch, not Identity's RS256/`Authority` branch,
by default. Exercising a real Identity token against a real service locally
needs a manual opt-in — temporarily unset that service's `Jwt:DevSigningKey`
and point its `Jwt:Authority` at `http://localhost:5087` with
`RequireHttpsMetadata:false`.

## Do not

- Add refresh tokens/rotation without a new ADR decision — this pass is
  access-token-only, deliberately.
- Let the discovery/JWKS DTOs drift onto the global camelCase JSON policy —
  every property needs an explicit `[JsonPropertyName]`.
- Treat `Identity:Jwt:SigningKeySource=KeyVault` as implemented — it fails
  fast by design; there is no real provider behind it yet.
- Store a raw OTP code anywhere — only its keyed-HMAC hash.
- Read another service's database (resolve OTP delivery via Communication's
  port, over Dapr — never call a vendor SDK directly from here).
- Add a package version to a `.csproj` (use `Directory.Packages.props`).
