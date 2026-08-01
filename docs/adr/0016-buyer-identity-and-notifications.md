# ADR-0016 — Buyer identity & notifications: Communication and Identity services

- **Status:** Accepted
- **Date:** 2026-07-28 (Communication); extended 2026-08-01 (Identity)

## Context

This filename/number was already forward-referenced from
`gateways/EventPlatform.Gateway/README.md`, `gateways/EventPlatform.Gateway/CLAUDE.md`,
and `docs/adr/0015-frontend-react-vite-antd-and-bff-gateway.md` as the future
record for two related-but-separate concerns: real buyer authentication
(OTP-based Identity) and outbound notifications. The notifications half —
**Communication** — was built first (see "Decision (Communication)" below).
Buyer Identity was originally **Deferred**, per that section, and gets its
own decision content in "Decision (Identity)" below, per this ADR's own
stated intent to extend rather than supersede itself when Identity landed.
Organizer auth via Entra External ID remains separately deferred (build-order
step 9) — untouched by either half of this ADR.

`docs/design/hld.md`'s pre-implementation sketch calls this component
"Notification." **Communication** is the as-built name — see
`docs/data-flow-and-service-boundaries.md` for that reconciliation; `hld.md`
itself is left untouched, consistent with how none of the other five
services' entries there were retroactively renamed either.

## Decision (Communication)

Build `services/communication/` as a sixth backend service owning every
outbound Email/SMS/WhatsApp message the platform sends.

- **Dual-vendor, config-gated adapters, one port per channel.** `IEmailSender`
  / `ISmsSender` / `IWhatsAppSender` each support Azure Communication
  Services (ACS) and/or Twilio, selected per channel via configuration
  (`Communication:{Email,Sms,WhatsApp}:Provider`), falling back to a
  dev/logging sender when nothing is configured — the same config-key-presence
  gate already proven in Payments (`IPaymentGateway` →
  `SimulatedPaymentGateway`/`StripePaymentGateway`). Three narrow ports
  rather than one unified interface, because Twilio has no first-party email
  product under its `Twilio` package (that's SendGrid, a different SDK) —
  forcing Email's `subject`/`htmlBody` into a shared shape with SMS/WhatsApp's
  plain `body` would help nothing. This also lets ACS and Twilio run
  simultaneously for different channels (e.g. ACS for Email, Twilio for
  WhatsApp while its sandbox is easier to get approved).
- **A single send endpoint**, `POST /v1/notifications/send`, internal
  (Dapr service invocation only, never gateway-routed), taking a channel
  discriminator plus either a template key (Email) or a raw body (SMS/WhatsApp).
- **Email is template-driven.** Templates are embedded resource files
  shipped with the service (`Communication.Infrastructure/Templates/Resources/`),
  not database rows — nothing in scope needs organizer-editable templates at
  runtime, and this avoids inventing a CRUD/admin-UI/seed-data mechanism this
  repo has no precedent for. Still port-shaped (`ITemplateStore`), so a
  future DB-backed store is a new adapter, not a caller change. Placeholder
  substitution uses Scriban (BSD-2-Clause) rather than hand-rolled string
  replace — it correctly handles a missing placeholder and HTML-escapes
  values by default (relevant since a placeholder could carry user-supplied
  text), and leaves room to grow (loops/conditionals) without a rewrite.
- **No transactional outbox.** Communication never publishes an integration
  event — it's a terminal consumer and a synchronous responder — so its
  `CommunicationDbContext` doesn't implement the outbox contract and its
  Infrastructure project has no reference to `EventPlatform.Messaging`.
  This is a deliberate deviation from every other service's scaffold.
- **`OrderConfirmed`/`TicketIssued` subscribers are wired for redelivery-safety
  now; real delivery is deferred.** Neither event carries an email/phone —
  only a bare `UserId` — and no Identity/user-profile service exists yet to
  resolve one. Both subscribers are fully wired (topics registered, a new
  `ProcessedNotificationEvent` dedup ledger checked-then-recorded in the same
  transaction as the delivery-log write) but resolve the recipient through a
  new port, `IRecipientResolver`, whose only implementation today
  (`UnavailableRecipientResolver`) always returns "unknown." An honest
  `Skipped` row is written to the delivery log instead of failing, crashing,
  or silently dropping the event. This becomes a one-adapter swap — not a
  change to the port or its callers — once Identity (or any user-profile
  source) exists.

## Decision (Identity)

Build `services/identity/` as a seventh backend service: buyer phone+OTP
authentication, acting as a lightweight OIDC token issuer so the 5 existing
services validate its tokens through their existing JWT validation code path
(`building-blocks/EventPlatform.Hosting/AuthenticationExtensions.cs`) with
**zero code changes**. That code already does plain `AddJwtBearer` +
`options.Authority`/`options.Audience` in Production, which triggers ASP.NET
Core's automatic OIDC discovery (`{Authority}/.well-known/openid-configuration`
→ `jwks_uri`) — Identity's whole job is to be a standards-compliant RS256
issuer at that URL.

