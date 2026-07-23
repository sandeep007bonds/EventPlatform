# ADR-0012 — Payments: saga + idempotency + PCI SAQ-A

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

Payments must never double-charge and never confirm an unpaid order, under
retries, crashes, and webhook replays. Card-data handling must minimize
compliance burden and breach risk.

## Decision

- **PCI-DSS SAQ-A**: raw card data never touches our servers; use the PSP's
  hosted fields / SDK (e.g., Stripe Elements). We handle only tokens / intent
  ids.
- **Payment saga** (orchestrated, ADR-0010) with compensations (release hold /
  refund).
- **Idempotency everywhere**: client-generated keys on order + payment; PSP
  idempotency keys; idempotent hold→sold (keyed by `orderId`); a webhook
  **inbox** deduped by PSP event id.
- **Resilience**: timeouts, circuit breaker, secondary-PSP failover; if all PSPs
  are down, **pause new checkouts** rather than take unconfirmable money.
- Append-only financial ledger + daily settlement reconciliation.

## Consequences

- Invariants hold: one order, one charge, one set of tickets — for any
  retry/crash/replay combination.
- Lowest PCI burden (SAQ-A) and the smallest card-breach surface.
- The rare "charged but hold expired" race is handled by keeping TTL > max payment
  time, with an auto-refund safety net.

## Alternatives considered

- **Store/handle card data ourselves** — massive PCI scope + risk. Rejected.
- **Two-phase commit across services** — impractical/fragile. Rejected in favour
  of saga + idempotency.
