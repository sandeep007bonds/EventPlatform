# Ticketing service

Issues tickets when an order is confirmed — one ticket per sold seat, each with a
CSPRNG scan token (encoded as a QR at the edge).

## Flow

Consumes `OrderConfirmed` (Ordering, over Dapr pub/sub) and issues one `Ticket`
per seat, idempotently (deduped on `(order_id, seat_id)`). Each issue emits
`TicketIssued` via the outbox.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/integration/ordering/order-confirmed` | Dapr pub/sub topic `OrderConfirmed` → issue tickets |
| GET | `/v1/orders/{orderId}/tickets` | Tickets for an order |
| GET | `/v1/tickets/{id}` | A single ticket |

## Layers

`Ticketing.Api` · `Ticketing.Application` (issuing + ports) · `Ticketing.Domain`
(`Ticket`) · `Ticketing.Infrastructure` (EF Core + Postgres, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a Dapr
sidecar and Postgres; run the checkout chain (Catalog → Inventory → Ordering →
Payments) so `OrderConfirmed` flows and tickets get issued.