- **Access-token-only — no refresh tokens.** Confirmed via explicit user
  decision. Identity mints a single JWT (~7 day lifetime) at OTP
  verification; a buyer re-verifies via OTP once it expires. This
  deliberately skips an entire class of session-management scope (refresh
  rotation, reuse detection, a revoke endpoint) that a full session system
  would need — a fresh, low-friction OTP re-verify is an acceptable
  trade-off for a "lightweight OIDC issuer."
- **Claims: `sub`, `iss`, `aud`, `iat`/`nbf`/`exp`, `role: "buyer"` — no
  `tenant_id`.** Per ADR-0022, a buyer isn't tenant-scoped (they transact
  with many organizers over time), and the two endpoints that used to
  require a `tenant_id` claim (`POST /v1/holds`, `POST /v1/checkout`) were
  already reworked specifically so a token like this one works. `role` is
  parity with dev-login's claim shape only — not enforced by any
  authorization policy today.
- **OTP hashing: keyed HMAC-SHA256, not bcrypt/Argon2/PBKDF2.** A 6-digit
  code bounded by a 5-minute TTL and a 5-attempt lockout (`PhoneVerification.MaxAttempts`)
  gets no meaningful extra resistance from a slow password KDF — the only
  real requirement is "don't store the raw code," which a keyed MAC already
  satisfies (the HMAC key lives in config, `Identity:Otp:HmacKey`, never in
  the database). A slow KDF here would only add latency to every verify
  request for no security benefit.
- **Signing-key persistence: a generated RSA key, persisted in Postgres,
  config-gated for a future Key Vault swap — but the Key Vault path is not
  implemented this pass.** `PersistedRsaSigningKeyProvider` generates a
  2048-bit RSA key once (`SigningKey.Generate()`) and persists it (PKCS8,
  base64) in the `identity` database, so a restart doesn't invalidate every
  outstanding buyer session — every service in this repo deploys
  `replicas: 1`, so a process-instance-level generate-once-under-a-lock is
  correct (not `static` — the type is registered `AddSingleton`, so an
  instance field already gives the "once per process" guarantee). The DI
  gate (`Identity:Jwt:SigningKeySource`) mirrors Communication's config-read
  pattern in shape (raw `IConfiguration` indexer, never `IsDevelopment()`)
  but **not** in fail-open behavior: requesting `"KeyVault"` throws at
  startup rather than silently falling back to the dev-persisted path — a
  wrong key-custody model for token signing should never be a silent
  degrade, unlike Communication's vendor gate where "unset" safely means
  "log instead of send." A real `KeyVaultSigningKeyProvider` is future work,
  slotting into the same interface with no caller change.
