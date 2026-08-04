# ADR-0023 — Organizer auth: in-house email+password via Identity, superseding Entra External ID

- **Status:** Accepted
- **Date:** 2026-08-04

## Context

The original build order's step 9 planned organizer (admin-side) auth as a
managed Microsoft Entra External ID tenant — provisioning an Azure AD B2C
tenant, registering the frontend app, wiring `@azure/msal-react` into
`AdminLayout`, and pointing `AuthenticationExtensions.cs`'s already-written
Production OIDC branch at it. That plan predates Identity (ADR-0016) —
Identity was originally scoped as buyer-only, with organizer auth
deliberately left to a separate external IdP.

Identity now exists and already does the hard part: an RS256 signing key
generated once and persisted in Postgres, a hand-rolled
`/.well-known/openid-configuration` + `/.well-known/jwks.json` pair, and —
critically — the 5 downstream services already validate any RS256 issuer at
a configured `Authority` with **zero code changes**, because
`AuthenticationExtensions.cs`'s Production branch is plain `AddJwtBearer` +
`options.Authority`/`options.Audience`. Organizers are also a materially
different population than buyers: a small, trusted set of accounts (not the
phone-scale, high-volume problem OTP was built for), which email+password
handles perfectly well without needing a dedicated external IdP.

Reusing Identity avoids provisioning a new Azure tenant, avoids a
per-organizer invitation/provisioning workflow with an external directory,
and keeps one fewer external dependency for a self-hosted, cost-conscious
platform (see ADR-0017's dev-topology cost reasoning, in the same spirit).

**No `Tenant` entity existed anywhere in this repo before this change** —
confirmed by a full grep across every service. `tenant_id` was purely an
opaque GUID stamped from a JWT claim (`TenantContextMiddleware.cs` reads
`User.FindFirstValue("tenant_id")`, nothing more); ADR-0022 already flagged
this gap in passing (dev-login's `TenantId` being caller-suppliable with no
membership check to validate it against). Organizer registration is
therefore also, necessarily, tenant registration — there was no existing
"create a tenant" mechanism to call into.

## Decision

Extend the existing Identity service with a second, parallel auth flow —
organizer email+password — rather than provisioning Entra External ID or
building a separate service:

- **Self-service tenant creation.** `POST /v1/organizers/register` takes an
  organization name + email + password and creates a new `Tenant` and its
  first `OrganizerAccount` together, atomically, in one transaction. The
  registering organizer becomes that tenant's sole account. There is no
  invite-a-teammate/multi-organizer-per-tenant flow this pass — explicitly
  deferred, not silently dropped (see Consequences).
- **`PasswordHasher<TUser>` from `Microsoft.Extensions.Identity.Core`, not
  hand-rolled hashing.** This is the deliberate opposite choice from OTP's
  hand-rolled keyed-HMAC hashing (see ADR-0016's Identity decision): a
  password's risk profile — long-lived, no attempt-capped/short-TTL
  structure — differs enough from a 6-digit/5-minute/5-attempt code that a
  vetted, battle-tested primitive is worth the one extra package here. Only
  the `PasswordHasher<TUser>` piece is pulled in — **not** the full ASP.NET
  Core Identity cookie/UI/EF-store stack, which would conflict with
  Identity's own JWT issuance and add machinery this service doesn't need.
- **Account lockout: 5 failed attempts, 15-minute lockout**
  (`OrganizerAccount.MaxFailedAttempts`/`LockoutDuration`) — the same
  attempt-capped pattern `PhoneVerification` already established for OTP,
  applied here for the same reason: bound the cost of a brute-force guess
  loop. `LoginOrganizerHandler` returns the same generic
  `InvalidCredentials` outcome whether the email is unregistered or the
  password is wrong, standard anti-enumeration practice already consistent
  with how this codebase never reveals another tenant's resource existence
  elsewhere (e.g. seat-map tenant-mismatch 404s).
- **One token issuer, two claim shapes.** `ITokenIssuer.IssueAsync` is
  generalized from a buyer-only `IssueAsync(Guid buyerId, ct)` to
  `IssueAsync(Guid subjectId, string role, Guid? tenantId, ct)`. A buyer
  token still carries `role: "buyer"` and omits `tenant_id` entirely (per
  ADR-0022 — buyers aren't tenant-scoped). An organizer token carries
  `role: "organizer"` **and** `tenant_id` — organizers genuinely are
  tenant-scoped, the opposite of a buyer. Both are minted by the same
  `JwtTokenIssuer`, signed by the same persisted RSA key, served through the
  same JWKS/discovery document — no per-audience key or endpoint split.
- **Dev-login retired from the frontend UI.** With real login now covering
  both roles, `/admin/login` renders only the new organizer register/login
  flow; `/login` (buyer) already moved to the real OTP flow in an earlier
  pass. `LoginPage.tsx`/`authApi.ts` (the shared dev-login form and its API
  client) are deleted — they had no remaining caller. The gateway's
  `POST /api/auth/dev-login` endpoint itself is untouched and stays
  available for curl/script testing, same treatment `scripts/dev-token.sh`
  already gets — just orphaned from the app's UI.

## Consequences

- Organizer auth needs no new Azure resource, no MSAL wiring, and no
  external-directory provisioning workflow — it ships as an extension of an
  already-deployed service.
- This repo's first `Tenant` entity now exists, in Identity's `identity`
  schema (new `tenants`/`organizer_accounts` tables — local dev DB rebuild
  required: `./scripts/dev-down.sh -v && ./scripts/dev-up.sh`).
- `ITokenIssuer`/`JwtTokenIssuer`'s signature change touches every existing
  caller: `VerifyOtpHandler`'s one call site now passes `"buyer"`/`null`
  explicitly. Existing `Identity.Tests` doubles of `ITokenIssuer` needed the
  same signature update.
- Invite-a-teammate / multi-organizer-per-tenant is **not built**. A second
  organizer joining an existing tenant needs a real invite-token flow (email
  delivery via Communication, an acceptance endpoint, a permissions
  decision) — deferred as a separate, later feature.
- Password reset / forgot-password is **not built**. An organizer who
  forgets their password has no self-service recovery yet — a real,
  expected follow-up, not silently dropped.
- `PasswordVerificationResult.SuccessRehashNeeded` (e.g. after a future
  iteration-count upgrade) is currently treated as a plain successful
  verification with no rehash side effect — a documented, deliberate no-op
  for this pass, not a correctness gap.
- `UnauthorizedPage.tsx`'s "Log in" link is now route-aware (`/admin/login`
  under `/admin/*`, `/login` elsewhere) — with two genuinely different login
  flows per role, sending an organizer to the buyer OTP page would have been
  a real regression, not just cosmetically wrong.

## Alternatives considered

- **Provision Entra External ID as originally planned** — rejected: real
  ongoing cost and provisioning complexity for a problem (small, trusted
  organizer account base) that email+password already solves; would also
  add a second, differently-shaped IdP alongside Identity rather than
  reusing machinery already built and proven.
- **Full ASP.NET Core Identity (cookie/UI/EF-store stack)** — rejected:
  Identity already owns JWT issuance via its own `JwtTokenIssuer`/RSA
  signing key; adopting ASP.NET Core Identity's own cookie-based sign-in
  and EF store would conflict with that instead of complementing it. Taking
  only `PasswordHasher<TUser>` gets the vetted hashing primitive without the
  unwanted machinery.
- **Hand-rolled password hashing, mirroring OTP's HMAC approach** —
  rejected: a password's risk profile doesn't share OTP's short-TTL/
  attempt-capped structure that makes a slow KDF unnecessary there: see
  ADR-0016's Identity decision for that reasoning, which does not carry
  over to a long-lived credential.
- **Manual/admin-provisioned tenants** (an operator creates tenants
  out-of-band, organizers register against an existing tenant/invite code)
  — rejected for this pass: no provisioning tool exists yet, and
  self-service signup is the simpler, more standard SaaS onboarding shape;
  revisit if/when a real need for gated tenant creation appears.

## References

- ADR-0016 — Identity's original buyer-OTP design; this ADR extends the
  same service rather than superseding that decision.
- ADR-0022 — the buyer-token-omits-`tenant_id` precedent this ADR's
  organizer-token-carries-`tenant_id` decision deliberately mirrors/inverts.
- ADR-0011 — the general "tenant is trusted only from a validated claim,
  never client-supplied" principle; self-service tenant *creation* here
  doesn't violate it, since the created tenant's id is never
  caller-suppliable — it's generated server-side at registration.
- `services/identity/CLAUDE.md`, `services/identity/README.md`,
  `frontend/CLAUDE.md` — updated for the new organizer auth surface.
