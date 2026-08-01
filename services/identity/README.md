# Identity service

Buyer phone+OTP authentication, and a lightweight OIDC token issuer so the 5
existing services accept its tokens through their existing JWT validation
code — no code changes on their side.

## Flow

1. `POST /v1/otp/request` — buyer submits a phone number (E.164). Identity
   generates a 6-digit code, hashes it (keyed HMAC-SHA256, never stored raw),
   and sends it via Communication's SMS channel (Dapr service invocation).
   Rate-limited to one request per phone number per 60 seconds.
2. `POST /v1/otp/verify` — buyer submits the phone number + code. On a
   match, Identity resolves or creates a durable `BuyerAccount` (verification
   *is* signup) and issues a signed JWT (`sub` = the buyer's stable id,
   ~7-day lifetime, no `tenant_id` — a buyer isn't tenant-scoped). 5 wrong
   attempts locks the challenge out.
3. The 5 existing services validate that JWT automatically: their
   `Jwt:Authority` config points at Identity, and ASP.NET Core's built-in
   OIDC discovery fetches `GET /.well-known/openid-configuration` →
   `GET /.well-known/jwks.json` to find and verify the signing key — no
   custom validation code anywhere.

No refresh tokens — a buyer re-verifies via OTP once their access token
expires.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/otp/request` | Send an OTP code to a phone number (gateway-routed) |
| POST | `/v1/otp/verify` | Verify a code, get back a signed access token (gateway-routed) |
| GET | `/.well-known/openid-configuration` | OIDC discovery document (server-to-server only) |
| GET | `/.well-known/jwks.json` | Public signing key(s), RFC 7517 (server-to-server only) |

## Signing key

A 2048-bit RSA key, generated once and persisted (PKCS8, base64) in the
`identity` Postgres database, so a restart doesn't invalidate outstanding
buyer sessions. `Identity:Jwt:SigningKeySource` can select a Key Vault-backed
provider instead — **not implemented yet**; requesting it fails fast at
startup rather than silently falling back. See [CLAUDE.md](CLAUDE.md) and
[ADR-0016](../../docs/adr/0016-buyer-identity-and-notifications.md).

## Layers

`Identity.Api` (no Dapr pub/sub — zero subscriptions) · `Identity.Application`
(OTP request/verify slices + ports) · `Identity.Domain`
(`PhoneVerification`, `BuyerAccount`, `SigningKey`) · `Identity.Infrastructure`
(EF Core + Postgres, OTP hashing, the Dapr call to Communication, RSA
signing-key management — no outbox) · `tests/Identity.Tests`.

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a
Dapr sidecar, Postgres, and Communication running (for the SMS leg). With no
SMS vendor configured on Communication, every OTP send uses its dev/logging
sender — read the code from the console output or the
`communication.delivery_log` table.
