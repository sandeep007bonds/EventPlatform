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
  buyer reload/redirect-return mid-authentication),
  `POST /v1/orders/{id}/payment/sync` (buyer-triggered "my payment just
  resolved, reconcile it now" — ownership-checked, carries no outcome of its
  own, only asks Payments to re-read the intent from Stripe, see ADR-0028),
  `POST /v1/orders/{id}/cancel`
  (buyer-initiated cancellation + refund),
  `POST /v1/checkout/quote` (prices a hold, optionally with a promo code, and
  creates nothing — what the buyer's "Apply" button calls. A rejected code
  comes back as a 200 with a machine-readable `promoCodeRejection` alongside
  the real undiscounted price, not as an error: the quote is still valid)
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
- **An order names a performance and an event, and needs both (ADR-0039).**
  `Order.EventSessionId` is what the buyer actually bought — the night, the
  inventory, the tickets and the scan all hang off it. `Order.CatalogEventId`
  stays alongside it because two things are decided for the whole run rather
  than for one night: a promo code is defined per event, and the per-buyer
  ticket cap is counted across every performance of it. Neither can be derived
  from the other here without a call to Catalog, so both are carried on the
  hold snapshot and stamped on the order.
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
  hold-extension activity's result. The outcome reaches the saga by whichever
  of three routes is first: the **buyer's browser** nudging
  `/v1/orders/{id}/payment/sync` the instant `confirmPayment` resolves
  (fastest, the common case); Stripe's **webhook** (authoritative, and the
  only route that survives the buyer closing the tab); or the saga's own
  **poll** every 20s via `SyncPaymentStatusActivity`. All three land on the
  same idempotent reconciliation in Payments, so whichever is second is a
  no-op, and checkout completes even where Stripe can't call back at all
  (localhost) — ADR-0028. The external-event subscription is created once,
  outside the poll loop — re-subscribing per tick would leave abandoned
  waiters able to swallow the event.
  The orchestrator is deterministic — all I/O
  is in activities — so a crash mid-flight resumes exactly where it left off.
  The Api races a bounded poll of the `Order` row (for a fast return once a
  client secret exists) against the full `WaitForWorkflowCompletionAsync`.
  Tenant is sourced from the fetched hold (`hold.TenantId`, step 1 of the
  saga), not from `CheckoutWorkflowInput` — that record has no `TenantId`
  field (ADR-0022).
- **Ordering computes the money, and it is the only thing that does (ADR-0034).** One model,
  in one pure static class (`Ordering.Domain/OrderPricingCalculator.cs`, no I/O):
  `subtotal = Σ line.PriceMinor` → `discount` (clamped to the subtotal of the lines the code's
  tiers actually cover) → `fee = event.BookingFeePerTicketMinor × Σ line.Quantity` (per admission,
  never discounted) → `tax = round(net × rate) + round(fee × rate)`, both AwayFromZero →
  `total = net + fee + tax`. The tax is two roundings rather than one because the fee is
  **non-refundable**: `Order.RefundableMinor` (= `net + round(net × rate)`) has to be an exact
  subtraction, and a single combined rounding leaves it a minor unit out. `CancelOrderWorkflow`
  refunds `RefundableMinor`; `CheckoutWorkflow`'s compensation refunds in full, since that buyer
  got nothing. `TotalMinor` keeps its meaning — the payable amount — so
  `CreateIntentInput`/`ConfirmInput`/`OrderConfirmed` were untouched; `Order` stores
  `SubtotalMinor`/`DiscountMinor`/`BookingFeeMinor`/`TaxMinor`/`TaxRatePercent`/`TaxLabel`/`PromoCodeId`/`PromoCodeText`
  alongside it so a placed order can explain itself later. Catalog owns the *definition* of a promo
  code, but **redemption counting lives here**, because Ordering owns the orders and reading
  Catalog's database is forbidden. `PromoCodeEvaluator` (Application) is the single implementation
  of "may this buyer use this code, and what is it worth here," shared by the advisory
  `POST /v1/checkout/quote` preview and `CheckoutWorkflow`'s own re-check — so a quoted price and a
  charged price can never come from two different code paths. The saga **re-evaluates from
  scratch**; a code that lapsed in between fails the checkout with `CheckoutOutcome.PromoCodeInvalid`
  (409) rather than quietly charging full price. `OrderLine.TicketTypeId` (carried from the hold
  snapshot, which gets it from Inventory, which got it from the performance's `SessionAllocation`
  map) is what makes type-scoped codes possible. It replaced a free-text price-tier name that was a
  string doing identity's work: it matched only because the comparison happened to be
  case-insensitive, it silently stopped matching the moment a type was renamed, and it joined to
  nothing.
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
(EF Core + Postgres, Dapr hold client, payment stub, outbox) ·
`tests/Ordering.Tests` (checkout-saga decision logic, layering, orchestrator purity).

## Testing the saga

`CheckoutWorkflowTests` drives `CheckoutWorkflow` through a substituted `WorkflowContext`
and asserts *which activities get scheduled* for a given set of results — the
compensation ordering, the terminal outcome, and that a captured payment
actually reaches convert-and-confirm.

`OrchestratorPurityTests` is the one that matters most, and it is not a mock test.
An orchestrator may only ever await **durable** tasks and must be deterministic
across replays; break that and the workflow engine silently ends the turn having
scheduled nothing (`Sending 0 action(s)`), so the saga stalls with no exception
anywhere. A mocked context cannot reproduce that — there is no executor counting
actions — so instead the orchestrator sources are scanned for the constructs that
cause it (`CancelAsync`, `Task.Delay`/`Task.Run`, `DateTime.UtcNow`, `Guid.NewGuid`,
`HttpClient`, blocking waits). This is not hypothetical: exactly one such line
shipped and stranded captured payments in `AwaitingPayment` (ADR-0028).

## Local run

```bash
dapr run --app-id ordering \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/ordering/Ordering.Api
```

Run Catalog and Inventory too (same Dapr setup) so the saga can invoke Inventory
by app-id and price a promo code against Catalog.

## Dead letters

Every subscription here goes through `.SubscribesTo(topic, DeadLetterTopic)`, which adopts the
message's correlation chain and names `deadletter-ordering` for anything this service cannot handle
(ADR-0040). Dapr retries five times first — a resiliency policy caps it, without which a poison
message would be redelivered forever and never reach the dead letter at all.

`OnDeadLetterAsync` drains that topic into the `dead_letters` table and logs at Error. There is no
read API for it yet: it is an operator's view of message payloads and this platform has no operator
role.

## Do not

- Read another service's database (call Inventory via Dapr).
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Trust the tenant, user, or idempotency key from the request body — user comes
  from the token's `sub`, tenant is derived from the fetched `Hold` (ADR-0022),
  idempotency key comes from the `Idempotency-Key` header.
