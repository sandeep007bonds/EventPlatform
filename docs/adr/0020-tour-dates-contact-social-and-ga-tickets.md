# ADR-0020 — Tour dates, enforced booking cutoff, contact/social, and Reserved-vs-General-Admission tickets

- **Status:** Accepted
- **Date:** 2026-07-31

## Context

Four follow-on requirements were gathered after ADR-0019 shipped `EventGroup`
(tours) and inline event location, each confirmed explicitly:

1. A tour has its own overall advertised date range (`EventGroup.StartsAt`/
   `EndsAt`), separate from each leg's own specific run dates
   (`Event.StartsAt`/`EndsAt`).
2. `Event.EndsAt` should be required at creation, not optional-set-later —
   every leg fundamentally has a date range, not just a start instant.
3. The booking-cutoff field (`Event.OffSaleAt`, previously display-only) must
   become **enforced**: "after that date, tickets will not be sale." This
   surfaced a real cross-service gap — holds are placed through **Inventory**,
   not Catalog, so Inventory has to learn the cutoff too. Renamed to
   `BookingEndsAt` to reflect that it now carries real behavior, distinct from
   the still-display-only `OnSaleAt`.
4. Contact details + social links per event (phone/mobile/email/website plus
   an open-ended list of social platform links), confirmed as: an open list
   (not fixed platform columns, so a new platform never needs a schema
   change), set at the tour level as a default, **with a per-leg override**.
5. Reserved (seated, row/seat-based) vs. General Admission (capacity-only, no
   individual seats) as a **per-section** allocation choice within one seat
   map — confirmed core to this launch, not deferred. Investigated in depth
   because it is not just a Catalog field: every layer of the purchase
   pipeline was hard-wired to "one sellable unit = one seat"
   (`Inventory.Domain.InventoryItem`/`HoldItem`, `Ordering.Domain.OrderLine`,
   `EventPlatform.Contracts.Ordering.OrderConfirmed`'s `SeatIds`,
   `Ticketing.Domain.Ticket`/`TicketIssued`, `Catalog.Domain.SeatMap`'s
   seat-only section generation).
6. A virtual waiting-room/queue system (Ticketmaster/Queue-it-style) was
   discussed and explicitly **deferred**, documented separately in
   `docs/progress-tracker.md`'s "Deferred — Product features" table as
   opt-in per event, not applied to every event by default. Untouched by this
   ADR.

## Decision

### Tour and leg dates, contact/social

