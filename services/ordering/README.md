# Ordering service

Owns checkout — converting a validated seat hold into a confirmed order via the
checkout saga.

## Checkout saga

`validate hold → create order → charge → convert-to-sold → confirm`, with
compensation on failure (release hold, refund). Idempotent on
`(tenant, Idempotency-Key)`.

- **Inventory** and **Payments** calls go through `IHoldClient` / `IPaymentClient`
  (Dapr service invocation).
- **Durability upgrade (planned):** run the saga as a **Dapr Workflow** so it
  survives a crash mid-flight (ADR-0010).

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/checkout` | Check out a hold. Requires an `Idempotency-Key` header |
| GET | `/v1/orders/{id}` | Fetch an order |

## Layers

`Ordering.Api` · `Ordering.Application` (saga + ports) · `Ordering.Domain`
(`Order`) · `Ordering.Infrastructure` (EF Core + Postgres, Dapr client, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a Dapr
sidecar and Postgres; run Catalog + Inventory alongside so the saga can reach
Inventory and seats exist to buy.
