# CLAUDE.md — Catalog service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns events, event groups (tours), seat maps (Reserved and General Admission
sections), ticket types and pricing. Publishes events and generates their
inventory. Serves read endpoints for the storefront (cached). Bounded
context: **Catalog** (ADR-0008).

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
  concept of `EventGroup` at all. `EventGroup` also holds tour-wide date-range
  and contact/social defaults (`StartsAt`/`EndsAt`, `ContactPhone`/`ContactMobile`/
  `ContactEmail`/`WebsiteUrl`, an open `SocialLinks` list) that a leg's own
  values (if any) override entirely (see ADR-0020).
- **`Event.EndsAt` is required** (set at creation, alongside `StartsAt`) — every
  leg has a real date range, not just a start instant. **`Event.BookingEndsAt`**
  (renamed from the old, display-only `OffSaleAt`) and **`Event.OnSaleAt`** are
  both real, **enforced** window bounds: Catalog hands both to Inventory via
  `EventPublished` so Inventory can reject new holds outside that window
  (before `OnSaleAt`, after `BookingEndsAt`). Neither can change after publish
  in this pass (`UpdateEventDetails` is Draft-only, same as every other detail
  field).
- **`Event.MaxTicketsPerBuyer`** (nullable — `null` means no limit), settable
  at `Create`/editable via `UpdateDetails` (Draft-only, same lifecycle as
  `BookingEndsAt`). Propagated to Inventory via `EventPublished`, which
  enforces it cumulatively across a buyer's holds at hold-placement time —
  Catalog itself does not enforce anything (see ADR-0021).
- **Seat maps mix Reserved and General-Admission sections.** `DefineSeatMap`'s
  section input carries an `AllocationType`: `Reserved` sections generate
  individual `Seat` rows (rows × seats-per-row) exactly as before;
  `GeneralAdmission` sections are a capacity-only pool (no individual seats) —
  a single event can have both kinds side by side. `GetSeatMapResponse`
  (the hand-off Inventory reads) carries both `Seats` and
  `GeneralAdmissionSections` lists.
- **Events published:** `EventPublished` (now also carries `BookingEndsAt` and
  `MaxTicketsPerBuyer`), `EventUpdated`
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
