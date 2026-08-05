# ADR-0024 — Scan hardening, tour/leg date invariants, and entry gates

- **Status:** Accepted
- **Date:** 2026-08-05

## Context

Hands-on testing surfaced four gaps in the live-event flow:

1. `POST /v1/tickets/scan` (`Ticketing.Api/Endpoints/TicketingEndpoints.cs`)
   only checked the ticket's token and tenant — a ticket bought for one event
   could be checked in at any other event's gate, and check-in worked at any
   time regardless of the event's actual schedule.
2. Tour/leg date rules were unenforced: a leg (`Event`)'s `[StartsAt, EndsAt]`
   could fall entirely outside its `EventGroup`'s advertised
   `[StartsAt, EndsAt]`; sibling legs of the same tour could have overlapping
   date ranges; `Event.BookingEndsAt` could be set later than the leg's own
   `StartsAt`, letting ticket sales continue after the show had already
   started.
3. There was no way for an organizer to define physical entry gates for an
   event's location or restrict a seat-map section to one.

Also found, incidental to the `CreateEvent` work: `CreateEventHandler` never
checked that a supplied `EventGroupId` actually belongs to the caller's own
tenant — an organizer could attach an event to another tenant's tour.

## Decision

### Tour/leg date invariants (Catalog)

`Event.UpdateDetails` gains a same-aggregate invariant:
`BookingEndsAt` must not be later than the event's own `StartsAt` (in
addition to the existing `BookingEndsAt > OnSaleAt` check). Because this
compares against `StartsAt`, which isn't part of `UpdateEventDetailsCommand`,
it's enforced both in the domain method (the ultimate guard) and — to avoid
an uncaught-exception-as-500, the same class of gap already flagged for
`DefineSeatMap`'s duplicate-section-name exception — as an early check in
`UpdateEventDetailsHandler`, returning a new
`UpdateEventDetailsOutcome.BookingCutoffAfterStart`.

Group-range containment and no-overlap between sibling legs are
cross-aggregate checks (`Event` + `EventGroup`), so they live in the
**Handler**, not FluentValidation — this codebase has no precedent for an
async/repository-backed validator (confirmed by grep), and the existing
cross-aggregate pattern is exactly this: fetch-then-check in the handler,
same as `DefineSeatMapHandler`'s tenant-ownership check or
`RegisterOrganizerHandler`'s email-uniqueness pre-check.
`CreateEventHandler` and `UpdateEventDetailsHandler` both:

- Reject with `EventGroupNotFound` (`CreateEvent` only — an
  `EventGroupId` can only be set at creation) when the group doesn't exist
  or isn't owned by the caller's tenant — the incidental ownership-check fix.
- Reject with `OutsideEventGroupRange` when the leg's dates fall outside a
  group's own `StartsAt`/`EndsAt` (when the group has set them — both are
  optional, since a tour may be created before every leg's dates are known).
- Reject with `OverlapsExistingLeg` when the leg's `[StartsAt, EndsAt]`
  overlaps another leg of the same group (strict interval overlap — legs
  that touch exactly at the boundary are allowed).

