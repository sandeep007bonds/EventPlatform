# ADR-0028 — Async payment authentication (Stripe Payment Element) and hold extension

- **Status:** Accepted
- **Date:** 2026-08-09

## Context

`StripePaymentGateway.ChargeAsync` created and confirmed a Stripe
`PaymentIntent` synchronously, server-side, against a card-only
`paymentMethodId` tokenized client-side via Stripe Elements' `CardElement`
(shipped as an earlier pass this session). Real testing exposed the gap
this always had: a genuine test card came back `requires_action` (3-D
Secure) from `PaymentIntentCreateOptions{ Confirm = true }`, which the
gateway treated as an outright failure — no challenge was ever presented,
so the payment was simply stuck. This is not an edge case for this
platform: it is India-focused (real seed data this session is "ColdPlay
India Tour"), and RBI mandates 3-D Secure/AFA on essentially all card
transactions, so 3DS has to work as the *primary* path. The user then
explicitly broadened scope beyond a narrow 3DS patch: **(1)** full Stripe
Payment Element (cards + UPI + whatever else Stripe surfaces for the
account's region), not just cards with 3DS; **(2)** the buyer's seat hold
should be *extended* once payment begins, so a slow UPI app-approval or 3DS
challenge doesn't expire their seats out from under them.

## Decision

### Payments: create-only intents, `AutomaticPaymentMethods`, no card data server-side

`IPaymentGateway.ChargeAsync(...)` → `CreateIntentAsync(amountMinor,
currency, idempotencyKey, ct)` — no `paymentMethodId` parameter at all.
`StripePaymentGateway.CreateIntentAsync` builds
`PaymentIntentCreateOptions { Amount, Currency, AutomaticPaymentMethods =
new() { Enabled = true } }`, deliberately omitting `PaymentMethod`/
`PaymentMethodTypes`/`Confirm` — Stripe's Payment Element attaches and
confirms the method entirely client-side, against the returned client
secret, so the server never needs a `pm_...` id up front and never
authenticates the payment itself. `GatewayResult` (`Succeeded`/
`FailureReason`) is replaced by `GatewayIntentResult` (`Reference`,
`ClientSecret`, `CapturedImmediately`) — there is no synchronous
success/failure branch any more; a genuine intent-creation failure (bad
amount, PSP outage) is a real exception, left to propagate, exactly as it
already implicitly was for an uncaught Stripe error before this change.
`Payment` gains a persisted `ClientSecret` (`RecordIntentDetails` replaces
`RecordProviderReference`) so a retried/duplicate create call can hand back
the same secret without re-calling Stripe. `POST /v1/payments/intents`
replaces `POST /v1/payments/charge`, deleted outright (confirmed via grep:
no other caller existed). `PaymentWebhookService`/`StripeWebhookGateway`
need **zero** changes — they already reconcile `Payment` by
`ProviderReference` and were built anticipating exactly this async flow.

### Ordering: `OrderId` becomes the Dapr Workflow instance id

`OrderingEndpoints.CheckoutAsync` mints `OrderId` before scheduling the
saga and uses it as both the workflow's instance id and `Order.Id` (`Order`
now takes a leading `id` in its private ctor/`Create` rather than minting
`Guid.CreateVersion7()` internally). This removes any lookup table: a
webhook-driven subscriber can `RaiseEventAsync(orderId.ToString("N"), ...)`
directly.

### `CheckoutWorkflow`: create-intent → extend hold → wait for external event or timer

Step 3 of the saga (previously "charge, branch on success") is replaced
with: `CreateIntentActivity` (create the intent) → `RecordPaymentIntentActivity`
(persist the client secret on the `Order` row) → `ExtendHoldActivity` (best
effort, its result seeds the wait deadline but is never itself branched on
— `ConvertActivity`'s own expiry check remains the real safety net) →
`context.WaitForExternalEventAsync<PaymentOutcomeSignal>("PaymentOutcome")`
raced via `Task.WhenAny` against `context.CreateTimer(deadline, ...)`. This
is the standard Dapr Workflow/Durable Task external-event-with-timeout
idiom — first use in this repo, so its exact `CreateTimer`/
`WaitForExternalEventAsync` overloads should be double-checked against the
installed `Dapr.Workflow 1.15.0` surface during a real build (flagged, not
silently assumed correct). A new `CheckoutOutcome.PaymentTimedOut` (distinct
from `PaymentFailed`) covers "buyer never finished authenticating" vs. "the
provider declined it," both mapped to `422`.

Two new subscriber endpoints in Ordering
(`OnPaymentCapturedAsync`/`OnPaymentFailedAsync`, `.WithTopic(...)` on
`PaymentCaptured`/`PaymentFailed`) check `GetWorkflowStateAsync`: if the
saga is still `Running`, `RaiseEventAsync` resumes it. If it already
finished (most likely the timeout branch already fired and released the
hold) and the late event is a `PaymentCaptured`, the order is refunded
directly (`IPaymentClient.RefundAsync`) so the buyer isn't charged for
seats they no longer have — a concrete mitigation, not left as a silent
gap. This is Ordering's first outbound Dapr *subscription* (it previously
only called Inventory/Payments synchronously), so `Ordering.Api` gained a
`Dapr.AspNetCore` reference and `UseCloudEvents()`/`MapSubscribeHandler()`.

### Three routes to the outcome, in order of who knows first

The browser is the first to know a payment succeeded — `confirmPayment`
resolves with the confirmed PaymentIntent in its hand. Waiting for that fact
to arrive back by a slower route is wasted time, so the outcome now reaches
the saga by whichever of three routes gets there first:

1. **The buyer's browser tells us** (`POST /v1/orders/{id}/payment/sync`,
   called the instant `confirmPayment` resolves, and again from
   `CheckoutReturnPage` for redirecting methods). Fastest, and the common
   case. This only *triggers* reconciliation — Payments still re-reads the
   intent from Stripe itself, so a client claiming a success it never had
   changes nothing. Best-effort on the client: a failed nudge falls through
   to the routes below rather than blocking the buyer.
