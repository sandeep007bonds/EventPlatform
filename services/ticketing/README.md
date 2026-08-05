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
| POST | `/integration/catalog/event-published` | Dapr pub/sub topic `EventPublished` → warm the local scan cache (check-in window + entry-gate assignments), so a scan never needs a live cross-service call (ADR-0025) |
| GET | `/v1/orders/{orderId}/tickets` | Tickets for an order |
| GET | `/v1/tickets/{id}` | A single ticket |
| GET | `/v1/tickets/{id}/qrcode` | The ticket's scan token rendered as a PNG QR code (auth: the ticket's own buyer, or the owning tenant) |
| GET | `/v1/events/{eventId}/tickets` | Every ticket for a tenant's event |
| POST | `/v1/tickets/scan` | Check a ticket in by its scan token, for a given event and (optionally) gate |

## Scaling for a mega-event

`deploy/base/ticketing/hpa.yaml` autoscales on CPU. Because the scan cache is
warmed once per event (not per scan), `ScanTicketAsync` makes zero calls to
Catalog/Inventory — each pod scans purely against its own Postgres connection
pool, so throughput scales linearly with replica count. For a known gate-open
time, pre-scale ahead of the crowd (`kubectl scale deployment/ticketing
--replicas=N` or bump `hpa.yaml`'s `minReplicas`) rather than relying solely
on reactive autoscaling — a cold pod's request queue in the first seconds of
a sudden crowd is avoidable.

## Layers

`Ticketing.Api` · `Ticketing.Application` (issuing + ports) · `Ticketing.Domain`
(`Ticket`) · `Ticketing.Infrastructure` (EF Core + Postgres, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a Dapr
sidecar and Postgres; run the checkout chain (Catalog → Inventory → Ordering →
Payments) so `OrderConfirmed` flows and tickets get issued.
