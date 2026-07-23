# ADR-0010 — Messaging: event-driven, orchestrated saga, transactional outbox

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

Services must stay decoupled and independently deployable, yet coordinate flows
that span several of them (notably checkout: inventory → order → payment →
ticketing). We must never lose events, oversell, or double-charge.

## Decision

- **Async, event-driven by default** using **choreography** — services
  publish/subscribe domain events; new consumers plug in without changing
  producers.
- **Synchronous calls (gRPC internal / REST external) only when an answer is
  needed now** (e.g., validate a hold).
- **Orchestration for the checkout/payment saga** via a **durable workflow**
  (Dapr Workflow), with explicit **compensations** (release hold, refund).
- **Transactional Outbox** on every event-emitting service (state change +
  outgoing event in one DB transaction; a relay publishes them).
- Messaging backbone: **Azure Service Bus** (transactional) + **Event Hubs /
  Kafka API** (high-throughput streaming / audit), behind Dapr pub/sub.

## Consequences

- Extensibility: adding a service = subscribing to existing events.
- Reliability: the outbox guarantees no lost events without two-phase commit; the
  durable workflow survives crashes and resumes idempotently.
- Some added complexity (eventual consistency, idempotent consumers) — the
  correct trade for this domain.

## Alternatives considered

- **Pure choreography for checkout** — too little control/visibility for money
  flows. Rejected in favour of orchestration there.
- **Synchronous request/response throughout** — tight coupling, fragile under
  spikes. Rejected.
- **Dual-write without an outbox** — risks lost events. Rejected.
