# Detailed Design

This directory holds the engineering design that sits below the
[ADRs](../adr/) and the [architecture overview](../02-architecture.md).

| Doc | Scope | Purpose |
|-----|-------|---------|
| [High-Level Design (HLD)](hld.md) | System-wide | Component catalog, interfaces, AKS deployment, cross-cutting concerns |
| [Data Flow Diagrams (DFD)](dfd.md) | System-wide | L0 context, L1 processes, L2 drill-downs (checkout, seat hold), trust boundaries |
| [Low-Level Design — Phase 1 (seated)](lld-phase1-seated.md) | Phase 1 slice | Build-ready detail: schemas, Redis/Lua, sequences, APIs, concurrency, test plan |

## Design ladder

```
ADRs           →  why we chose each approach (immutable decisions)
HLD            →  what the parts are and how they fit (system-wide)
DFD            →  how data moves between parts (system-wide)
LLD            →  how to build a given slice (per phase, just-in-time)
```

The HLD and DFDs are living, system-wide references. LLDs are written **per
phase, just before build** — Phase 1 (seated) is done here; later phases (waiting
room, GA, reporting, etc.) get their own LLDs when scheduled, to avoid designing
detail that would be reworked.