- **Hand-rolled OIDC surface, not a framework.** `GET /.well-known/openid-configuration`
  and `GET /.well-known/jwks.json` are minimal, purpose-built endpoints
  (`System.IdentityModel.Tokens.Jwt`/`Microsoft.IdentityModel.Tokens` — new
  `Directory.Packages.props` pins, MIT-licensed) rather than adopting
  IdentityServer/OpenIddict/Duende — consistent with ADR-0009's
  no-MediatR-on-the-hot-path lean-implementation precedent. Both DTOs use
  explicit `[JsonPropertyName]` on every property rather than relying on the
  service-wide camelCase JSON policy — OIDC/JWK field names are spec-fixed
  (`jwks_uri`, `kty`, `n`, `e`, …), and a coincidental camelCase match would
  silently break every consuming service's discovery fetch if that global
  policy ever changed. Neither endpoint is gateway-routed — both are
  fetched server-to-server by the 5 services' own `Authority`-based
  discovery (a plain direct HTTP(S) GET, not a Dapr-invocation call — in a
  deployed environment `Jwt:Authority` on each service must be a directly
  reachable URL, e.g. in-cluster K8s Service DNS, not
  `http://.../v1.0/invoke/...`).
- **OTP delivery reuses Communication, unchanged.** `POST /v1/otp/request`
  calls Communication's existing `POST /v1/notifications/send` via Dapr
  service invocation (`Channel: Sms`, `Recipient` = E.164 phone, `Body` =
  the OTP text Identity composes itself — SMS has no template-key mechanism).
  `TenantId` on that call uses a documented placeholder platform-pseudo-tenant
  GUID, since an OTP send has no natural buyer tenant and that field is only
  used for delivery-log reporting on Communication's side, never
  authorization. No changes to Communication were needed.
- **Scaffold mirrors Communication.** `Identity.Domain`/`.Application`/`.Infrastructure`/`.Api`
  + `tests/Identity.Tests`, no transactional outbox (Identity never
  publishes an integration event, same reasoning as Communication), no
  background reaper for expired `PhoneVerification` rows (unlike
  Inventory's `ExpiredHoldReaper` — an expired challenge has no side effect
  to react to; `VerifyOtpHandler` lazily marks it expired on read).
- **Backend-only this pass.** The frontend's `AuthContext` swap from
  dev-login to real Identity-based buyer login (build-order step 10) is
  explicitly out of scope here — not touched.

## Consequences

- The platform can send real Email/SMS/WhatsApp today via the synchronous
  `/v1/notifications/send` endpoint (verifiable end-to-end with only the
  dev/logging senders — no live vendor credentials needed), while the two
  async subscribers prove the redelivery-safety machinery works without yet
  delivering real notifications.
- Switching or dual-running ACS/Twilio per channel is a configuration
  change, not a code change.
- `docs/data-flow-and-service-boundaries.md`'s "not yet consumed" note for
  `OrderConfirmed`/`TicketIssued` is superseded for Communication
  specifically: they now have a subscriber, just not yet real delivery.
- Communication is the first service in this repo to ship a `tests/`
  project (`xunit`/`Shouldly`/`NSubstitute`/`Testcontainers.PostgreSql`/
  `NetArchTest.Rules` were already CPM-pinned and unused), and CI gained its
  first `dotnet test` step — zero risk to the other five services, which
  still have no test projects to run.
- `POST /v1/holds` and `POST /v1/checkout` can now genuinely be exercised
  with a real buyer token issued by Identity, not just `dev-token.sh`'s
  `TENANT_ID=""` simulation of one (ADR-0022's precondition, fulfilled).
- `HoldService.PlaceHoldAsync`/`CheckoutWorkflowInput` already had no
  tenant-claim requirement on the buyer path (ADR-0022) — Identity's tokens
  simply satisfy that shape; no further backend changes were needed to
  "accept" an Identity token beyond each service's `Jwt:Authority` config
  eventually pointing at Identity's real URL (an ops/config task, not code).
- `IRecipientResolver`'s real implementation (Communication → Identity
  phone/email lookup, so `OrderConfirmed`/`TicketIssued` delivery can
  finally resolve a real recipient) remains a separate, not-in-scope-now
  integration — Identity doesn't expose that lookup endpoint this pass
  either. Flagged as a natural next step, not built.
