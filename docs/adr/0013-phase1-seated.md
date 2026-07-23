# ADR-0013 — Phase 1 scope: seated events first

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

The platform supports both seated (assigned-seat) and general-admission (GA)
events. We need a focused Phase 1 that de-risks the hardest correctness problem.

## Decision

**Phase 1 targets seated events.** The first vertical slice proves seated
no-oversell under concurrency: browse → atomic seat hold (Redis Lua + Postgres
optimistic concurrency + TTL/reaper/ledger) → payment (Stripe test) → ticket,
validated by load tests showing zero oversell.

## Consequences

- Tackles the hardest inventory case (individual seat contention) first; GA
  (atomic counter) is simpler and follows.
- Focuses early effort on the correctness core, per the roadmap's "correctness
  before scale" principle.
- The seat-map editor and seated data model are prioritized in Phase 1/2.

## Alternatives considered

- **GA first** — simpler but defers the riskiest problem; less representative of
  the stadium/football target events. Rejected.
