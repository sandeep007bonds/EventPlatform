# CLAUDE.md — Inventory service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

The no-oversell system of record. Owns seat availability, holds (with TTL), the
convert-to-sold path, and the immutable inventory ledger. Bounded context:
**Inventory & Hold** (ADR-0008). Generates seat inventory when Catalog publishes
an event.

## Owns

- **Data store:** PostgreSQL `inventory` DB (this service only) + Redis (hot path)
- **Public API:** REST `/v1/holds` (Stage B), `/v1/events/{id}/inventory`
- **Events published:** `SeatHeld`, `SeatReleased`, `SeatSold` (via outbox)
- **Events consumed:** `EventPublished` (Catalog) → provision seat inventory

## Design notes (ADR-0009)

- **Lean on the hot path:** Minimal API, hand-tuned data access, **no MediatR**.
- **No-oversell:** Redis Lua atomic check-and-set is the fast gate; Postgres
  optimistic concurrency (`InventoryItem.Version`) is the final authority; a hold
  TTL + reaper reclaims abandoned holds. See [LLD §4–5](../../docs/design/lld-phase1-seated.md).
- **Idempotent provisioning:** re-delivery of `EventPublished` is a no-op once an
  event has inventory.
- **Drift reconciliation:** Redis holds only a sparse cache; a restart/flush loses
  it. `InventoryReconciler` (a background service) detects this via a sentinel key
  and rebuilds the fast gate from Postgres — writing back held (with remaining TTL)
  and sold seats. **Safety invariant:** the rebuild only adds restrictions, never
  frees a seat, so it can never cause oversell even if it races a live hold.

## Structure

Layers directly under this folder (no `src/`): `Inventory.Api` (host + endpoints +
Dapr subscription), `Inventory.Application` (ports + provisioning + reconciliation),
`Inventory.Domain` (`InventoryItem`, `Hold`, `LedgerEntry` + invariants),
`Inventory.Infrastructure` (EF Core + Postgres, Redis hold store, the Catalog
seat-map client, the expiry reaper, the drift reconciler, outbox). `tests/` to follow.

## Local run

```bash
dapr run --app-id inventory \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/inventory/Inventory.Api
```

Run Catalog too (same Dapr setup) so `EventPublished` flows through pub/sub and
the seat-map client can reach Catalog by app-id.

## Do not

- Read another service's database (pull the seat map from Catalog via Dapr).
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Put MediatR on the hold path.
