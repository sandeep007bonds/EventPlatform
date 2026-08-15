# ADR-0032 — Real token validation in deployed environments; the dev signing key stays local

- **Status:** Accepted
- **Date:** 2026-08-15

## Context

Found during a completeness audit of the deploy manifests, before any
`terraform apply`.

`AuthenticationExtensions` has always had two branches, selected by whether
`Jwt:DevSigningKey` is configured: a symmetric HS256 path for local
development, and the standard `Authority`-based OIDC path (RS256, JWKS,
discovery) for everywhere else. That design was correct.

The manifests set `Jwt__DevSigningKey` on every service, so the cluster took
the **dev** branch. Meanwhile the Identity service (ADR-0016, ADR-0023) signs
**RS256** with a persisted key and an issuer of
`https://identity.eventplatform.example` — a placeholder domain.

The two never met. Identity-issued tokens could not be validated by any
service: wrong algorithm, wrong issuer, wrong key. And because the frontend's
dev-login was retired when Identity shipped, buyer OTP and organizer
email+password were the *only* login paths in the deployed app. Login would
have succeeded and every subsequent call returned 401 — a failure that no
amount of provisioning or health-checking would surface, because every pod
comes up green.

Two smaller findings in the same audit:

- **Queue** was missing `Jwt__DevSigningKey` entirely, unlike its eight
  siblings, so it already took the `Authority` branch — pointed at a
  placeholder domain that does not resolve. Its anonymous join/status
  endpoints worked; its tenant-owned settings endpoints did not.
- The `jwt-dev-signing-key` secret was mounted into every pod.

## Decision

**Configure the branch per environment rather than changing the code.** The
existing two-branch design already expresses exactly the right thing; only the
configuration was wrong.

- Local development was **already** on the `Authority` branch — every service's
  `appsettings.Development.json` points at `http://localhost:5087` with no dev
  signing key. (An earlier claim in `services/identity/CLAUDE.md` that local
  still used the dev key was stale; corrected.) So local and cluster now use
  the same mechanism, differing only in the URL.
- The dev key survives as an **opt-in escape hatch**: set `Jwt__DevSigningKey`
  as an environment variable to run one service in isolation without Identity,
  its database, or Communication. The gateway keeps it in its own Development
  config, which is what maps `POST /api/auth/dev-login` locally.
- The cluster sets `Jwt__Authority=http://identity` and
  `Jwt__RequireHttpsMetadata=false` in the shared config map, and sets no dev
  signing key anywhere. ASP.NET Core fetches
  `{Authority}/.well-known/openid-configuration`, caches Identity's JWKS, and
  validates RS256 signatures against it — rotating keys on its own.
- `Identity__Jwt__Issuer` is set to that same `http://identity` URL. It has to
  match the Authority verbatim: validation compares the token's `iss` against
  the issuer in the discovery document, and a mismatch rejects every token
  with an error that names neither URL.

This incidentally fixes Queue: it now gets a real Authority like everything
else, rather than a placeholder.

`RequireHttpsMetadata=false` is safe here specifically because the Authority
is a Kubernetes Service name — the discovery and JWKS fetches never leave the
cluster. It would not be safe against an external issuer.

## Consequences

- Identity-issued tokens work end to end in the cluster. The deployed app is
  usable through its own UI, which it was not.
- **Dev-login stops working in the cluster.** The gateway maps that endpoint
  only when a dev signing key is configured, and now none is. It remains fully
  available locally, which is the only place it was ever meant to be. Scripts
  that curl a deployed environment need a real Identity token.
- `jwt-dev-signing-key` stays in Key Vault and in the SecretProviderClass but
  is no longer consumed by any workload — kept deliberately as the escape
  hatch, so re-enabling the dev path is adding an env var rather than a
  Terraform round trip.
- Identity now also receives a `Jwt__Authority` pointing at itself. Harmless:
  every one of its own endpoints is `.AllowAnonymous()`, so discovery is never
  triggered.
- A first request after a pod starts pays a one-off discovery round trip.
  Irrelevant at this scale, and cached thereafter.
- **Ordering matters on a cold cluster.** If Identity is not yet serving when
  another service first validates a token, that validation fails until the
  discovery document is reachable. ASP.NET Core retries on subsequent
  requests, so this self-heals rather than latching — but a first login
  attempt during a cold sync can fail once.

## Alternatives considered

- **Accept both token types simultaneously** (two authentication schemes, or a
  composite key resolver). Keeps dev-login working in the cluster, at the cost
  of a deployed environment that permanently accepts a symmetric token signed
  with a key sitting in Key Vault. That is a standing authentication bypass in
  exchange for a testing convenience — rejected.
- **Point `Jwt:Authority` at the public HTTPS ingress hostname** instead of
  in-cluster Service DNS. Lets `RequireHttpsMetadata` stay true, but routes
  service-to-service discovery out through the load balancer and back, and
  couples token validation to the ingress and its certificate being healthy.
- **Change the code to select on `ASPNETCORE_ENVIRONMENT`** rather than key
  presence. Would have made the manifests' intent unambiguous, but the
  services deliberately run as `Development` in this cluster for other
  reasons, so the environment name is not a reliable signal here — and
  presence-of-config is the same gating style Payments and Communication
  already use for their vendor adapters.
