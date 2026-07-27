# Gateway (BFF)

The frontend's single entry point to the backend — a YARP reverse proxy with
an explicit route allowlist, plus a Development-only login endpoint.

## Why

The 5 backend services have no CORS policy and no gateway existed before
this, so a browser couldn't call them directly. Rather than adding CORS to
all 5 (and growing that list with every future service), this gateway is the
one place a browser ever talks to, and the one place CORS is configured.

## Routing

Only the buyer/organizer-facing endpoints are mapped, under
`/api/<service>/v1/...` (see `appsettings.json`'s `ReverseProxy` section for
the exact list). Saga-internal endpoints (Inventory's hold
convert/release, Payments' charge/refund/webhook) are deliberately **not**
routed.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/auth/dev-login` | Development-only token mint (stands in for real login) |
| * | `/api/catalog/v1/...` | Proxied to Catalog |
| * | `/api/inventory/v1/...` | Proxied to Inventory (allowlisted routes only) |
| * | `/api/ordering/v1/...` | Proxied to Ordering |
| * | `/api/ticketing/v1/...` | Proxied to Ticketing |

Payments has no route — its only public endpoint is the Stripe webhook,
which Stripe calls directly, not through this gateway.

## Layers

Single project (no Domain/Application split — intentionally thin):
`Program.cs` (YARP + host wiring) · `Cors/` (frontend CORS policy) ·
`DevAuth/` (dev-only token mint).

See [service CLAUDE.md](CLAUDE.md), [ADR-0015](../../docs/adr/0015-frontend-react-vite-antd-and-bff-gateway.md),
and [ADR-0016](../../docs/adr/0016-buyer-identity-and-notifications.md).

## Run locally

See [docs/local-e2e-walkthrough.md](../../docs/local-e2e-walkthrough.md).
Needs the 5 backend services running to have anything to proxy to.
