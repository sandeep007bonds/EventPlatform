# Identity service

Two auth flows, one RS256 OIDC token issuer: buyer phone+OTP authentication,
and organizer email+password registration/login. The 5 existing services
accept tokens from either flow through their existing JWT validation code —
no code changes on their side.

## Buyer flow

1. `POST /v1/otp/request` — buyer submits a phone number (E.164). Identity
   generates a 6-digit code, hashes it (keyed HMAC-SHA256, never stored raw),
   and sends it via Communication's SMS channel (Dapr service invocation).
   Rate-limited to one request per phone number per 60 seconds.
2. `POST /v1/otp/verify` — buyer submits the phone number + code. On a
   match, Identity resolves or creates a durable `BuyerAccount` (verification
   *is* signup) and issues a signed JWT (`sub` = the buyer's stable id,
   `role: "buyer"`, ~7-day lifetime, no `tenant_id` — a buyer isn't
   tenant-scoped). 5 wrong attempts locks the challenge out.

## Organizer flow

1. `POST /v1/organizers/register` — a new organizer submits an organization
   name + email + password. Identity creates a new `Tenant` and its first
   `OrganizerAccount` together, atomically, and issues a signed JWT
   (`sub` = the organizer's stable id, `role: "organizer"`, `tenant_id` = the
   new tenant — unlike a buyer token). One organizer per tenant this pass;
   inviting a teammate into an existing tenant is not built yet.
2. `POST /v1/organizers/login` — an existing organizer submits email +
   password. 5 consecutive wrong passwords locks the account out for 15
   minutes. Passwords are hashed with `Microsoft.Extensions.Identity.Core`'s
   `PasswordHasher<TUser>` — not the hand-rolled HMAC scheme OTP uses (see
   [ADR-0023](../../docs/adr/0023-organizer-auth-in-house-identity.md)).

Both flows are validated identically downstream: the 5 existing services'
`Jwt:Authority` config points at Identity, and ASP.NET Core's built-in OIDC
discovery fetches `GET /.well-known/openid-configuration` →
`GET /.well-known/jwks.json` to find and verify the signing key — no custom
validation code anywhere, and no per-audience split (one key, one JWKS,
both token shapes).

No refresh tokens for either flow — a buyer re-verifies via OTP, an
organizer re-submits their password, once the access token expires.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/otp/request` | Send an OTP code to a phone number (gateway-routed) |
| POST | `/v1/otp/verify` | Verify a code, get back a signed access token (gateway-routed) |
| POST | `/v1/organizers/register` | Register a new organization + its first organizer account (gateway-routed) |
| POST | `/v1/organizers/login` | Log in with an existing organizer email+password (gateway-routed) |
| GET | `/.well-known/openid-configuration` | OIDC discovery document (server-to-server only) |
| GET | `/.well-known/jwks.json` | Public signing key(s), RFC 7517 (server-to-server only) |

## Signing key

A 2048-bit RSA key, generated once and persisted (PKCS8, base64) in the
`identity` Postgres database, so a restart doesn't invalidate outstanding
buyer or organizer sessions. `Identity:Jwt:SigningKeySource` can select a Key
Vault-backed provider instead — **not implemented yet**; requesting it fails
fast at startup rather than silently falling back. See [CLAUDE.md](CLAUDE.md),
[ADR-0016](../../docs/adr/0016-buyer-identity-and-notifications.md), and
[ADR-0023](../../docs/adr/0023-organizer-auth-in-house-identity.md).

## Password hashing

Organizer passwords are hashed with `Microsoft.Extensions.Identity.Core`'s
`PasswordHasher<TUser>` (`Identity.Infrastructure/Security/AspNetCorePasswordHasher.cs`)
— only that package, not the full ASP.NET Core Identity cookie/UI/EF-store
stack, which would conflict with Identity's own JWT issuance. OTP codes are
hashed differently (keyed HMAC-SHA256, see above) — the two credentials have
different risk profiles, see ADR-0023.

## Layers

`Identity.Api` (no Dapr pub/sub — zero subscriptions) · `Identity.Application`
(`Otp/` and `Organizers/` slices + ports) · `Identity.Domain`
(`PhoneVerification`, `BuyerAccount`, `Tenant`, `OrganizerAccount`,
`SigningKey`) · `Identity.Infrastructure` (EF Core + Postgres, OTP hashing,
password hashing, the Dapr call to Communication, RSA signing-key
management — no outbox) · `tests/Identity.Tests`.

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a
Dapr sidecar, Postgres, and Communication running (for the SMS leg). With no
SMS vendor configured on Communication, every OTP send uses its dev/logging
sender — read the code from the console output or the
`communication.delivery_log` table.
