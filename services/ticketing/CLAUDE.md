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
  `POST /v1/tickets/scan` (tenant-owned, body `{ token }` — looks up by the
  opaque scan token, calls `Ticket.CheckIn()`; 404 unknown token, 409
  already-checked-in/void)
- **Events published:** `TicketIssued` (via outbox, one per ticket — unchanged),
  `OrderTicketsIssued` (via outbox, one per order, enqueued once all of the
  order's tickets are minted — see ADR-0021)
- **Events consumed:** `OrderConfirmed` (Ordering) → issue tickets

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

## Structure

`Ticketing.Api` (host + endpoints + Dapr subscription) · `Ticketing.Application`
(issuing + ports) · `Ticketing.Domain` (`Ticket` + invariants) ·
`Ticketing.Infrastructure` (EF Core + Postgres, outbox). `tests/` to follow.

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
