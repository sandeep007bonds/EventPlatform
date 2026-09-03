# ADR-0039 — The performance, not the event, is what gets sold

**Status:** Accepted · **Date:** 2026-09-03

## Context

Everything downstream hung off an **event id**. Inventory provisioned per event, orders and tickets
named an event, a scan was validated against an event, and `Event` itself carried a single
`StartsAt`/`EndsAt` pair and one inline venue address.

That works exactly as long as an event happens once. It does not:

1. **A three-night run had to be three events.** Three pages, three slugs, three seat maps built by
   hand, three sets of ticket types and promo codes. Nothing in the model said they were the same
   show, so nothing could aggregate them — and a buyer browsing saw three entries that looked like
   different concerts.
2. **Any report over them would be wrong.** A sales figure "for the event" would be a figure for one
   night. Building Reporting on this grain would bake the error into a dimensional model, which is
   the expensive kind of wrong to unwind.
3. **The Venue service (ADR-0038) had no way in.** A venue's reusable, versioned seat map is
   pointed at by *something*, and an event with one date and one address is the wrong something —
   two nights of a run can use two different configurations of the same hall.
4. **Seats still carried prices in Catalog**, which ADR-0038 had just established they should not,
   because there was nowhere else to say which block sells as which ticket type.

The reference architecture makes `EventSession` its Phase 2 blocker for the same reasons, and its
inventory invariant is `UNIQUE(SessionId, SeatId)`.

## Decision

### 1. `EventSession` is the selling grain

An `Event` owns one or more `EventSession`s. The event is what is being sold and how it is marketed;
the session is one performance — its night, its venue, its seat map, its inventory.

Everything downstream re-keys to `EventSessionId`. `Event` keeps a denormalised
`FirstSessionStartsAt`/`LastSessionEndsAt` so the storefront can still list and filter by date in one
indexed scan rather than loading every session of every event.

An event is **created with its first performance**. One with none sells nothing, has no date to be
listed by, and cannot be checked against its tour's range — and the single-performance case, which
is most of them, then needs no second call.

Two performances of one event **cannot overlap**: one act cannot be on two stages at once, and the
aggregate is the only thing that can see all of them to check.

### 2. What sits at which level

Session: `StartsAt`, `EndsAt`, `DoorsOpenAt`, `BookingEndsAt`, `SalesPaused`, venue and seat map.
Event: `OnSaleAt`, `RequiresQueue`, `MaxTicketsPerBuyer`, currency, tax, fees, ticket types, promo
codes, policies, everything presentational.

The dividing question is whether the answer differs per night. A booking cutoff does — "book until
two hours before this show" is a different instant every night. An on-sale moment does not: a run
goes on sale once. A per-buyer limit must not, or one buyer takes the cap three times over on a
three-night run, which is the behaviour the limit exists to prevent — so Inventory keeps the event
id alongside the session id and counts across the run.

`RequiresQueue` stays on the event, which is why **Queue is untouched by this change**: one waiting
room gates one on-sale.

### 3. A performance names a Venue seat-map version; Catalog's seat map is deleted

`SeatMap`, `Seat`, `GeneralAdmissionSection`, `EntryGate` and `EventLocation` are removed from
Catalog, along with every seat-map slice and `TicketTypeResolver`. A session stores `VenueId`,
`SeatMapId` and `SeatMapVersionId`.

The version is **pinned**, not resolved at sale time: a published version is immutable, so pinning
is what stops a later venue reconfiguration moving the seats a sold ticket names.

A small `VenueSnapshot` (name, city, country, time zone) is cached on the session for display, and
is explicitly *only* for display — a venue renamed shows its old name on an event page until someone
touches the session, which is a far better failure than the event list not loading. Anything
decided from — seat identity, gates, capacity — is read live from Venue by id.

### 4. `SessionAllocation` carries the commercial overlay

ADR-0038 took the price off the seat. Something still has to say which block sells as which ticket
type, and it has to be per performance: Friday's Lower Tier can be Gold while Saturday's matinee
sells the same seats as Premium.

`SessionAllocation` binds a Venue **section or admission-area code** to a `TicketTypeId`. By code,
not by seat id: codes are stable across renames by design, and one row per block means a
60,000-seat stadium needs about twenty of them.

Changing a session's seat map **clears its allocations**. They bind to codes belonging to the old
version, and silently keeping the ones that happen to match would leave the rest missing without
saying so.

### 5. `EventPublished` splits in two

- `EventPublished` keeps only event-level facts. Queue is its one remaining consumer.
- `EventSessionPublished` is emitted **once per performance** and carries the seat map by id and
  version, the capacity, the window, and the priced allocation list inline.

The allocation list travels inline because it is one row per block — tens of rows even for a
stadium — so Inventory needs no call back to Catalog. The **seats** do not: a stadium plan is
megabytes, and a message bus is the wrong place to move it. Inventory reads them from Venue.

`EventSalesPaused`/`EventSalesResumed` gain an `EventSessionId`; pausing a whole event emits one per
performance, because Inventory has no way to expand "the event" into the nights it consists of.

### 6. Publishing validates, and reports every problem

`SessionPublishCheck` — shared by publishing an event and publishing one late-added performance —
requires that the map exists, the pinned version is still the published one, **every block has an
allocation**, and every allocated ticket type is active. Publishing an event is all-or-nothing
across its performances.

That third rule is the one worth stating: an unallocated block is not spare capacity, it is capacity
Inventory never hears about, and the map then renders with a hole a buyer cannot distinguish from a
sold-out section.

Failures come back as a **list**. An organizer fixing a three-night run needs all three problems at
once, not one refresh apart.

## Consequences

**Catalog and its contracts change together, so Catalog cannot compile alone.** Inventory and
Ticketing bind the old `EventPublished` shape; the solution builds again once they are re-keyed. The
work is deliberately split across commits for reviewability, not because each commit is independently
green.

**Every Catalog migration is dropped and regenerated.** The old ones describe tables that no longer
exist. This is safe here only because there is no production data.

**`PUT /v1/events/{id}/details` becomes `PUT /v1/events/{id}/selling-rules`.** What is left after the
dates and venue moved out is the money and the selling rules, and the route now says so.

**The buyer journey grows a step.** `/events/{slug}` has to offer a choice of performance when there
is more than one, and every hold, order, ticket and scan has to carry which night. That is frontend
work, tracked as its own landing.

**Ticket types are not yet phased.** `SessionAllocation` makes per-performance pricing expressible
by pointing two sessions at different types, which is most of what price phases are wanted for.
Time-boxed price *phases* within one type remain future work.

**Event cancellation still does not refund.** `EventSessionCancelled` says a performance was called
off and nothing more. Working out who bought what and giving their money back is a saga with
approval and compensation in it, not a side effect of a status change.

## Closes

EVENT-005 · ORDER-013 · INV-007 · GATE-001 (session validation half) · and unblocks REPORT-003's
`DimSession`. Advances EVENT-013 (a performance can now be cancelled) and PRICE-010 / INV-012, whose
Inventory half lands with the re-key.