2. **Stripe's webhook**, unchanged. Authoritative, and the only route that
   works when the buyer closes the tab mid-payment.
3. **The saga's own poll**, below.

The trigger endpoint lives on **Ordering**, not Payments: Payments has no
browser-facing routes by design (saga-internal + webhook only, per the
gateway's own allowlist rule), and Ordering already owns the `Order` so it
can authorize the caller (`Order.UserId` vs. the token's `sub`, reported as
404 on mismatch so it never confirms another buyer's order exists). It calls
Payments' internal sync over Dapr; the resulting `PaymentCaptured` flows
through the existing outbox → pub/sub → subscriber → `RaiseEventAsync` path,
so the saga resumes through machinery that already existed.

### The saga also *pulls*, so a webhook is an optimization, not a dependency

Waiting only to be told has a hard operational edge: Stripe cannot reach a
`localhost` endpoint at all, so on a developer machine the webhook never
arrives and every checkout sits in `AwaitingPayment` until the saga times
out — solvable with a `stripe listen` forwarder, but that makes a CLI tool
a hard prerequisite for the app to work locally. The same class of failure
exists in production whenever a webhook is dropped or the endpoint is
briefly unreachable.

So the wait is a **poll loop**, not a single timer: each tick (20s) races
the external event against a short timer, and on a timer win calls a new
`SyncPaymentStatusActivity` → Payments' `POST /v1/payments/{orderId}/sync`
→ `PaymentSyncService`, which re-reads the PaymentIntent straight from
Stripe (`IPaymentGateway.GetStatusAsync`) and applies the **same**
reconciliation as the webhook path: the same `TryMarkCaptured`/
`TryMarkFailed` transitions, the same `PaymentCaptured`/`PaymentFailed`
outbox events. Push and pull are therefore interchangeable, not
alternatives — whichever observes the outcome second is a no-op, because
every transition is a `Try*`. The webhook stays the production path (it is
instant); polling bounds the worst case to one tick and removes the
inbound-connectivity requirement entirely.

The external-event subscription is created **once**, outside the loop, with
only the timer recreated per tick — re-subscribing per iteration would
leave abandoned waiters able to swallow the event.

### `OrderingEndpoints.CheckoutAsync`: race a fast poll against the full blocking wait

Rather than adopt `SetCustomStatusAsync`/workflow-state polling (no
precedent in this codebase), the checkout endpoint races a bounded
~10-second poll of `IOrderRepository.GetByIdAsync` (watching for
`Order.PaymentClientSecret` or a terminal status) against the existing
`WaitForWorkflowCompletionAsync`. Whichever resolves first wins: a client
secret found → `200 { orderId, clientSecret }` (the buyer now sees Payment
Element); a terminal order found with no secret → `200 { orderId,
clientSecret: null }` (the simulated-gateway instant-capture dev path);
the completion task wins first → the existing `MapCheckoutOutcome` switch,
unchanged; the poll budget elapses with neither resolved → fall back to
`await completionTask` (today's fully-blocking behavior). A rare, disclosed
edge case: a genuinely concurrent double-submit racing inside
`CreateOrderActivity` can leave the poll watching an `orderId` that never
gets persisted (the other request's id wins) — it still resolves correctly
via the fallback, just a few seconds slower.

### Inventory: `HoldOptions.PaymentExtensionTtl`, saga-triggered extension

New `HoldOptions.PaymentExtensionTtl` (default 15 minutes, vs. the base
2-minute `Ttl`) — generous for a UPI app-switch or 3DS challenge without
holding inventory hostage indefinitely. `Hold.Extend(newExpiresAt)` only
ever moves `ExpiresAt` forward (safe no-op on replay).
`HoldService.ExtendHoldAsync` uses a plain `SaveChangesAsync` (confirmed
`Hold` carries no optimistic-concurrency token, unlike `InventoryItem`/
`GeneralAdmissionAllocation`), then extends Redis's bookkeeping-key TTLs
(`IHoldStore.ExtendAsync` — plain `KeyExpireAsync` calls on the
sentinel/set keys; confirmed the per-seat/per-allocation markers
themselves carry no TTL and need no change). New internal
`POST /v1/holds/{id}/extend` (saga-only), called by a new saga-owned
`ExtendHoldActivity`, mirroring `ConvertActivity`/`ReleaseHoldActivity`
already being saga-triggered, not buyer-triggered.

**Required companion fix, shipped in the same change**:
`HoldService.ReapHoldAsync` previously only checked `hold.Status ==
Active`, never `hold.ExpiresAt` — a hold extended in the narrow race window
between the reaper's batch query and its per-hold reap call would have
been incorrectly reaped anyway, defeating the extension. Added an explicit
re-check (`if (hold.ExpiresAt > DateTimeOffset.UtcNow) return false;`).

### Frontend: Payment Element replaces Card Element; intent creation moves before the payment form

`CheckoutPage.tsx` calls `checkout(holdId, idempotencyKey, buyerEmail)`
directly on submit (no more client-side tokenization step before the
backend call) — the central UX-flow inversion: the intent now exists
*before* the buyer ever sees a payment form. `clientSecret === null` →
navigate straight to the order page (dev/instant-capture path);
`clientSecret` present → mount `<Elements stripe={stripePromise}
options={{ clientSecret }}>` wrapping the rewritten
`CheckoutPaymentForm`, which renders `<PaymentElement />` and calls
`stripe.confirmPayment({ elements, confirmParams: { return_url }, redirect:
'if_required' })` — `if_required` keeps in-page resolution for methods
needing no redirect (most cards) while still supporting the ones that do
(3DS challenge frame, UPI app-switch). A new `CheckoutReturnPage`
(`/checkout/:holdId/return`) is the `return_url` target: it reads Stripe's
own `payment_intent_client_secret` plus our `orderId` query param, calls
`stripe.retrievePaymentIntent` directly (no `<Elements>` context on this
page, so `stripePromise` is used, not the `useStripe`/`useElements` hooks),
and routes to the order page on success/pending or back to a fresh
checkout attempt on failure. `OrderPage` gained a second bounded poll
(mirroring its existing ticket-poll shape, but uncapped — an authentication
flow can legitimately run long, and the extended hold already bounds it)
while `order.status === 'AwaitingPayment'`, so a buyer landing there
mid-authentication sees it flip to `Confirmed` without a manual refresh.
`DEV_FALLBACK_PAYMENT_METHOD_ID` is removed — Payment Element never takes a
pre-supplied method id.

## Consequences

- `POST /v1/payments/charge` and every card-only/`paymentMethodId` code
  path across Payments and Ordering are gone — grepped clean after the
  change.
- Ordering's checkout saga can now genuinely pause for an arbitrary
  authentication duration instead of assuming payment resolves
  synchronously — the architecturally significant shift this ADR records.
- A missed webhook no longer strands a checkout: the saga's poll loop
  reconciles against Stripe directly within one tick. The residual gap is
  narrower but real — a `Payment` can still be left `Initiated` if the
  buyer abandons authentication *and* nothing ever polls it again (the
  saga stops polling once it times out and fails the order). There is no
  standalone reconciliation job sweeping old `Initiated` rows; disclosed,
  not solved here.
- Running against a real Stripe key locally no longer requires the Stripe
  CLI. `scripts/dev-up.sh` still starts `stripe listen` automatically when
  the CLI is installed and authenticated (instant confirmation), and
  silently skips it otherwise — the poll loop covers that case within a
  few seconds.
- Polling costs one Stripe API read per in-flight payment per tick, but it
  is now the third route rather than the first, so the interval is a relaxed
  20s: an abandoned checkout costs ~45 reads over the 15-minute hold window
  instead of ~300, and nobody is waiting on those reads anyway.
- The browser-triggered sync is not a trust boundary — it carries no
  outcome, only "look now." Anyone can call it for their own order; the
  worst they achieve is making the backend re-read Stripe, which is exactly
  what it does on its own anyway.
- A genuine Stripe API exception at intent-creation time still has no
  saga-level compensation path (fails the whole workflow instance rather
  than reaching `FailOrderActivity`/`ReleaseHoldActivity`) — a pre-existing
  gap with the same shape the old `ChargeActivity` already had for an
  uncaught exception, not newly introduced.
- `CancelOrderWorkflow` is unaffected — its instance ids are independently
  minted, no collision risk with `CheckoutWorkflow`'s now-`OrderId`-derived
  ones.
- Enabling UPI/other India payment methods for the Stripe account, and
  confirming the webhook endpoint subscribes to
  `payment_intent.succeeded`/`payment_intent.payment_failed`/
  `charge.refunded`, are operational steps outside this repo, required
  before UPI actually appears in Payment Element.

## Alternatives considered

- **A narrow 3DS-only patch to the existing card-only flow** — rejected;
  the user explicitly asked for full Payment Element (cards + UPI + more),
  and a 3DS-only fix would still require the same synchronous-to-async
  saga restructure, just without the payment-method breadth.
- **`SetCustomStatusAsync`/workflow-state polling for the fast API return**
  — rejected in favor of polling the `Order` row directly: no precedent for
  custom-status polling in this codebase, versus repository polling and the
  existing `MapCheckoutOutcome` switch, both already-proven patterns.
- **Keeping the pre-payment `Ttl` unextended and accepting a lost hold on a
  slow authentication** — rejected; the user explicitly chose to extend the
  hold once payment begins, given losing seats after a buyer has committed
  to paying is a far worse outcome than a strict pre-payment countdown.

## References

- ADR-0025/ADR-0026 — the "propagate once, verify/read locally, zero
  cross-service calls on the hot path" convention (Payments' webhook
  pipeline already followed this shape; unrelated to the saga-external-event
  pattern introduced here, which is a different kind of async boundary).
- `services/payments/CLAUDE.md`, `services/ordering/CLAUDE.md`,
  `services/inventory/CLAUDE.md`, `frontend/CLAUDE.md` — updated service
  docs.
