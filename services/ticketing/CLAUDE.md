# CLAUDE.md — Ticketing service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Issues tickets when an order is confirmed — one ticket per sold seat, each with an
opaque scan token (encoded as a QR at the edge). Bounded context: **Ticketing**
(ADR-0008). The aggregate is `Ticket`; the context is named `Ticketing` so the type
never clashes with its namespace.

## Owns

- **Data store:** PostgreSQL `ticketing` DB (this service only)
- **Public API:** `GET /v1/orders/{id}/tickets`, `GET /v1/tickets/{id}`
- **Events published:** `TicketIssued` (via outbox)
- **Events consumed:** `OrderConfirmed` (Ordering) → issue tickets

## Design notes

- **Idempotent issuance:** re-delivery of `OrderConfirmed` is a no-op once an order
  is ticketed (unique index on `(order_id, seat_id)` + a pre-check).
- **Scan token:** a 128-bit CSPRNG token (`RandomNumberGenerator`), unique per
  ticket. The QR encodes the token; the gate scans it. See tracker T-ticket-token
  for signing/rotation before production.

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
