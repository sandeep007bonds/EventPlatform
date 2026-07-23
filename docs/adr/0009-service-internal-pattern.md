# ADR-0009 — Per-service pattern: Clean Architecture + Vertical Slices + CQRS

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

Services must be extensible (adding features should be additive and low-risk) and
testable. We also have one latency-critical service (Inventory) where framework
overhead matters.

## Decision

Each service uses **Clean / Hexagonal Architecture** (domain at the center,
infrastructure as adapters) + **Vertical Slice Architecture** (a feature is a
slice with its own command/handler/validator, via MediatR) + **light CQRS**
(separate read/write models).

**Exception:** the **Inventory hot path** is deliberately leaner — Minimal APIs,
tight code, minimal pipeline — trading pattern purity for speed.

## Consequences

- Adding a feature = adding a slice; low blast radius → the "easily extensible"
  goal.
- Infrastructure (DB, PSP, bus) is swappable behind ports/adapters.
- Consistency enforced via the shared service template (ADR-0007).
- Slight upfront boilerplate per service; accepted.

## Alternatives considered

- **Layered (Controller → Service → Repository)** — changes thread through layers;
  higher coupling. Rejected for extensibility.
- **Event sourcing everywhere** — powerful but heavy; reserved for where it earns
  its keep (inventory ledger, audit), not mandated globally.
