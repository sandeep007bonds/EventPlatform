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
  demand via `QRCoder`; see ADR-0025), `GET /v1/events/{id}/tickets`,
  `POST /v1/tickets/scan` (tenant-owned, body `{ token, eventId, gateId? }`
  — looks up by the opaque scan token; 404 on an unknown token *or* a token
  whose `Ticket.CatalogEventId` doesn't match `eventId` (same shape,
  deliberately — a wrong-event scan shouldn't reveal the token is valid
  elsewhere); 409 outside the event's check-in window, presented at the
  wrong gate, or already-checked-in/void; otherwise calls `Ticket.CheckIn()`
  (see ADR-0024) — every check is a purely local read, see below (ADR-0025)
- **Events published:** `TicketIssued` (via outbox, one per ticket — unchanged),
  `OrderTicketsIssued` (via outbox, one per order, enqueued once all of the
  order's tickets are minted — see ADR-0021)
- **Events consumed:** `OrderConfirmed` (Ordering) → issue tickets;
  `EventPublished` (Catalog) → warm the local scan cache (see below, ADR-0025)

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
  calls — because the scan cache is warmed once per event, not once per
  scan (ADR-0025, superseding ADR-0024's original live-call design once the
  real mega-event throughput requirement was known).**
  `EventScanContextProvisioningService` runs once per event, triggered by a
  new subscription to Catalog's `EventPublished` (this service's second
  subscription, after `OrderConfirmed`): it persists `EventScanContext`
  (check-in window: `DoorsOpenAt` falling back to `StartsAt`, through
  `EndsAt` — now carried directly on `EventPublished`, no Catalog call
  needed for this part at all), calls `ICatalogEventClient`
  (`DaprCatalogEventClient`, this service's first *outbound* Dapr call) once
  for the seat map's gate assignments (persisted as `SeatEntryGate` rows),
  and calls `IInventoryGaClient` (`DaprInventoryGaClient`) once for every
  general-admission allocation's section-to-gate mapping (persisted as
  `GaAllocationGate` rows — a bounded retry covers the race against
  Inventory's own async provisioning off the same `EventPublished`
  message). `ScanTicketAsync` then only ever reads these three local,
  indexed tables — no dependency on Catalog/Inventory being reachable or
  fast at the moment of an actual scan. Deliberately Postgres-backed, not
  Redis: this data is read-only and immutable once warmed, and a fresh/
  scaled-out pod needs no pub/sub "catch-up" the way an in-memory-only
  cache would.

## Structure

`Ticketing.Api` (host + endpoints + Dapr subscriptions + QR-code generation)
· `Ticketing.Application` (issuing + scan-context provisioning + ports) ·
`Ticketing.Domain` (`Ticket`, `EventScanContext`, `SeatEntryGate`,
`GaAllocationGate` + invariants) · `Ticketing.Infrastructure` (EF Core +
Postgres, outbox, the once-per-event Catalog/Inventory Dapr clients).
`tests/` to follow.

## Local run

```bash
dapr run --app-id ticketing \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/ticketing/Ticketing.Api
```

Run Ordering (and the rest of the checkout chain) too so `OrderConfirmed` flows.

## Do not

- Store card data or PII beyond what a ticket needs.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
