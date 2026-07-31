# ADR-0019 — Event tours (`EventGroup`) and inline event location, replacing Venue

- **Status:** Accepted
- **Date:** 2026-07-31

## Context

The just-shipped `Venue` aggregate (ADR-0018-adjacent work: a reusable,
tenant-owned venue directory linked from `Event.VenueId`) turned out not to
match what's actually needed. Explicit direction: "we dont require venue..
event will have all the information.. and this event can support multiple
days, location per date etc." Follow-up clarified the real shape of the ask:
something like a Coldplay world tour — one promotional umbrella that plays
in different cities in India on different dates, where a buyer can buy a
ticket for one specific city/date (confirmed as the priority) and,
potentially later, a bundle covering multiple dates.

Re-reading the code surfaced the deciding architectural fact: Inventory,
Ordering, and Ticketing already assume **one seat map / one inventory batch
/ one checkout per `Event`** — `Inventory.Domain/InventoryItem.cs`'s
`EventId`, Inventory's `EventPublished` subscription that provisions
inventory per event, and `SeatMap.EventId` ("one seat map per event"). Making
each city/date of a tour independently sellable therefore does **not**
require a new "Occurrence" sub-entity under `Event` with Inventory/Ordering/
Ticketing rewired to a finer key — that would be a much larger, riskier
change touching four services. It's simpler and strictly additive to keep
`Event` exactly as it already is (one `Event` = one sellable show/leg,
unchanged contract with the other three services) and add a thin, optional
parent that just clusters multiple `Event`s for display and organizer
convenience.

## Decision

- **`EventGroup`** (`Catalog.Domain/EventGroup.cs`) — deliberately thin:
  `Id`, `TenantId`, `Title` only. It exists purely so multiple legs
  (`Event`s) can be clustered under one organizer-facing heading; it carries
  no shared banner/description/media — each leg keeps its own full details
  exactly as `Event` already supports. `Event` gains a nullable
  `EventGroupId` — `null` for a standalone one-off event (the common case,
  and every event created before this ADR), set when the organizer creates a
  leg as part of a tour. **Zero changes to Inventory, Ordering, Ticketing, or
  the gateway** — every leg is created, published, seat-mapped, held,
  checked out, and ticketed through the exact same code path as any
  standalone event today.
- Naming: the domain/API type is `EventGroup` (generic — a tour, a
  multi-city comedy circuit, a conference roadshow are all the same shape),
  but the UI labels it "Tour," matching the organizer's own mental model.
  `GET /v1/events?eventGroupId=` (both public and `mine=true` modes) lists a
  tour's legs — reused by the public event page ("other dates on this
  tour") and the admin dashboard; no separate endpoint needed for that.
- **`Venue` is removed entirely** — the aggregate, its repository, its four
  Application slices, its endpoints, and the admin Venue pages are all
  deleted. `Event.VenueId` is replaced with inline structured location
  fields directly on `Event`: `LocationName`, `AddressLine1`,
  `AddressLine2?`, `City`, `Region?`, `PostalCode?`, `Country`, `Latitude?`,
  `Longitude?` — the same shape `Venue` had, minus `TenantId` (implicit via
  `Event.TenantId`) and `Capacity` (redundant with the event's own
  `SeatMap.Capacity`). Confirmed via explicit direction: structured fields,
  not free text — an organizer fills in the same address detail as before,
  it's just no longer a separate linked, reusable record. This also
  simplifies the public event page: it no longer needs a second network
  call to resolve a venue by id.
- **Bundle/package purchase across multiple legs of a tour is explicitly
  deferred**, not silently dropped. The user's own words confirmed this is
  wanted eventually ("you can buy the complete package also"), but it needs
  Ordering's checkout saga to support a multi-event order with its own
  price — a materially separate, larger feature. This pass makes each leg
  independently buyable (the confirmed priority) and leaves bundle purchase
  as clearly-scoped future work, revisitable without another schema change
  since `EventGroupId` already links the legs.

## Consequences

- Organizers can create a tour (`EventGroup`) once, then create multiple
  city/date legs under it, each independently sellable through the entire
  existing checkout pipeline with no new code in Inventory/Ordering/
  Ticketing. A standalone one-off event (no tour) works exactly as before —
  `EventGroupId` is simply left null.
- The public event page shows "Part of: `<tour title>`" and an "other dates
  on this tour" list when applicable, with one extra read (`GET
  /v1/event-groups/{id}` + `GET /v1/events?eventGroupId=`) — no change to
  the buyer's core browse → hold → checkout → ticket flow.
- No tour-level shared banner/description — a reader might expect a tour to
  have its own promotional page; it doesn't in this pass, only a title used
  for clustering/navigation.
- No bundle/package purchase across legs yet — a buyer must check out each
  city/date separately. Flagged as a known gap, not an oversight.
- No venue dedupe/merge tooling (same gap `Venue` already had, now moot
  since there's no separate venue record to dedupe) — each event's location
  is entered independently, even for two legs at the same physical building.
- Editing an event's location after creation remains out of scope —
  `UpdateEventDetails` stays limited to description/category/dates/media,
  same as before this pass.

## Alternatives considered

- **A new `EventOccurrence`/session sub-entity under `Event`, each with its
  own seat map** — rejected; this is the heavier alternative described in
  Context. It would require Inventory's `EventPublished` subscription,
  `InventoryItem.EventId`, Ordering's checkout, and Ticketing's ticket
  issuance to all key off a finer occurrence id instead of `Event.Id` — a
  cross-service migration for no benefit over the chosen design, since each
  leg already needs to be independently sellable and `Event` already models
  exactly that.
- **Keep `Venue` but make `VenueId` optional** — rejected per explicit
  direction ("we dont require venue"); once location is captured inline per
  event, a separate reusable-venue directory adds an entity/UI surface
  nobody asked for.
- **Free-text location string** instead of structured fields — rejected;
  explicit direction was structured fields, matching what `Venue` already
  validated (required name/address/city/country, optional region/postal/
  geo).
- **Build bundle/package purchase now** — rejected for this pass; it's a
  separate, larger feature (multi-event order + new pricing concept in
  Ordering) than "make each date independently sellable," which was the
  confirmed priority.

## References

- `services/catalog/CLAUDE.md` — updated "Owns" section.
- `docs/adr/0018-media-service-and-blob-storage.md` — the now-superseded
  `Venue`-referencing work this ADR replaces (0018 itself is about Media/
  blob storage and is unaffected; only its Venue-adjacent context is
  superseded here).
- `services/inventory/CLAUDE.md`, `services/ordering/CLAUDE.md`,
  `services/ticketing/CLAUDE.md` — confirm the per-`Event` seat map /
  inventory / checkout contract this ADR deliberately left untouched.
