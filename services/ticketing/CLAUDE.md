# CLAUDE.md — Ticketing service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Issues tickets when an order is confirmed — one ticket per sold seat, plus one
ticket per general-admission unit purchased, each with an opaque scan token
(encoded as a QR at the edge). Bounded context: **Ticketing** (ADR-0008). The
aggregate is `Ticket`; the context is named `Ticketing` so the type never
clashes with its namespace.

## Owns

- **Data store:** PostgreSQL `ticketing` DB (this service only)
- **Public API:** `GET /v1/orders/{id}/tickets`, `GET /v1/tickets/{id}`,
  `GET /v1/tickets/{id}/qrcode` (auth: the ticket's own buyer, or the owning
  tenant — a PNG QR code encoding the ticket's scan token, generated on
  demand via `QRCoder`; see ADR-0025),
  `GET /v1/sessions/{eventSessionId}/tickets`,
  `POST /v1/tickets/scan` (tenant-owned, body `{ token, eventSessionId, gateId? }`
  — looks up by the opaque scan token; 404 on an unknown token *or* a token
  whose `Ticket.EventSessionId` doesn't match `eventSessionId` (same shape,
  deliberately — a ticket for the wrong night shouldn't reveal the token is
  valid on another one); 409 outside that performance's check-in window,
  presented at the wrong gate, or already-checked-in/void; otherwise calls
  `Ticket.CheckIn()`
  (see ADR-0024) — every check is a purely local read, see below (ADR-0025);
  internal (saga-only, not gateway-routed)
  `POST /v1/orders/{orderId}/tickets/void` — voids every ticket for an order,
  called by Ordering's cancellation saga
