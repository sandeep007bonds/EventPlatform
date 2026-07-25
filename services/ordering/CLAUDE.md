# CLAUDE.md — Ordering service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns checkout: turns a validated seat **hold** into a confirmed **order** via the
checkout saga (validate → pay → convert-to-sold → confirm) with compensation.
Bounded context: **Ordering** (ADR-0008). The aggregate is `Order`; the context
is named `Ordering` so the type never clashes with its namespace.

## Owns

- **Data store:** PostgreSQL `ordering` DB (this service only)
- **Public API:** `POST /v1/checkout` (Idempotency-Key), `GET /v1/orders/{id}`
- **Events published:** `OrderConfirmed` (via outbox)
- **Events consumed:** — (calls Inventory + Payment synchronously in the saga)

## Design notes

- **Idempotent checkout:** deduped on `(tenant_id, idempotency_key)` — unique
  index + a pre-check. The `Idempotency-Key` header is required.
- **Saga:** `CheckoutService` runs the steps sequentially in-process with explicit
  compensation (release hold, refund). **Follow-up:** move to a **Dapr Workflow**
  so the saga survives a crash mid-flight (ADR-0010, LLD §6).
- **Cross-service calls** go through ports: `IHoldClient` (Inventory) and
  `IPaymentClient` (Payments), both via Dapr service invocation.

## Structure

`Ordering.Api` (host + endpoints) · `Ordering.Application` (checkout saga + ports) ·
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
