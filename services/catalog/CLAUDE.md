# CLAUDE.md — Catalog service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns events, venues, seat maps, ticket types and pricing. Publishes seated
events and generates their seat inventory. Serves read endpoints for the
storefront (cached). Bounded context: **Catalog** (ADR-0008).

## Owns

- **Data store:** PostgreSQL `catalog` DB (this service only)
- **Public API:** REST `/v1/events`, `/v1/events/{id}/seatmap`
- **Events published:** `EventPublished`, `EventUpdated`
- **Events consumed:** —

## Structure

Currently a skeleton: `src/Catalog.Api` uses `AddServiceDefaults()` /
`UseServiceDefaults()` from `EventPlatform.Hosting`. During Phase 1 (issue #6)
this grows the standard layers: `Catalog.Application` (Features/ slices),
`Catalog.Domain`, `Catalog.Infrastructure`, and `tests/`.

## Local run

```bash
dotnet run --project src/Catalog.Api
# browse the API docs at /scalar/v1 (non-production)
```

## Do not

- Read another service's database.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
