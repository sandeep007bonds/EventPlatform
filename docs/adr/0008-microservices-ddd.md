# ADR-0008 — Decomposition: DDD bounded contexts, database-per-service

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

The platform spans distinct capabilities (catalog, inventory, orders, payments,
ticketing, waiting room, notifications, reporting, auth). We need independent
deployability and independent scaling, especially for the hot path.

## Decision

Decompose by **DDD bounded context**, one microservice per context, each owning
its data (**database-per-service**). Services interact only via APIs or events —
never by reaching into another service's store.

## Consequences

- Independent deploy + scale; the Inventory / Waiting-Room hot path scales
  separately from everything else.
- Database-per-service is enforced from day one (sharing a database is the one
  thing that silently destroys independent deployability).
- Requires disciplined contracts and eventual consistency across contexts
  (handled by ADR-0010).

## Alternatives considered

- **Modular monolith** — simpler early, but can't scale/isolate the hot path
  per-tenant as required. Rejected for this workload.
- **Shared database across services** — rejected outright; it re-couples
  deployments.
