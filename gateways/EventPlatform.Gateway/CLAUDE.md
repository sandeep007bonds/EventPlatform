# CLAUDE.md — Gateway (BFF)

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

The browser-facing entry point for the frontend. A thin YARP reverse proxy
with an explicit route allowlist — never a blanket per-service proxy — plus a
Development-only token-mint endpoint standing in for real login until
Identity (buyer OTP) and Entra External ID (organizer OIDC) exist. See
ADR-0015 and ADR-0016.

## Owns

- **No data store.** Stateless; owns no schema, no domain.
- **Public API:** `POST /api/auth/dev-login` (Development only); everything
  else is proxied under `/api/<service>/...` to the 6 backend services per
  the explicit allowlist in `appsettings.json`'s `ReverseProxy` section
  (Payments and Communication have no routes — saga-internal/Dapr-invocation
  only, never reachable through this gateway).
- **Events published/consumed:** none — the gateway is an HTTP ingress
  concern only, not a Dapr participant.

## Design notes

- **Explicit allowlist, not a blanket proxy.** Only frontend-facing routes
  are mapped. Inventory's `holds/{id}/convert`/`release` and Payments'
  `charge`/`refund`/webhook are deliberately unmapped — saga-internal or
  Stripe-only, never reachable through this gateway.
- **Auth pass-through, not re-validation.** The gateway forwards the
  `Authorization` header unchanged; it never validates JWTs itself. Each
  backend service keeps validating exactly as it always has.
- **No Dapr.** This is a browser-facing ingress concern, not a service
  participating in pub/sub or service invocation — it runs as a plain
  process, no sidecar.
- **Dev-login is Development-only and self-guarding.** Only mapped when
  `Jwt:DevSigningKey` is configured; the host also fails fast at startup if
  that key is somehow set while `ASPNETCORE_ENVIRONMENT=Production`. Once
  Identity and Entra External ID exist, dev-login stops being the app's real
  login path — kept only for curl/script testing, like
  `scripts/dev-token.sh` already is.
- **CORS lives here, nowhere else.** The 5 backend services have no CORS
  policy and never will — only the gateway is ever called from a browser.

## Structure

`EventPlatform.Gateway` (single project — no Domain/Application split; the
gateway is intentionally thin): `Program.cs`, `Cors/`, `DevAuth/`. Reuses
`EventPlatform.Hosting` for JSON/observability/health/OpenAPI defaults only —
deliberately does **not** call `AddServiceDefaults`/`UseServiceDefaults`
wholesale, since that bundles auth + tenant middleware a stateless proxy
doesn't own.

## Local run

```bash
dotnet run --project gateways/EventPlatform.Gateway
# browse the API docs at /scalar/v1 (non-production)
```

Run the 5 backend services too (see `docs/local-e2e-walkthrough.md`) — the
gateway has nothing to proxy to on its own.

## Do not

- Add a route for a saga-internal or Stripe-only endpoint.
- Validate JWTs here — that stays each backend service's job.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Let `Jwt:DevSigningKey` reach a Production config.
