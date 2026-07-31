# CLAUDE.md — Catalog service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns events, venues, seat maps, ticket types and pricing. Publishes seated
events and generates their seat inventory. Serves read endpoints for the
storefront (cached). Bounded context: **Catalog** (ADR-0008).

## Owns

- **Data store:** PostgreSQL `catalog` DB (this service only)
- **Public API:** REST `/v1/events`, `/v1/events/{id}/seatmap`,
  `/v1/events/{id}/details`, `/v1/event-groups`. `GET /v1/events` (list) and
  `GET /v1/events/{id}` and `/seatmap` are `.AllowAnonymous()` —
  anonymous and cross-tenant callers only ever see non-draft events; a caller's
  own tenant additionally sees its drafts (`Event.IsVisibleTo`). `GET /v1/events?mine=true`
  bypasses that visibility rule entirely and returns only the caller's own
  tenant's events at any status (401 without a tenant) — the organizer
  dashboard view, not public browsing. `GET /v1/events?eventGroupId=` filters
  to the legs of a given tour/series, in either mode. `POST /v1/events/{id}/publish`
  and `PUT /v1/events/{id}/details` both require the caller's tenant to own the
  event (404 on a mismatch, same opaque not-found pattern as `DefineSeatMap`);
  `UpdateEventDetails` is Draft-only (409 otherwise). Location (venue name,
  address, city, geo) is set inline on `Event` at creation — there is no
  separate Venue entity/directory. `EventGroup` ("tour") is a thin, optional
  parent clustering multiple independently-sellable `Event`s: tenant-owned,
  `POST`/`GET /v1/event-groups` require the caller's tenant; `GET /v1/event-groups/{id}`
  is `.AllowAnonymous()` — a group by itself, unlinked, reveals nothing
  sensitive. Each leg is created/published/seat-mapped/sold exactly like any
  standalone event (see ADR-0019) — Inventory/Ordering/Ticketing have no
  concept of `EventGroup` at all.
- **Events published:** `EventPublished`, `EventUpdated`
- **Events consumed:** —

## Structure

Layers sit directly under this folder (no `src/`): `Catalog.Api` (host +
endpoints, uses `EventPlatform.Hosting`), `Catalog.Application` (Features/ slices),
`Catalog.Domain` (aggregate + invariants), `Catalog.Infrastructure` (EF Core +
Postgres). `tests/` to follow.

## Local run

```bash
dotnet run --project services/catalog/Catalog.Api
# browse the API docs at /scalar/v1 (non-production)
```

## Do not

- Read another service's database.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
