# Inventory & Hold service

The no-oversell system of record for the platform. Owns seat availability, holds
(with TTL), convert-to-sold, and the immutable inventory ledger.

- **Consumes** `EventPublished` (Catalog) and generates one `inventory_item` per
  seat by reading Catalog's seat map (via Dapr service invocation).
- **Fast gate + final authority:** Redis Lua atomic hold (Stage B) backed by
  Postgres optimistic concurrency (`InventoryItem.Version`).
- **Self-healing cache:** if Redis is restarted or flushed, the `InventoryReconciler`
  rebuilds the fast gate from Postgres (the authority). It only ever re-applies
  held/sold restrictions — never frees a seat — so it cannot itself cause oversell.
- **Lean** on the hot path: Minimal API, no MediatR (ADR-0009).

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/integration/catalog/event-published` | Dapr pub/sub topic `EventPublished` → provision inventory |
| GET | `/v1/events/{eventId}/inventory` | Provisioned seat count for an event |
| POST | `/v1/holds` | Place an atomic seat hold (Redis fast gate + Postgres authority) |
| DELETE | `/v1/holds/{holdId}` | Release a hold |

## Layers

`Inventory.Api` · `Inventory.Application` (ports + provisioning) ·
`Inventory.Domain` · `Inventory.Infrastructure` (EF Core + Postgres, Catalog
client, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Inventory needs a
Dapr sidecar (pub/sub + service invocation) and Postgres; run Catalog alongside it
so `EventPublished` flows and the seat-map client can reach Catalog.