- `EventGroup` gains `StartsAt`/`EndsAt` (`DateTimeOffset?`, both independent
  of any leg's dates — not derived) and `ContactPhone`/`ContactMobile`/
  `ContactEmail`/`WebsiteUrl` plus a child `EventGroupSocialLink` collection
  (`Platform` free text + `Url` — the open-list design). `EventGroup` gets its
  first `Update(...)` method; it previously only had `Create`.
- `Event.EndsAt` moves from optional (set later via `UpdateDetails`) to a
  **required** `Create(...)` parameter, with its `EndsAt > StartsAt`
  invariant moving from `UpdateDetails` into `Create`. Still editable
  afterward via `UpdateDetails` (Draft-only, unchanged lifecycle) — just no
  longer optional at any point.
- `Event.OffSaleAt` renamed to `BookingEndsAt` everywhere (domain, EF
  config, response DTOs, commands, frontend). `OnSaleAt` is unchanged —
  still display-only.
- `Event` gains its own `ContactPhone`/`ContactMobile`/`ContactEmail`/
  `WebsiteUrl` + `EventSocialLink` collection, settable via `UpdateDetails`
  (same Draft-only panel as description/category/media). **Override
  semantics, resolved at the read layer, not duplicated into storage**: if
  the leg has any `EventSocialLink` rows or any contact field set, its own
  values are used; otherwise the read falls back to the owning
  `EventGroup`'s defaults. Implemented via static mapper classes
  (`EventResponseMapper`, `EventGroupResponseMapper`) rather than repeating
  the fallback logic in every handler.
- **`BookingEndsAt` stays settable only pre-publish**, via the same
  Draft-only `UpdateDetails` lifecycle as every other event detail field.
  Changing the cutoff *after* publish is explicitly out of scope for this
  pass (it would need a new post-publish command plus wiring the currently
  zero-consumer `EventUpdated` event into Inventory — a materially separate
  addition, listed under Consequences).

### Reserved vs. General Admission

- New `AllocationType` enum (`Reserved | GeneralAdmission`) makes the
  seat/capacity choice a **per-section** property, not a whole-event toggle
  — `SeatMap.AddReservedSection(...)` (today's exact seat-generation
  behavior, unchanged) sits alongside a new
  `AddGeneralAdmissionSection(...)` that creates a `GeneralAdmissionSection`
  child entity (stable `Id`, `SectionName`, `PriceTier`, `PriceAmount`,
  `Capacity`) with no individual `Seat` rows. **A single seat map can mix
  both** — e.g. reserved orchestra seating plus a GA standing section in the
  same event, matching how real venues actually work.
- Inventory gets a parallel, counter-based `GeneralAdmissionAllocation`
  entity (mirrors `InventoryItem`'s shape: `TotalCapacity`/`HeldCount`/
  `SoldCount`/`Version` instead of a per-seat row) and a new
  `EventInventorySettings` entity (`EventId` PK, `BookingEndsAt`) — both
  provisioned idempotently from `EventPublished`, which now carries
  `BookingEndsAt` as its cheapest existing hook into Inventory.
- **Redis fast gate for GA capacity is fail-closed** (a capacity key that
  was never initialized, or was lost to a flush, reads as zero remaining) —
  the deliberate *opposite* of the sparse seat model's fail-open default
  (a missing seat key means available). This is safe either way because
  Postgres remains the final authority regardless of what Redis says; GA
  capacity must be explicitly initialized at provisioning time, unlike
  seats, which need no seeding. New Lua scripts
  (`TryHoldGeneralAdmissionScript`/`ReleaseGeneralAdmissionScript`/
  `MarkGeneralAdmissionSoldScript`) mirror the exact atomic
  check-and-set structure of the existing per-seat scripts, operating on a
  remaining-capacity counter instead of a per-seat key.
- `HoldService.PlaceHoldAsync` checks the enforced booking cutoff first —
  before touching Redis or Postgres — returning a new
  `PlaceHoldOutcome.BookingWindowClosed` if `DateTimeOffset.UtcNow` has
  passed `EventInventorySettings.BookingEndsAt`.
- **A hold now covers seats and/or general-admission quantities together**
  in one request/aggregate: `Hold` gains a parallel
  `GeneralAdmissionItems` collection (`HoldGeneralAdmissionItem`) alongside
  the existing `Items`. The checkout saga's convert call needed **no
  change** — it already only sends `{ orderId }` and resolves everything
  server-side, confirmed by re-reading `ConvertActivity`/the convert
  endpoint before starting this work.
- **Polymorphic widening, not split types, applied consistently down the
  whole pipeline** — the same pattern already used for `LedgerEntry`
  (nullable `GeneralAdmissionAllocationId`/`Quantity` alongside nullable
  `InventoryItemId`) is repeated in:
  - `Ordering.Domain.OrderLine`/`OrderLineSpec`: nullable
    `InventoryItemId`/`SeatId` alongside nullable
    `GeneralAdmissionAllocationId` + `Quantity` (always 1 for a seat line) +
    `UnitPriceMinor` (new, alongside the existing line-total `PriceMinor`,
    now meaningful once a line can represent more than one unit).
  - `EventPlatform.Contracts.Ordering.OrderConfirmed`: `SeatIds:
    IReadOnlyList<Guid>` replaced with `Lines: IReadOnlyList<OrderLineSummary>`
    (each summary carrying either a `SeatId` or a
    `(GeneralAdmissionAllocationId, Quantity)` pair) — the one real breaking
    contract change in this pass, since a bare seat-id list can no longer
    describe a GA purchase.
  - `Ticketing.Domain.Ticket`: `SeatId` becomes nullable, paired with a
    nullable `GeneralAdmissionAllocationId` (exactly one set, enforced in
    `Ticket.Create`). `TicketIssuingService.IssueAsync` reads
    `OrderConfirmed.Lines` and mints one ticket per seat line, and
    `Quantity` tickets per GA line — each individually scannable, with no
    seat.
  - `EventPlatform.Contracts.Ticketing.TicketIssued`: same nullable
    `SeatId`/`GeneralAdmissionAllocationId` widening.
- This pattern was chosen over splitting into separate seat/GA
  types/tables in every case, for the same reason it was chosen for
  `LedgerEntry`: one unified shape to read and reason about, no
  parallel-table joins, and callers that only ever deal with seats (nothing
  changes for them — `Quantity` is simply always 1).

## Consequences

- Organizers can launch a mixed reserved-seat + general-admission event in
  one seat map, and buyers can hold/checkout a mix of both in one purchase —
  this was flagged up front as "the biggest piece of this whole
  conversation" because it forks the no-oversell mechanism across four
  services; it is now load-bearing infrastructure, not a Catalog-only field.
- A booking cutoff set before publish is genuinely enforced at hold time,
  closing a real gap (`OffSaleAt` was previously decorative).
- `InventoryReconciler` is **not** extended for GA counters in this pass — a
  GA capacity key lost to a Redis flush degrades fast-path availability
  (Redis under-reports remaining capacity as zero) until the next
  successful hold/release touches it, but never causes oversell, since
  Postgres's `GeneralAdmissionAllocation.Hold(quantity)` check is
  unconditional regardless of Redis state. This mirrors the existing
  seat-reconciliation safety property; extending the reconciler to rebuild
  GA counters after a flush is future work, not a correctness gap.
- Changing `BookingEndsAt` after publish remains out of scope — needs a new
  post-publish command plus wiring the currently-unconsumed `EventUpdated`
  event into Inventory. Flagged as a recommendation, not built here.
- `SeatHeld`/`SeatReleased`/`SeatSold` (Inventory's own published events)
  stay seat-only in this pass — they have zero external consumers today
  (confirmed by repo-wide search), so widening them for GA holds was not
  worth doing alongside the real, consumed contract change
  (`OrderConfirmed`). Revisit if a consumer for these ever appears.
- No Inventory endpoint exposes a GA allocation's live *remaining* capacity
  to the buyer UI yet — the frontend's quantity stepper is capped by the
  section's *total* capacity (from Catalog's seat map), with the real
  enforcement happening server-side at hold time (a 409 surfaces if
  oversold, same graceful-degradation pattern already used for seat
  conflicts). A dedicated read endpoint is straightforward future work, not
  built here.
- No UI exists yet to edit an already-created tour's dates/contact/social
  defaults (`UpdateEventGroupCommand`/endpoint exist and are reachable via
  API, but only `CreateEventGroupPage` — not a separate edit page — sets
  them from the UI, immediately after creation). Editing a tour's
  organizer-level defaults later is a small, contained future addition.
- The virtual waiting-room/queue system remains tracked separately in
  `docs/progress-tracker.md`, explicitly opt-in per event, untouched by this
  pass.
- Bundle/multi-leg package purchase across a tour remains deferred per
  ADR-0019, untouched here.

## Alternatives considered

- **A whole-event Reserved-vs-GeneralAdmission toggle** instead of a
  per-section choice — rejected; real venues mix reserved and GA sections in
  the same event (e.g. reserved floor seating plus GA standing), and a
  per-section property reuses the existing "one seat map per event"
  structure rather than inventing a parallel concept.
- **Splitting `OrderLine`/`Ticket`/`LedgerEntry` into separate seat and GA
  tables/types** — rejected in favor of nullable-field widening, consistent
  with the precedent already set for `LedgerEntry`. A split would require
  every caller to branch on type and would double the join surface for no
  benefit, since a seat line/ticket is structurally just "a GA line with
  quantity fixed at 1 and no allocation id."
- **Extending `InventoryReconciler` to rebuild GA counters after a flush in
  this same pass** — deferred; Postgres remains authoritative regardless
  (the same safety property already proven for seats), so this only affects
  fast-path availability post-flush, not correctness. Scoped out to keep
  this already-large pass contained.
- **Fail-open Redis default for GA capacity** (mirroring the seat model
  exactly) — rejected; a missing capacity key would then read as "available"
  with no bound, which is wrong for a pool that must be explicitly sized.
  Fail-closed is correct here specifically because GA capacity is always
  explicitly initialized at provisioning time, unlike seats.
- **Wiring `BookingEndsAt` changes into a post-publish flow now** — rejected
  for this pass; it requires wiring the currently-unconsumed `EventUpdated`
  event into Inventory, a materially separate addition from the rest of this
  already-large piece of work.

## References

- `services/catalog/CLAUDE.md`, `services/inventory/CLAUDE.md`,
  `services/ordering/CLAUDE.md`, `services/ticketing/CLAUDE.md` — updated
  "Owns"/design-notes sections.
- `docs/progress-tracker.md` — the separately-tracked, opt-in-per-event
  waiting-room/queue design sketch this ADR does not touch.
- ADR-0019 — the `EventGroup`/tour foundation this ADR builds dates and
  contact/social defaults on top of.