- **Events published:** `TicketIssued` (via outbox, one per ticket — unchanged),
  `OrderTicketsIssued` (via outbox, one per order, enqueued once all of the
  order's tickets are minted — see ADR-0021)
- **Events consumed:** `OrderConfirmed` (Ordering) → issue tickets;
  `EventSessionPublished` (Catalog, one per performance) → warm the local scan
  cache (see below, ADR-0025)

## The scan is validated against the performance (ADR-0039)

A ticket names an `EventSessionId`, and so does the scan. This is the difference
between a scanner that works at a three-night run and one that does not: the
check-in window is a different pair of instants every night, and Friday's ticket
must be refused at Saturday's door. `Ticket.CatalogEventId` is still carried,
but only so a ticket can say which run it belongs to — nothing in the scan path
decides anything from it.

## Design notes

- **Reserved seat vs. general admission:** `Ticket.SeatId` and
  `Ticket.GeneralAdmissionAllocationId` are both nullable — exactly one is set
  (enforced in `Ticket.Create`). `TicketIssuingService.IssueAsync` reads
  `OrderConfirmed.Lines` (each line's `Quantity` — always 1 for a seat line) and
  mints that many tickets per line, so a general-admission line becomes several
  individually-scannable tickets with no seat.
- **Idempotent issuance:** re-delivery of `OrderConfirmed` is a no-op once an order
  is ticketed (unique index on `(order_id, seat_id)` + a pre-check — Postgres
  treats each `NULL seat_id` as distinct, so multiple general-admission tickets
  per order don't collide on that index).
- **`TicketVoidingService.VoidByOrderAsync`** (buyer-initiated cancellation,
  called by Ordering's cancellation saga): all-or-nothing across an order's
  tickets, mirroring `SeatBlockingService`'s "all-or-nothing across the
  requested seats" precedent — any ticket already `CheckedIn` blocks voiding
  the whole order (a buyer can't cancel out from under a ticket they already
  used), never a partial void.
- **Scan token:** a 128-bit CSPRNG token (`RandomNumberGenerator`), unique per
  ticket. The QR encodes the token; the gate scans it. See tracker T-ticket-token
  for signing/rotation before production. `Ticket.CheckedInAt` (nullable,
  set by `CheckIn()`) is the audit timestamp `POST /v1/tickets/scan` exposes.
- **One combined ticket-delivery email per order.** `TicketIssuingService.IssueAsync`
  takes an optional `buyerEmail` (read from `OrderConfirmed.BuyerEmail`) and,
  after minting every ticket for the order, enqueues one `OrderTicketsIssued`
  carrying the buyer's email and the full ticket list — Communication renders
  and sends this directly, rather than reconstructing "all tickets issued" by
  counting per-ticket `TicketIssued` events (ADR-0021).
- **Scan-time window/gate checks are purely local reads — zero cross-service
  calls — because the scan cache is warmed once per performance, not once per
  scan (ADR-0025, superseding ADR-0024's original live-call design once the
  real mega-event throughput requirement was known).**
  `SessionScanContextProvisioningService` runs once per performance, triggered
  by a subscription to Catalog's `EventSessionPublished` (this service's second
  subscription, after `OrderConfirmed`): it persists `SessionScanContext`
  (check-in window: `DoorsOpenAt` falling back to `StartsAt`, through
  `EndsAt` — all carried directly on the message, no Catalog call
  needed for this part at all), calls `IVenueGateMapClient`
  (`DaprVenueGateMapClient`, this service's first *outbound* Dapr call) once
  against the **Venue** service for the pinned seat-map version's gate
  assignments (persisted as `SeatEntryGate` rows), and calls
  `IInventoryGaClient` (`DaprInventoryGaClient`) once for every
  general-admission allocation's area-to-gate mapping (persisted as
  `GaAllocationGate` rows — a bounded retry covers the race against
  Inventory's own async provisioning off the same `EventSessionPublished`
  message). `ScanTicketAsync` then only ever reads these three local,
  indexed tables — no dependency on Venue/Inventory being reachable or
  fast at the moment of an actual scan. Deliberately Postgres-backed, not
  Redis: this data is read-only and immutable once warmed, and a fresh/
  scaled-out pod needs no pub/sub "catch-up" the way an in-memory-only
  cache would.
  **The gate map is read from the version, not the map.** Venue seat maps are
  versioned and immutable once published, and the performance pins one — so the
  gates a scan enforces are the gates that were in force when the tickets were
  sold, even after the venue publishes a new layout.

## Structure

`Ticketing.Api` (host + endpoints + Dapr subscriptions + QR-code generation)
· `Ticketing.Application` (issuing + scan-context provisioning + ports) ·
`Ticketing.Domain` (`Ticket`, `SessionScanContext`, `SeatEntryGate`,
`GaAllocationGate` + invariants) · `Ticketing.Infrastructure` (EF Core +
Postgres, outbox, the once-per-performance Venue/Inventory Dapr clients).
`tests/Ticketing.Tests` covers the `Ticket` lifecycle (seat and general
admission as exclusive shapes, no double check-in, void being idempotent where
check-in throws, and entry surviving a later void) and `TicketIssuingService`
(one ticket per seat, N tickets per general-admission quantity, distinct
tokens, idempotent redelivery, and one order-level `OrderTicketsIssued`
alongside the per-ticket events).

## Local run

```bash
dapr run --app-id ticketing \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/ticketing/Ticketing.Api
```

Run Ordering (and the rest of the checkout chain) too so `OrderConfirmed` flows,
and Catalog plus **Venue** so a performance can be published and its gate map
warmed.

## Dead letters

Every subscription here goes through `.SubscribesTo(topic, DeadLetterTopic)`, which adopts the
message's correlation chain and names `deadletter-ticketing` for anything this service cannot handle
(ADR-0040). Dapr retries five times first — a resiliency policy caps it, without which a poison
message would be redelivered forever and never reach the dead letter at all.

`OnDeadLetterAsync` drains that topic into the `dead_letters` table and logs at Error. There is no
read API for it yet: it is an operator's view of message payloads and this platform has no operator
role.

## Do not

- Store card data or PII beyond what a ticket needs.
- Call Venue or Inventory at scan time. The cache is warmed once, on publish;
  a scan that makes a network call is a scan that fails at the door.
- Validate a scan against the event. It is validated against the performance —
  that is the whole point of ADR-0039 here.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
