# Payments service

Charges and refunds orders through a payment provider, idempotently, and records
each payment (PCI **SAQ-A** — only PSP references are stored, never card data).

## Flow

The checkout saga (Ordering) calls `charge`; on compensation it calls `refund`.
Charges are deduped on `(order_id, idempotency_key)`. Each result emits
`PaymentCaptured` / `PaymentFailed` / `PaymentRefunded` via the outbox.

- **Gateway behind `IPaymentGateway`.** Dev uses `SimulatedPaymentGateway`
  (synchronous capture, always succeeds). The real **Stripe** gateway (Stripe.net,
  secret from Key Vault) drops in without touching the saga.
- **Webhook inbox.** `POST /v1/payments/webhooks/stripe` accepts Stripe callbacks
  (async capture, 3-D Secure completion, refunds). The `Stripe-Signature` header
  is verified against the signing secret (`Payments:Stripe:WebhookSecret`) — the
  endpoint is anonymous because it is authenticated by signature, not a bearer
  token. Delivery is at-least-once, so each event is deduped on its Stripe event
  id (`processed_webhook_event` ledger) and every reconciliation is idempotent,
  giving exactly-once processing with no double charge or double event. Without a
  configured signing secret the endpoint returns `503`.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/payments/charge` | Charge an order (internal, saga-called) |
| POST | `/v1/payments/refund` | Refund an order's payment (internal) |
| POST | `/v1/payments/webhooks/stripe` | Stripe provider callback (signature-verified) |

## Layers

`Payments.Api` · `Payments.Application` (charge/refund/webhook + ports) ·
`Payments.Domain` (`Payment`) · `Payments.Infrastructure` (EF Core + Postgres,
gateway, webhook verifier, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).
