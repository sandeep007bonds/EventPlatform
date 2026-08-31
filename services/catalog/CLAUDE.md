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
  `/v1/events/{id}/seatmap/sections` (`POST` add, `PUT` replace-one-by-name),
  `/v1/events/{id}/seatmap/sections/{sectionName}` (`DELETE`),
  `/v1/events/{id}/details`, `/v1/event-groups`, `/v1/events/{id}/entry-gates`.
  `GET /v1/events` (list) and
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
  values (if any) override entirely (see ADR-0020). `CreateEvent`/
  `UpdateEventDetails` both reject (409) a leg whose `[StartsAt, EndsAt]`
  falls outside its tour's own advertised range, or overlaps a sibling leg's
  dates; `CreateEvent` also rejects (404) an `EventGroupId` that doesn't
  belong to the caller's tenant (ADR-0024).
- **`Event.EndsAt` is required** (set at creation, alongside `StartsAt`) — every
  leg has a real date range, not just a start instant. **`Event.BookingEndsAt`**
  (renamed from the old, display-only `OffSaleAt`) and **`Event.OnSaleAt`** are
  both real, **enforced** window bounds: Catalog hands both to Inventory via
  `EventPublished` so Inventory can reject new holds outside that window
  (before `OnSaleAt`, after `BookingEndsAt`). Neither can change after publish
  in this pass (`UpdateEventDetails` is Draft-only, same as every other detail
  field). `BookingEndsAt` also can never be later than the leg's own
  `StartsAt` (ADR-0024).
- **`Event.MaxTicketsPerBuyer`** (nullable — `null` means no limit), settable
  at `Create`/editable via `UpdateDetails` (Draft-only, same lifecycle as
  `BookingEndsAt`). Propagated to Inventory via `EventPublished`, which
  enforces it cumulatively across a buyer's holds at hold-placement time —
  Catalog itself does not enforce anything (see ADR-0021).
- **`Event.RequiresQueue`** (bool, default `false`), settable at `Create`/
  editable via `UpdateDetails` (Draft-only, same lifecycle as
  `BookingEndsAt`) — the single on/off switch for gating a buyer's hold
  behind the Queue service's virtual waiting room. Propagated via
  `EventPublished` to both Inventory (enforcement) and Queue (provisioning);
  Catalog itself does not enforce anything (see ADR-0026).
- **Seat maps mix Reserved and General-Admission sections.** `DefineSeatMap`'s
  section input carries an `AllocationType`: `Reserved` sections generate
  individual `Seat` rows (rows × seats-per-row) exactly as before;
  `GeneralAdmission` sections are a capacity-only pool (no individual seats) —
  a single event can have both kinds side by side. `GetSeatMapResponse`
  (the hand-off Inventory reads) carries both `Seats` and
  `GeneralAdmissionSections` lists. `DefineSeatMap` still only ever creates the
  map (404/`AlreadyDefined` on a second call); `POST /v1/events/{id}/seatmap/sections`
  (`AddSeatMapSections`) appends more sections to an **existing** Draft-only
  map — same section-shape/validation/entry-gate rules, same
  `SeatMap.AddReservedSection`/`AddGeneralAdmissionSection` domain methods
  (which already enforce name-uniqueness against every section already in the
  map, not just the ones in the request), just loaded via
  `ISeatMapRepository.GetTrackedByEventIdAsync` (change-tracked, unlike the
  `AsNoTracking()` `GetByEventIdAsync` every read path uses) so the new
  sections are picked up by `SaveChangesAsync`. `PUT .../sections`
  (`UpdateSeatMapSection`) and `DELETE .../sections/{sectionName}`
  (`RemoveSeatMapSection`) round out full Draft-only editing —
  `SeatMap.RemoveSection` deletes every seat/GA-section row matching a name
  (EF orphan-delete on the tracked collection, no explicit `Remove()` call
  needed); "edit" is implemented as remove-then-`AddReservedSection`/
  `AddGeneralAdmissionSection` rather than a true in-place update, since a
  section's rows/capacity can only be expressed by regenerating its
  seats/pool anyway — freeing the name first means the existing
  duplicate-name check needs no special-casing for a same-name (no rename)
  edit. Safe only pre-publish, since nothing outside Catalog references a
  seat/section id before Inventory provisions from it.
- **Entry gates.** `EntryGate` (`Id`, `EventId`, `Name`) — an organizer defines
  named physical entry points for an event's location, then restricts a
  seat-map section to one at `DefineSeatMap` time (`Seat.EntryGateId`/
  `GeneralAdmissionSection.EntryGateId`, set once, immutable after — a
  section with no gate set may be entered through any gate). No update/delete
  slice this pass. Ticketing resolves gate eligibility live at scan time via
  Dapr service invocation against `GET /v1/events/{id}/seatmap` — Catalog
  does not enforce anything itself (see ADR-0024).
