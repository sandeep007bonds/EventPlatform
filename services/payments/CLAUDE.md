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
  (called by the checkout saga via Dapr service invocation)
- **Events published:** `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded`
- **Events consumed:** —

## Design notes

- **Idempotent charge:** deduped on `(order_id, idempotency_key)` — unique index
  + a pre-check.
- **Gateway behind a port:** `IPaymentGateway`. Dev uses `SimulatedPaymentGateway`
  (captures synchronously). The real **Stripe** gateway (Stripe.net, secret key
  from Key Vault, webhook inbox for async capture) drops in here — see tracker
  T-stripe. **No card data or secrets in code.**

## Structure

`Payments.Api` (host + endpoints) · `Payments.Application` (charge/refund + ports) ·
`Payments.Domain` (`Payment` + invariants) · `Payments.Infrastructure` (EF Core +
Postgres, gateway, outbox). `tests/` to follow.

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
