# ADR-0021 — Ticket delivery email, check-in/scan, and per-buyer ticket limits

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

Following up "focus on dev first" (deprioritizing the Identity/OTP buyer-auth
work captured separately in `docs/progress-tracker.md` as P2), three concrete
gaps and one new feature request were confirmed:

1. **Ticket check-in was unreachable.** `Ticket.CheckIn()` was fully
   implemented in the domain, but no endpoint anywhere called it — there was
   no scan/check-in route at all.
2. **Ticket delivery email was half-built.** Communication's
   `IntegrationEventNotificationHandler` always recorded every
   `TicketIssued`/`OrderConfirmed` delivery as `Skipped`, because its only
   `IRecipientResolver` implementation (`UnavailableRecipientResolver`)
   always returns `null` — no service anywhere persists a buyer's email; it
   only ever existed transiently inside the dev-login JWT's `email` claim.
   Since buyers won't necessarily authenticate via that dev-JWT flow going
   forward (mobile+OTP instead, per P2), the buyer now simply provides an
   email at checkout — a plain required field, not derived from any token
   claim. Also confirmed: **one combined order email** listing every ticket,
   not one email per ticket (even though `TicketIssued` still fires once per
   ticket) — a real batching gap Communication's own `CLAUDE.md` already
   flagged as unsolved.
3. **Archive/purge for consumed tickets** — design captured only (see
   Consequences), nothing built this pass.
4. **Organizer-configurable per-buyer ticket limits** — a per-order-only cap
   is trivially bypassed by placing several separate orders, so real
   enforcement sums a buyer's existing commitment for the event across all
   their holds (active + converted, not just the current request).
   `Hold.UserId` already exists, so this needs no identity work to be
   meaningful.

## Decision

### Buyer email at checkout (Ordering)

`POST /v1/checkout`'s `CheckoutRequest` gains a required `BuyerEmail`,
validated present and well-formed in `OrderingEndpoints.CheckoutAsync` (the
same manual-check style already used for the `Idempotency-Key` header —
Ordering has no FluentValidation pipeline to extend). Threaded through
`CheckoutWorkflowInput` → `CreateOrderInput` → `Order.Create(...)` (new
nullable `BuyerEmail` column, `HasMaxLength(320)`) → read back in
`ConfirmOrderActivity` (which already loads the `Order` entity) and appended
as a new trailing field on `OrderConfirmed`. No new service, no new table
beyond one nullable column — the same "widen with a field" pattern already
used for `OrderLine`/`Ticket`/`LedgerEntry`.

### One combined ticket-delivery email (Ticketing → Communication)

