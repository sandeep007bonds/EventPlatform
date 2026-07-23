# ADR-0014 — In-process mediator: MediatR pinned to v12.5.0

- **Status:** Accepted
- **Date:** 2026-07-23
- **Refines:** [ADR-0009](0009-service-internal-pattern.md)

## Context

ADR-0009 uses Vertical Slice Architecture with an in-process mediator for
command/handler dispatch, referencing MediatR. MediatR has since moved to
commercial licensing: **v13.0+** is dual-licensed (RPL-1.5 / commercial) and
requires a license key; a free **Community** edition exists only for
organizations under **$5M gross annual revenue** that have **never raised
>$10M** in outside capital. **v12.5.0** is the last Apache-2.0 (OSS) release.

As a commercial SaaS intended to scale past those thresholds, the Community tier
is a **tripwire**, not a durable free option — it would convert to a paid
obligation exactly as the business grows.

## Decision

Use **MediatR pinned to v12.5.0** (Apache-2.0, free) for in-process dispatch.
Do **not** upgrade to v13+. Usage is thin (dispatch + a few pipeline behaviors)
and sits behind our own abstraction, so replacement stays cheap.

## Consequences

- No licensing cost and no legal tripwire as the platform scales.
- **No functional loss:** v12.5.0 covers requests/handlers, notifications,
  pipeline behaviors, streaming, cancellation everywhere, and timeout behavior.
  v13 added **no** features we need — it was primarily the licensing change plus
  a netstandard2.0 target.
- **Trade-off:** v12.5.0 is frozen (no future upstream fixes/updates). Low risk
  for a small, mature, stable library; it runs fine on .NET 10.
- **Escape hatch:** thin usage behind an abstraction means migrating to a free
  alternative — or a ~30-line hand-rolled dispatcher — later is a small,
  contained change.
- Pinned in `Directory.Packages.props`; Dependabot must **not** bump it to 13.x.

## Alternatives considered

- **MediatR v13 Community (free now)** — becomes a paid obligation on crossing
  $5M revenue / $10M raised, exactly as we scale. Rejected.
- **MediatR v13 commercial (paid)** — unnecessary cost for a thin dependency.
  Rejected for now.
- **Free / hand-rolled mediator** — viable and fully removes the dependency;
  retained as the ready escape hatch if v12.5.0 ever becomes a problem.

## References

- MediatR v13.0.0 release notes (LuckyPennySoftware/MediatR)
- MediatR v12.5.0 release notes (last Apache-2.0 release)
- Jimmy Bogard — AutoMapper and MediatR commercial editions launch (2025-07-02)