- Local dev has a known gap: every one of the 5 existing services'
  `appsettings.Development.json` still sets `Jwt:DevSigningKey`, so they
  validate via the HS256 dev branch, not Identity's RS256/`Authority`
  branch, by default. Exercising a real Identity token against a real
  service locally is a manual, documented opt-in (temporarily unset that
  service's `Jwt:DevSigningKey`, point `Jwt:Authority` at
  `http://localhost:5087`) — not wired into `dev-up.sh`'s default startup.
- This is a schema-adding change (new `identity` Postgres database) — run
  `./scripts/dev-down.sh -v && ./scripts/dev-up.sh` locally after pulling,
  same as every prior schema-touching pass this session.

## Alternatives considered

- **Single vendor (ACS only)** — rejected per this pass's explicit
  requirement that both ACS and Twilio be addressable under the port.
- **Twilio for email too** — rejected; Twilio's email product is SendGrid,
  a separate SDK/package, out of scope for this pass.
- **Database-stored, organizer-editable templates** — rejected for now; no
  requirement calls for runtime editability, and embedded resources are
  simpler, code-reviewed, and versioned with the code that uses them. The
  port (`ITemplateStore`) already allows a DB-backed adapter later.
- **bcrypt/Argon2/PBKDF2 for OTP hashing** — rejected; a slow KDF defends
  against offline brute-force of a stored secret, but a 6-digit code is
  already bounded by a 5-minute TTL and a 5-attempt lockout — the only real
  requirement (don't store the raw code) is satisfied by a fast keyed HMAC.
- **Implementing the Key Vault signing-key provider now** — rejected for
  this pass; it's non-trivial crypto-delegation code that cannot be
  compiled or exercised against a live Key Vault in this sandbox, and
  nothing in the current dev deploy target would select it anyway. The DI
  gate is built so it's a pure slot-in later.
- **Refresh tokens (access + refresh pair, rotation, revocation)** —
  rejected; explicit user decision this round. Access-token-only avoids an
  entire class of token-theft/replay-window design work a full session
  system would need, at the acceptable cost of a buyer re-verifying via OTP
  once their token expires.
- **A full OIDC framework (IdentityServer/OpenIddict/Duende)** — rejected;
  Identity only needs to satisfy `AddJwtBearer`'s automatic
  Authority-based discovery (a discovery doc + a JWKS endpoint + RS256
  tokens), not a full authorization-code/consent/client-registration
  server. Hand-rolling that minimal surface is consistent with this repo's
  existing lean-over-framework precedent (ADR-0009).
- **Adding `RecipientEmail`/`RecipientPhone` fields to `OrderConfirmed`/`TicketIssued`**
  — rejected; Ordering and Ticketing don't have that data either, so this
  would relocate the gap rather than close it, and would couple two other
  services' contracts to a Communication-service concern.
- **Leaving the two subscribers completely unwired** — rejected; this repo
  already has a "not yet consumed" precedent for that, but it would forfeit
  proving the dedup-ledger/redelivery-safety machinery end-to-end, and both
  events' XML docs already declare "consumed by notifications" as intent.

## References

- `services/payments/Payments.Infrastructure/DependencyInjection.cs` — the
  config-gated adapter-selection pattern this reuses.
- `services/ticketing/Ticketing.Api/Endpoints/TicketingEndpoints.cs` — the
  Dapr pub/sub subscriber pattern this reuses.
- `services/communication/README.md`, `services/communication/CLAUDE.md`.
- `services/identity/README.md`, `services/identity/CLAUDE.md`.
- ADR-0011 — the general tenant-from-claim principle for `ITenantContext`.
- ADR-0022 — the buyer tenant-derivation fix Identity's tokens rely on; the
  precondition this half of the ADR fulfills.
- `building-blocks/EventPlatform.Hosting/AuthenticationExtensions.cs` — the
  unmodified Production JWT-validation path Identity's tokens are designed
  to satisfy.