New contract `EventPlatform.Contracts.Ticketing.OrderTicketsIssued` (+
`IssuedTicketSummary`), carrying `OrderId`, `CatalogEventId`, `UserId`,
`BuyerEmail`, and every minted ticket. `TicketIssuingService.IssueAsync`
gains a `buyerEmail` parameter and, **after** its existing per-ticket loop
(which keeps enqueuing today's `TicketIssued` unchanged), enqueues **one**
`OrderTicketsIssued` with the complete ticket list it already built in that
same call — no cross-event correlation/counting needed, since Ticketing
already knows the full set at the point it finishes minting.

Communication adds a new handler method,
`IntegrationEventNotificationHandler.HandleOrderTicketsIssuedAsync`
(alongside the two existing ones, same dedup-via-`ProcessedNotificationEvent`
shape) that, when `BuyerEmail` is present: renders a new `order-tickets`
embedded Scriban template (the ticket list is pre-formatted into one
multi-line placeholder string, since the renderer only takes a flat
`IReadOnlyDictionary<string,string>`) and sends via `IEmailSender`
**directly** — not through `NotificationSendService`, and not through
`IRecipientResolver` (the email already arrived on the event; that port
stays reserved for the future OTP/Identity use case). `NotificationSendService`
is bypassed because it does its own internal `SaveChangesAsync`, which would
leave a window between "delivery logged" and "event marked processed" where
a crash could cause an at-least-once redelivery to genuinely double-send —
this handler instead writes the delivery-log row **and** the
processed-event marker in one `SaveChangesAsync`, matching the same
one-transaction rigor `ProcessedWebhookEvent` already uses in Payments. When
`BuyerEmail` is absent (shouldn't happen once checkout requires it, but
defensive), falls back to recording a `Skipped` row. The dev/logging
`IEmailSender` — already fully built, logging the complete
`toAddress`/`subject`/`htmlBody` and returning a synthetic success — **is**
the mock; no new sender needed, just newly wired to actually fire.

### Ticket check-in / scan (Ticketing)

`Ticket.cs` gains `CheckedInAt: DateTimeOffset?`, set by `CheckIn()`
alongside the existing status transition. `ITicketRepository`/
`TicketRepository` gain `GetByTokenAsync` (the DB already has a unique
index on `Token`, just no repository method exposing it). New
`POST /v1/tickets/scan` endpoint (body `{ token }`, tenant-checked against
`Ticket.TenantId` — the same pattern `DefineSeatMap`/`PublishEvent` already
use for tenant-owned actions) looks up by token, calls `CheckIn()`, and
returns 200 with the ticket (404 unknown token, 409 already-checked-in/void
via the existing `InvalidOperationException` message). `TicketResponse`
gains `CheckedInAt`. Frontend gets `features/admin/tickets/ScanTicketPage.tsx`
at `/admin/scan`, in `AdminLayout`'s nav.

### Organizer-configurable per-buyer ticket limit (Catalog → Inventory)

`Event` gains `MaxTicketsPerBuyer: int?` (`null` = no limit), settable at
`Create` and editable via `UpdateDetails` (Draft-only, same lifecycle as
every other detail field — no post-publish edit in this pass, same
reasoning as `BookingEndsAt`). Propagated to Inventory exactly the way
`BookingEndsAt` already is: `EventPublished` gains `MaxTicketsPerBuyer`,
`EventInventorySettings` gains the same field. The previously dead-code
`EventInventorySettings.UpdateBookingCutoff` method (confirmed unreachable,
since `ExistsForEventAsync` short-circuits
`InventoryProvisioningService.ProvisionAsync` before it's ever reached on
redelivery) is renamed to a symmetrical `Update(bookingEndsAt,
maxTicketsPerBuyer)` for consistency — left equally unreachable; not a
regression, not this pass's problem to fix.

New `IInventoryRepository.GetBuyerCommittedQuantityAsync(eventId, userId)`,
implemented with the same explicit `dbContext.Set<HoldItem>()`/
`dbContext.Set<HoldGeneralAdmissionItem>()` join-to-`Hold` style
`GetReconciliationStateAsync` already uses, summing seat count plus GA
quantity across the buyer's `Active` **and** `Converted` holds for the
event (`Released` holds don't count — freed back up; `Converted` must
count, or someone could re-buy after checkout completes).
`HoldService.PlaceHoldAsync` checks this immediately after the existing
`BookingEndsAt` check: if `alreadyCommitted + requestedQuantity >
MaxTicketsPerBuyer`, fails with a new `PlaceHoldOutcome.BuyerLimitExceeded`
→ mapped to `409 Conflict`, same treatment as `BookingWindowClosed`.

## Consequences

- Buyers now receive one combined, mocked (dev-log sender) ticket-delivery
  email per order, and gate staff can check a ticket in via
  `POST /v1/tickets/scan` — both previously entirely missing.
- Organizers can cap tickets per buyer per event; enforcement is real
  (server-side, cumulative across a buyer's holds) rather than a
  per-order-only cap that's trivially bypassed.
- `BuyerEmail` is a plain checkout-time input field, unrelated to how the
  buyer authenticated — this pass does not touch buyer login/identity (P2
  remains separately deferred).
- No QR code image generation — the email includes the raw opaque token text
  only, matching what `TicketResponse`/the order/ticket pages already show.
- No real ACS/Twilio email sending — the dev/logging sender is the mock by
  design; no vendor credentials touched.
- Ticketing still has no `tests/` project — a pre-existing gap, not fixed in
  this pass (matches the same gap in Ordering/Catalog).
- **Archive/purge for consumed tickets — design only, nothing built.**
  Phased approach captured in `docs/progress-tracker.md`'s Deferred table as
  P3: **Phase 1** a same-DB `ticket_archive` table plus an
  organizer-triggered "archive this event's tickets" action, triggered
  time-based-by-event (not per-ticket-at-scan-time, since a checked-in
  ticket may still be needed for same-day disputes/re-entry). **Phase 2** an
  automatic sweep via a `BackgroundService` using the exact `PeriodicTimer` +
  scoped-`IServiceScopeFactory` shape `ExpiredHoldReaper` already
  establishes, triggered by a retention window past `Event.EndsAt` (needs
  `EndsAt` denormalized onto `Ticket` at issuance to avoid N cross-service
  calls to Catalog per sweep). **Phase 3** swaps the archive table for
  blob/cold storage once storage cost justifies it — the same
  port-plus-swappable-adapter pattern already used for
  `IPaymentGateway`/`IEmailSender`.

## Alternatives considered

- **Deriving the buyer's email from the dev-login JWT's `email` claim**
  instead of a checkout-time field — rejected; buyers won't necessarily use
  that dev-JWT flow once real mobile+OTP auth exists (P2), so a field
  decoupled from the auth mechanism is the more durable choice.
- **One email per ticket** (matching `TicketIssued`'s existing per-ticket
  cadence) — rejected in favor of one combined order email; a buyer
  purchasing multiple seats/GA admissions should get one email, not a flood.
- **Routing the combined email through `NotificationSendService`** — rejected;
  its internal `SaveChangesAsync` would split "delivery logged" from "event
  marked processed" into two transactions, opening a double-send window on
  redelivery. The direct-send handler path keeps both in one transaction.
- **A per-order-only ticket limit** — rejected; trivially bypassed by
  placing multiple separate orders. Cumulative enforcement across a buyer's
  holds for the event closes that gap with no new identity dependency.
- **Building the archive/purge pipeline now** — explicitly deferred per
  discussion; captured as a phased design (P3) rather than built, to keep
  this pass focused on delivery/check-in/limits.

## References

- `services/catalog/CLAUDE.md`, `services/inventory/CLAUDE.md`,
  `services/ordering/CLAUDE.md`, `services/ticketing/CLAUDE.md`,
  `services/communication/CLAUDE.md` — updated "Owns"/design-notes sections.
- `docs/progress-tracker.md` — the P3 archive/purge design capture.
- ADR-0016 — Communication's architecture (ports, dedup ledger, deferred
  recipient resolution) this ADR builds the new handler path on top of.
- ADR-0020 — the polymorphic seat/GA widening pattern (`OrderLine`/`Ticket`/
  `LedgerEntry`) this ADR follows again for `Order.BuyerEmail`/
  `Event.MaxTicketsPerBuyer`.
