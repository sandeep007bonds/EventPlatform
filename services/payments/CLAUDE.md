# CLAUDE.md — Payments service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Charges and refunds orders through a payment provider, idempotently, and records
each payment. Bounded context: **Payments** (ADR-0008). The aggregate is
`Payment`; the context is named `Payments` so the type never clashes with its
namespace. Target compliance: **PCI SAQ-A** — card data never touches our
servers; only PSP references are stored.

## Owns

- **Data store:** PostgreSQL `payments` DB (this service only)
- **Public API:** internal `POST /v1/payments/intents` (creates, but does not
  confirm, a payment intent — see below), internal
  `POST /v1/payments/{orderId}/sync` (re-reads the payment from the provider
  and reconciles it — the pull counterpart to the webhook, see below),
  `POST /v1/payments/refund`
  (called by the checkout saga's compensation path *and* by Ordering's
  buyer-initiated `CancelOrderWorkflow` — same endpoint, same `RefundActivity`,
  no Payments-side changes needed for cancellation to reuse it); public
  `POST /v1/payments/webhooks/stripe` (Stripe provider callback, signature-verified)
- **Events published:** `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded`
- **Events consumed:** —

## Design notes

- **Idempotent intent create:** deduped on `(order_id, idempotency_key)` — unique
  index + a pre-check. Two *concurrent* create calls can both pass the pre-check;
  the unique index lets one persist and `IPaymentRepository.TrySaveChangesAsync`
  swallows the loser's violation so it re-fetches the winner — including its
  already-recorded `ClientSecret`, so a retried call hands back the same secret
  rather than creating a second intent (no 500, no duplicate intent). The gateway
  is called with the same key, so the PSP dedupes it — and the loser's outbox
  events roll back with the failed save.
- **Gateway behind a port:** `IPaymentGateway`. Dev uses `SimulatedPaymentGateway`
  (captures synchronously — `CapturedImmediately = true`). The real **Stripe**
  gateway (Stripe.net, secret key from Key Vault) drops in here.
  `CreateIntentAsync` takes **no payment-method id at all** — it creates a
  PaymentIntent with `AutomaticPaymentMethods.Enabled = true` and deliberately
  never confirms it server-side (no `Confirm`/`PaymentMethod`/
  `PaymentMethodTypes`). The buyer attaches and authenticates a payment
  method (card, UPI, etc.) entirely client-side via Stripe's Payment Element,
  against the returned `ClientSecret` — this is what makes 3-D Secure/UPI
  app-switch work natively, since Stripe handles the challenge in the
  browser (ADR-0028). The resulting capture/decline is reported later, via
  the webhook or the sync endpoint below — never returned from this call.
  **No card data or secrets in code.**
- **Push *and* pull, one reconciliation:** `PaymentSyncService`
  (`POST /v1/payments/{orderId}/sync`) re-reads the PaymentIntent straight
  from the provider (`IPaymentGateway.GetStatusAsync`) and applies the
  *same* transitions and emits the *same* outbox events as the webhook
  path. The checkout saga polls it while waiting, so an outcome is learned
  even when the provider can't call back — which is always true on
  `localhost`, and occasionally true in production when a webhook is
  dropped. Safe to run alongside the webhook: every transition is a
  `TryMark*`, so whichever observes the outcome second is a no-op and no
  event is emitted twice (ADR-0028).
- **Webhook inbox (async capture / 3-D Secure / refunds):** `IPaymentWebhookGateway`
  (Stripe impl) verifies the `Stripe-Signature` header against
  `Payments:Stripe:WebhookSecret`, then `PaymentWebhookService` reconciles the
  `Payment` idempotently and emits the matching outbox event. At-least-once
  delivery is made exactly-once by deduping on the Stripe event id in the
  `processed_webhook_event` ledger, committed in the **same transaction** as the
  payment change and the outbox message. The neutral `PaymentWebhookNotification`
  keeps the Stripe SDK out of the Application/Domain layers.
- **Provider correlation:** the intent create stores the PaymentIntent id
  (`ProviderReference`) and its `ClientSecret` immediately, well before capture,
  so a later webhook maps back to the right payment and a retried checkout call
  can hand back the same client secret (ADR-0028).

## Structure

`Payments.Api` (host + endpoints) · `Payments.Application` (charge/refund/webhook
+ ports) · `Payments.Domain` (`Payment` + invariants) · `Payments.Infrastructure`
(EF Core + Postgres, gateway, webhook verifier, outbox). `tests/` to follow.

## Local run

```bash
dapr run --app-id payments \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/payments/Payments.Api
```

## Do not

- Store card data. Only PSP references (SAQ-A).
- Put secrets in code — Key Vault (cloud) / Dapr secret store (local) only.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
