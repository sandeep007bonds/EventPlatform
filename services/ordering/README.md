# Ordering service

Owns checkout — converting a validated seat hold into a confirmed order via the
checkout saga.

## Checkout saga

`validate hold → create order → charge → convert-to-sold → confirm`, with
compensation on failure (release hold, refund). Idempotent on
`(tenant, Idempotency-Key)`.

- Runs as a **Dapr Workflow** (`Ordering.Workflow`, ADR-0010): the orchestrator
  is deterministic and only calls activities, so a crash mid-flight resumes
  exactly where it left off. The Api schedules the workflow and awaits its result.
- **Inventory** and **Payments** calls go through `IHoldClient` / `IPaymentClient`
  (Dapr service invocation).
- **Concurrent-duplicate safe:** if two requests with the same `Idempotency-Key`
  race past the pre-check, the unique index lets one order win; the loser
  re-fetches it and the workflow returns `409` (`Duplicate`) — no 500, no double
  charge.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/checkout` | Check out a hold. Requires an `Idempotency-Key` header |
| GET | `/v1/orders?mine=true` | The caller's own orders (buyer), paginated |
| GET | `/v1/orders?forTenant=true` | The caller's tenant's orders (organizer), paginated |
| GET | `/v1/orders/{id}` | Fetch an order |

## Layers

`Ordering.Api` · `Ordering.Application` (ports + checkout contracts) ·
`Ordering.Workflow` (`CheckoutWorkflow` + activities) · `Ordering.Domain`
(`Order`) · `Ordering.Infrastructure` (EF Core + Postgres, Dapr client, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a Dapr
sidecar and Postgres; run Catalog + Inventory alongside so the saga can reach
Inventory and seats exist to buy.