- **Manual sales pause.** `Event.SalesPaused` (bool, default `false`) lets an organizer pause/resume
  sales on an already-`Published` event via `POST /v1/events/{id}/pause-sales`/`resume-sales`
  (`Event.PauseSales`/`ResumeSales`, 409 if not published or already in the requested state) —
  independent of the `OnSaleAt`/`BookingEndsAt` enforced time window, and without affecting
  already-placed holds/tickets. Publishes `EventSalesPaused`/`EventSalesResumed` for Inventory to
  reject/allow new holds accordingly (see ADR-0027).
- **Promo codes and tax (ADR-0034).** `PromoCode` (with its child `PromoCodeTier`) is a Catalog
  aggregate — a discount code is part of an event's commercial setup. Percentage or fixed amount,
  optional validity window, optional tier scoping, optional caps on total and per-buyer
  redemptions, public or private. Managed via `/v1/events/{eventId}/promo-codes`
  (`POST` create, `GET` list — both tenant-owned) and `.../{id}/deactivate`; two anonymous reads
  serve the buyer path: `.../promo-codes/public` (what a buyer may pick from — a deliberately
  narrower response that never publishes redemption caps) and `.../promo-codes/by-code/{code}`
  (server-to-server, for Ordering; unrouted at the gateway). **Catalog answers what the rules are;
  it never decides whether a code may be used** — redemption counting needs the orders, which only
  Ordering can read. **An empty tier list means every tier**, so an organizer discounting a whole
  order never enumerates their tiers and a tier added later is covered rather than excluded.
  There is no edit-after-create (`EntryGate`'s precedent): deactivate and make another.
  `Event.TaxRatePercent`/`TaxLabel` are one rate per event, Draft-only editable like every other
  detail field; Ordering charges it on the **post-discount** amount.
  `Event.BookingFeePerTicketMinor` is the same shape — Draft-only, stored here, computed by
  Ordering — a flat per-ticket fee in minor units. It is **not** discountable, **is** taxed, and is
  **not** returned on a cancellation, which is why Ordering rounds tax on the fee separately from
  tax on the tickets (ADR-0034). Catalog stores the number and enforces only that it is not
  negative.
- **`Event.TimeZoneId`** (nullable IANA id, e.g. `Asia/Kolkata`), Draft-only editable like every
  other detail field. Nothing on the backend reads it: every date is a `DateTimeOffset` and already
  an unambiguous instant, so this changes *when* nothing. It exists because a client otherwise has
  to render a start time in the **reader's** zone — a 7pm Delhi show reads as 1:30pm to a buyer in
  London. Stored as an IANA identifier rather than an offset, since offsets shift twice a year and
  an event across a DST boundary would drift. Validated in the *validator*, not the aggregate:
  resolving an id depends on the host's tz database, and an invariant that varies by machine is not
  an invariant.
- **`TicketType` — what a section is sold as (stage 1 of 3).** A named, priced aggregate per event:
  `Name` (unique per event, case-insensitively), `PriceMinor`, `Description`, `SalesStartsAt`/
  `SalesEndsAt`, `MaxPerBuyer`, `SortOrder`, `IsActive`. `Seat` and `GeneralAdmissionSection` now
  carry a `TicketTypeId`; their `PriceTier`/`PriceAmount` columns survive the migration window but
  **nothing reads them** — `GetSeatMapHandler` projects both from the joined type, so a rename or
  reprice takes effect at once rather than leaving thousands of stale seat rows. Managed via
  `/v1/events/{eventId}/ticket-types` (`POST`/`GET`/`PUT` + `.../{id}/deactivate`), all
  `RequireOrganizer()` with the usual opaque-404 tenant check.
  **Unlike every seat-map endpoint these are not Draft-only** — creating a type on a published event
  is the point, since an organizer opening a late release should not have to make a second event.
  The exception is **repricing, refused after publish** (409): Inventory holds its own copy of the
  price from provisioning time, so changing it here would move the displayed number and not the
  charged one. Seat-map requests still name a tier and a price; `TicketTypeResolver` finds or
  creates the type, and **an existing type's price wins** — the validators reject a single request
  naming one tier at two prices, which is the contradiction an organizer can see and fix.
  Types created here sell nothing until stage 2 lets capacity be added to a published event.
- **Events published:** `EventPublished` (now also carries `BookingEndsAt` and
  `MaxTicketsPerBuyer`), `EventUpdated`, `EventSalesPaused`, `EventSalesResumed`
- **Events consumed:** —

## Structure

Layers sit directly under this folder (no `src/`): `Catalog.Api` (host +
endpoints, uses `EventPlatform.Hosting`), `Catalog.Application` (Features/ slices),
`Catalog.Domain` (aggregate + invariants), `Catalog.Infrastructure` (EF Core +
Postgres). `tests/Catalog.Tests` covers `Event`'s date and lifecycle guards
(including `IsVisibleTo`, which is a security boundary rather than a display
rule), `SeatMap`'s seat generation/capacity/name-uniqueness across Reserved and
General-Admission sections, and the three cross-aggregate tour rules in
`CreateEventHandler` — exercised through MediatR, since the handler is internal
and the validation pipeline is part of what the endpoint actually invokes.

## Local run

```bash
dotnet run --project services/catalog/Catalog.Api
# browse the API docs at /scalar/v1 (non-production)
```

## Do not

- Read another service's database.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
