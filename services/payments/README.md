# Payments service

Charges and refunds orders through a payment provider, idempotently, and records
each payment (PCI **SAQ-A** — only PSP references are stored, never card data).

## Flow

The checkout saga (Ordering) calls `charge`; on compensation it calls `refund`.
Charges are deduped on `(order_id, idempotency_key)`. Each result emits
`PaymentCaptured` / `PaymentFailed` / `PaymentRefunded` via the outbox.

- **Gateway behind `IPaymentGateway`.** Dev uses `SimulatedPaymentGateway`
  (synchronous capture, always succeeds). The real **Stripe** gateway (Stripe.net,
  secret from Key Vault, webhook inbox) drops in without touching the saga.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/payments/charge` | Charge an order (internal, saga-called) |
| POST | `/v1/payments/refund` | Refund an order's payment (internal) |

## Layers

`Payments.Api` · `Payments.Application` (charge/refund + ports) · `Payments.Domain`
(`Payment`) · `Payments.Infrastructure` (EF Core + Postgres, gateway, outbox).

See [service CLAUDE.md](CLAUDE.md) and the [LLD](../../docs/design/lld-phase1-seated.md).
