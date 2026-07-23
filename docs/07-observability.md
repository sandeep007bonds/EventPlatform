# 07 — Observability

During an on-sale you have minutes, not hours, to notice and react to a problem.
Observability isn't an afterthought — it's the instrument panel the on-call team
and organizers fly the on-sale by.

## The three pillars (+ one)

- **Metrics** — Prometheus-compatible; OpenTelemetry instrumentation.
- **Logs** — structured JSON, centralized (Loki/ELK), correlation IDs.
- **Traces** — distributed tracing (OpenTelemetry → Tempo/Jaeger) across the
  whole checkout saga.
- **Events/audit** — the Kafka log doubles as a replayable audit trail.

Everything is correlated by a **request/trace id** that flows from the edge
through the waiting room, gateway, and every service.

## Golden signals per service

For each service: **latency, traffic, errors, saturation**. Plus domain SLOs:

| SLO | Target |
|-----|--------|
| Store checkout p99 (post-admission) | < 2s |
| Hold operation p99 | < 100ms |
| Payment success rate | Track vs baseline; alert on drop |
| Oversell events | **0** (page immediately if ever > 0) |
| Queue position update latency | < 1s |
| Ticket delivery time | < 30s |

## The on-sale "war room" dashboard

A single live view for each active on-sale, combining ops + business:

- Queue: waiting count, admit rate, admitted total, conversion %.
- Inventory: sold / held / remaining, sell-through velocity, ETA sell-out.
- Payments: attempt rate, success %, decline %, PSP latency, PSP health.
- System: request rate, error rate, p50/p99 latency, saturation (CPU, Redis
  ops, DB connections, Kafka lag).
- Anomalies: bot-score spikes, hold-expiry rate, refund rate.

This is what lets an operator (or organizer) *see* a PSP wobble or a bot attack
in seconds and pull the right lever (throttle admissions, failover PSP, raise
challenge level).

## Alerting

- **Page (critical):** any oversell, payment success-rate cliff, checkout error
  spike, inventory store saturation, Kafka consumer lag runaway, all-PSP-down.
- **Warn:** rising decline rate, elevated hold-expiry, cache hit-rate drop,
  approaching autoscale ceilings.
- Alerts are **actionable** and tied to runbooks; on-sales have a **pre-briefed
  on-call rotation** and a game-day rehearsal.

## Health, readiness, and capacity

- Liveness/readiness probes on every service; readiness gates traffic during
  warm-up.
- **Synthetic checks**: a bot that walks browse→hold→checkout→refund
  continuously against production canaries.
- Capacity dashboards show headroom vs. the scheduled on-sale forecast.

## Post-incident & post-event

- Every on-sale gets an **after-action review**: what the funnel did, where
  time/inventory was lost, PSP performance, bot activity.
- Blameless post-mortems for incidents feed back into load tests and runbooks.
