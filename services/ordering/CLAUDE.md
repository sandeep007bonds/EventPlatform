# CLAUDE.md — Ordering service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns checkout: turns a validated seat **hold** into a confirmed **order** via the
checkout saga (validate → pay → convert-to-sold → confirm) with compensation.
Bounded context: **Ordering** (ADR-0008). The aggregate is `Order`; the context
is named `Ordering` so the type never clashes with its namespace.

## Owns

- **Data store:** PostgreSQL `ordering` DB (this service only)
- **Public API:** `POST /v1/checkout` (Idempotency-Key), `GET /v1/orders`
  (`?mine=true` — buyer's own; `?forTenant=true` — organizer's tenant; exactly
  one is required), `GET /v1/orders/{id}`
- **Events published:** `OrderConfirmed` (via outbox)
- **Events consumed:** — (calls Inventory + Payment synchronously in the saga)

## Design notes

- **Polymorphic order lines:** `OrderLine`/`OrderLineSpec` are widened with
  nullable fields rather than split into separate seat/GA types — a line is
  either a reserved seat (`InventoryItemId`/`SeatId` set, `Quantity` always 1)
  or a general-admission quantity (`GeneralAdmissionAllocationId` set), never
  both. The hold snapshot read from Inventory (`HoldLineSnapshot`) carries the
  same shape, and `OrderConfirmed`'s `Lines` (`OrderLineSummary`) is the
  publish-side equivalent Ticketing consumes to mint the right ticket count.
- **Idempotent checkout:** deduped on `(tenant_id, idempotency_key)` — unique
  index + a pre-check. The `Idempotency-Key` header is required. Two *concurrent*
  requests can both pass the pre-check; the unique index then lets exactly one
  order win. `CreateOrderActivity` re-checks, and `IOrderRepository.TryAddAsync`
  swallows the unique-violation so the loser re-fetches the winner and the
  workflow short-circuits to `Duplicate` (409) — never a 500, never a double
  charge.
- **Saga (ADR-0010):** the checkout saga runs as a **Dapr Workflow**
  (`Ordering.Workflow`): `CheckoutWorkflow` orchestrates activities (fetch hold →
  create order → charge → convert-to-sold → confirm) with compensation (fail
  order, refund, release hold). The orchestrator is deterministic — all I/O is in
  activities — so a crash mid-flight resumes exactly where it left off. The Api
  schedules the workflow and awaits its completion.
- **Cross-service calls** go through ports: `IHoldClient` (Inventory) and
  `IPaymentClient` (Payments), both via Dapr service invocation.

## Structure

`Ordering.Api` (host + endpoints + workflow registration) · `Ordering.Application`
(ports + checkout contracts) · `Ordering.Workflow` (`CheckoutWorkflow` + activities) ·
`Ordering.Domain` (`Order`, `OrderLine` + invariants) · `Ordering.Infrastructure`
(EF Core + Postgres, Dapr hold client, payment stub, outbox). `tests/` to follow.

## Local run

```bash
dapr run --app-id ordering \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/ordering/Ordering.Api
```

Run Catalog and Inventory too (same Dapr setup) so the saga can invoke Inventory
by app-id.

## Do not

- Read another service's database (call Inventory via Dapr).
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Trust the tenant/user/idempotency key from the body — take them from the token
  and the `Idempotency-Key` header.
