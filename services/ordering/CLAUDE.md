# CLAUDE.md — Ordering service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns checkout: turns a validated seat **hold** into a confirmed **order** via the
checkout saga (validate → pay → convert-to-sold → confirm) with compensation.
Bounded context: **Ordering** (ADR-0008). The aggregate is `Order`; the context
is named `Ordering` so the type never clashes with its namespace.

## Owns

- **Data store:** PostgreSQL `ordering` DB (this service only)
- **Public API:** `POST /v1/checkout` (Idempotency-Key; body requires only a
  `BuyerEmail` — a plain checkout-time field for ticket delivery, not derived
  from any token claim, see ADR-0021 — no payment-method id any more: the
  backend creates a Stripe PaymentIntent and the buyer authenticates
  client-side via Payment Element, see ADR-0028. Returns `{ orderId,
  clientSecret }` — `clientSecret` is `null` when payment already resolved
  synchronously, otherwise the frontend mounts Payment Element against it),
  `GET /v1/orders` (`?mine=true` — buyer's own; `?forTenant=true` —
  organizer's tenant; exactly one is required), `GET /v1/orders/{id}` (the
  response includes `paymentClientSecret` while `AwaitingPayment`, for a
  buyer reload/redirect-return mid-authentication), `POST /v1/orders/{id}/cancel`
  (buyer-initiated cancellation + refund)
- **Events published:** `OrderConfirmed` (via outbox)
- **Events consumed:** `PaymentCaptured`, `PaymentFailed` (Payments, via
  webhook) — resume a checkout saga waiting on payment authentication
  (ADR-0028); Inventory is still called synchronously in the saga

## Design notes

- **`Order.BuyerEmail`** (nullable, `HasMaxLength(320)`) is a plain
  checkout-time field, threaded through `CheckoutWorkflowInput` →
  `CreateOrderInput` → `Order` → `OrderConfirmed`, so Ticketing/Communication
  can send a single combined ticket-delivery email (ADR-0021). Not derived
  from any JWT claim — buyers won't necessarily authenticate via a flow that
  carries an email claim (see the deferred Identity/OTP work).
- **Polymorphic order lines:** `OrderLine`/`OrderLineSpec` are widened with
  nullable fields rather than split into separate seat/GA types — a line is
  either a reserved seat (`InventoryItemId`/`SeatId` set, `Quantity` always 1)
  or a general-admission quantity (`GeneralAdmissionAllocationId` set), never
  both. The hold snapshot read from Inventory (`HoldLineSnapshot`) carries the
  same shape, and `OrderConfirmed`'s `Lines` (`OrderLineSummary`) is the
  publish-side equivalent Ticketing consumes to mint the right ticket count.
- **Idempotent checkout:** deduped on `(user_id, idempotency_key)` — unique
  index + a pre-check. Scoped by buyer, not tenant: a checkout attempt is a
  buyer action, and the buyer's own token may carry no `tenant_id` claim at
  all (ADR-0022). The `Idempotency-Key` header is required. Two *concurrent*
  requests can both pass the pre-check; the unique index then lets exactly one
  order win. `CreateOrderActivity` re-checks, and `IOrderRepository.TryAddAsync`
  swallows the unique-violation so the loser re-fetches the winner and the
  workflow short-circuits to `Duplicate` (409) — never a 500, never a double
  charge.
- **Saga (ADR-0010, async payment per ADR-0028):** the checkout saga runs as a
  **Dapr Workflow** (`Ordering.Workflow`): `CheckoutWorkflow` orchestrates
  activities (fetch hold → create order → create payment intent → record its
  client secret → extend the hold → wait for the async payment outcome or a
  timeout → convert-to-sold → confirm) with compensation (fail order, refund,
  release hold). `OrderId` is minted by `OrderingEndpoints.CheckoutAsync`
  *before* scheduling and doubles as the workflow's own Dapr instance id — a
  webhook-driven `PaymentCaptured`/`PaymentFailed` subscriber raises an event
  straight back into the running saga (`RaiseEventAsync(orderId.ToString("N"),
  "PaymentOutcome", ...)`) with no lookup table. The wait races
  `WaitForExternalEventAsync` against a `CreateTimer` deadline seeded by the
  hold-extension activity's result. The orchestrator is deterministic — all I/O
  is in activities — so a crash mid-flight resumes exactly where it left off.
  The Api races a bounded poll of the `Order` row (for a fast return once a
  client secret exists) against the full `WaitForWorkflowCompletionAsync`.
  Tenant is sourced from the fetched hold (`hold.TenantId`, step 1 of the
  saga), not from `CheckoutWorkflowInput` — that record has no `TenantId`
  field (ADR-0022).
- **Cross-service calls** go through ports: `IHoldClient` (Inventory),
  `IPaymentClient` (Payments), and `ITicketClient` (Ticketing), all via Dapr
  service invocation.
- **Cancellation saga:** `POST /v1/orders/{id}/cancel` runs `CancelOrderWorkflow`
  (`Ordering.Workflow`), the buyer-initiated counterpart to `CheckoutWorkflow` —
  void tickets → release sold inventory → refund → mark the order refunded.
  Deliberately the *reverse* step order of `CheckoutWorkflow`'s own compensation
  (which refunds before releasing the hold): that path unwinds a checkout that
  never completed, this one unwinds a fully completed sale, so the product
  (tickets/seats) is taken back before the money is returned. Each activity is
  individually idempotent (gated on the underlying aggregate's own status —
  `Order.Status`, `Hold.Status`, `Ticket.Status`), so a crash mid-flight resumes
  safely on replay. `RefundActivity`/`IPaymentClient.RefundAsync` are the exact
  same ones `CheckoutWorkflow`'s compensation path already used — reused as-is,
  no changes to Payments needed.

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
- Trust the tenant, user, or idempotency key from the request body — user comes
  from the token's `sub`, tenant is derived from the fetched `Hold` (ADR-0022),
  idempotency key comes from the `Idempotency-Key` header.