`CreateEventCommand` changes from `IRequest<Guid>` to
`IRequest<CreateEventResult>` (a `CreateEventOutcome` enum +
`CreateEventResult` record, mirroring `DefineSeatMapOutcome`/
`DefineSeatMapResult`'s exact shape) so these failures surface as clean 404/
409 responses instead of throwing. A new `IEventRepository.ListLegsForEventGroupAsync`
returns every leg of a group (any status, any tenant) for this
validation-only use — never exposed to a caller directly.

### Entry gates (Catalog: definition; Ticketing: enforcement)

A new `EntryGate` entity (`Id`, `EventId`, `TenantId`, `Name`) belongs to an
`Event`. `Seat` and `GeneralAdmissionSection` each gain a nullable
`EntryGateId`, set once at seat-map-definition time
(`SeatMap.AddReservedSection`/`AddGeneralAdmissionSection` gain a trailing
`entryGateId` parameter) — restriction is **by section/tier only**, not a
per-ticket override (confirmed via `AskUserQuestion`); a section with no
gate set may be entered through any gate. `DefineSeatMapHandler` validates
every section-referenced gate id belongs to the event before generating the
seat map (`DefineSeatMapOutcome.EntryGateNotFound` on a bad id — the seat
map is otherwise immutable once created, so this is the only chance to
catch it). New `POST`/`GET /v1/events/{eventId}/entry-gates`
(`CreateEntryGateEndpoint` auth+tenant-owned, `ListEntryGates`
`.AllowAnonymous()` — a gate name alone reveals nothing sensitive, and
Ticketing's cross-service scan-time read needs to reach it without an
organizer token).

**Enforcement is a live cross-service read at scan time, not data
propagated ahead of time.** `BookingEndsAt`/`OnSaleAt`/`MaxTicketsPerBuyer`
are propagated from Catalog to Inventory via `EventPublished` because they
gate the hot hold-placement path, which cannot afford a synchronous
cross-service call per hold. A ticket scan is the opposite case: a
low-frequency, latency-tolerant admin action, not the flash-sale hot path —
so `Ticketing`'s new `ICatalogEventClient`/`DaprCatalogEventClient` reads
the event's window (`GET v1/events/{id}`) and per-section gate map
(`GET v1/events/{id}/seatmap`) live via Dapr service invocation, mirroring
`Inventory.Infrastructure/DaprSeatMapClient.cs`'s existing pattern exactly.
For a general-admission ticket, `Ticket.GeneralAdmissionAllocationId` is
Inventory's own id, not Catalog's section id, so a second client
(`IInventoryGaClient`/`DaprInventoryGaClient`) resolves it via Inventory's
already-existing, already-anonymous
`GET v1/events/{id}/inventory/general-admission` endpoint (added earlier
this session for the GA-allocation-id fix) — no new Inventory code needed.
Propagating gate/window data through Inventory → Ordering → Ticketing's
schemas instead (the scale of the original Reserved-vs-GA effort) was
rejected as disproportionate for an occasional gate-scan check.

`ScanTicketRequest` gains a required `EventId` and optional `GateId`.
`ScanTicketAsync` now checks, in order: the ticket's tenant (unchanged),
`ticket.CatalogEventId == request.EventId` (same 404 shape as an unknown
token — a wrong-event scan shouldn't reveal the token is valid for *some*
event), the check-in window (`DoorsOpenAt` falling back to `StartsAt`,
through `EndsAt` — confirmed via `AskUserQuestion`), then the gate: if the
ticket's resolved section has a gate restriction *and* the scanning
request supplies a `GateId` that doesn't match, reject; an unscoped scanner
(`GateId` omitted) always bypasses a section's restriction — a deliberate
"floor supervisor" posture, not an oversight.

## Consequences

- A ticket can no longer be checked in at the wrong event's gate, before
  doors open, or after the event has ended.
- An organizer can restrict physical gates by seat-map section; a
  supervisor-scoped "any gate" scanner remains available for override.
- `Ticketing.Infrastructure` makes its first outbound Dapr calls (previously
  Dapr-subscriber-only) — `Dapr.Client` added to
  `Ticketing.Infrastructure.csproj`; `DaprClient` itself needed no new
  registration since `AddOutbox<TicketingDbContext>()` already registers it
  (`EventPlatform.Messaging/MessagingExtensions.cs`).
- `CreateEventCommand`'s return type changed from `Guid` to
  `CreateEventResult` — every call site (one: `CatalogEndpoints.CreateEventAsync`)
  was updated to switch on the new outcome.
- A tour's legs can no longer be created or edited (via `UpdateEventDetails`)
  outside the tour's own advertised date range, or overlapping a sibling
  leg's dates — enforced server-side and mirrored client-side
  (inline `Form.Item` validators in `CreateEventPage.tsx`/
  `AdminEventDetailPage.tsx`), though the no-overlap check is
  server-authoritative only (it needs the full sibling list and is
  inherently race-prone — the same reasoning already applied to
  remaining-availability checks in the earlier frontend-validation-parity
  pass).
- Two new schema additions in Catalog (`entry_gates` table, nullable
  `entry_gate_id` columns on `seats`/`seat_map_ga_sections`) — a local dev
  database rebuild is required (`./scripts/dev-down.sh -v && ./scripts/dev-up.sh`).

## Alternatives considered

- **Propagate `DoorsOpenAt`/`EndsAt`/gate assignment through Inventory →
  Ordering → Ticketing's schemas** (a new `EventPublished` field, a new
  Ticketing-owned settings table, gate ids threaded through `Hold`/
  `OrderLine`/`TicketIssued`) — rejected: this is the same order of
  cross-service schema-widening effort as the original Reserved-vs-GA
  initiative, wildly disproportionate for a per-scan check that tolerates a
  live read.
- **Per-ticket gate override, independent of section** — rejected for this
  pass per explicit direction; by-section restriction only.
- **Denormalize the scan-time Catalog/Inventory reads into a local
  Ticketing cache** — rejected for now; revisit only if scan-time latency or
  upstream availability under real load becomes a measured problem, not
  preemptively.

## References

- `services/catalog/CLAUDE.md`, `services/ticketing/CLAUDE.md` — updated
  design-notes sections.
- ADR-0020/ADR-0021 — the `BookingEndsAt`/`OnSaleAt`/`MaxTicketsPerBuyer`
  propagate-ahead-of-time precedent this ADR deliberately diverges from for
  the scan-time gate/window checks.
