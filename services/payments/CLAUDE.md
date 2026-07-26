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
- **Public API:** internal `POST /v1/payments/charge`, `POST /v1/payments/refund`
  (called by the checkout saga via Dapr service invocation); public
  `POST /v1/payments/webhooks/stripe` (Stripe provider callback, signature-verified)
- **Events published:** `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded`
- **Events consumed:** —

## Design notes

- **Idempotent charge:** deduped on `(order_id, idempotency_key)` — unique index
  + a pre-check.
- **Gateway behind a port:** `IPaymentGateway`. Dev uses `SimulatedPaymentGateway`
  (captures synchronously). The real **Stripe** gateway (Stripe.net, secret key
  from Key Vault) drops in here. **No card data or secrets in code.**
- **Webhook inbox (async capture / 3-D Secure / refunds):** `IPaymentWebhookGateway`
  (Stripe impl) verifies the `Stripe-Signature` header against
  `Payments:Stripe:WebhookSecret`, then `PaymentWebhookService` reconciles the
  `Payment` idempotently and emits the matching outbox event. At-least-once
  delivery is made exactly-once by deduping on the Stripe event id in the
  `processed_webhook_event` ledger, committed in the **same transaction** as the
  payment change and the outbox message. The neutral `PaymentWebhookNotification`
  keeps the Stripe SDK out of the Application/Domain layers.
- **Provider correlation:** the charge stores the PaymentIntent id
  (`ProviderReference`) even when not yet captured, so a later webhook maps back
  to the right payment.

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
