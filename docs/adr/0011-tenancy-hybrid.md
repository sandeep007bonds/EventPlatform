# ADR-0011 — Multi-tenancy: hybrid (pooled + cell isolation)

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

A central multi-tenant SaaS. Tenants (organizers) range from small to whales
running stadium on-sales. A single tenant's spike must not degrade others (noisy
neighbor), and some enterprise clients need stronger data separation.

## Decision

**Hybrid tenancy:**

- **Control plane pooled**, everything scoped by `tenant_id`.
- **Data pooled in PostgreSQL with Row-Level Security** by `tenant_id`; a whale
  can be **promoted to a dedicated schema/database** with no code change.
- **Hot path uses event-level cells**: shared Redis + general node pool by
  default; a big on-sale is **promoted to its own cell** (dedicated Redis shard +
  inventory partition + tainted AKS node pool).
- **Tenant context** (`tenant_id`, `event_id`) flows from a validated JWT claim
  through every call, event, and query; never trusted from the request body.
- **Noisy-neighbor controls**: per-tenant gateway quotas/rate limits, per-event
  admission rate, cell promotion for whales.

## Consequences

- Pooled economics for the long tail, siloed safety for whales; promotion is an
  operational config action, not a re-architecture.
- Directly leverages the AKS node-pool isolation from ADR-0002.
- RLS enforces isolation at the database, not just in app code.
- Requires rigorous tenant-context propagation and testing.

## Alternatives considered

- **Fully pooled** — cheapest, but weak isolation and the highest noisy-neighbor
  risk. Rejected.
- **Fully siloed** — strong isolation but expensive and slow to onboard.
  Rejected.
