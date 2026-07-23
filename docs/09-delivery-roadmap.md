# 09 — Delivery Roadmap

A phased plan that gets a **correct, safe** platform live first, then layers on
the extreme-scale machinery and the nice-to-haves. Each phase is shippable.

## Guiding sequencing principle

**Correctness before scale.** A platform that never oversells and never
double-charges at modest load is worth far more than a fast one that
occasionally does either. We nail the invariants first (Phase 1–2), then make
them survive a million people (Phase 3).

## Phase 0 — Foundations (weeks 0–3)
- Monorepo structure; **AKS** cluster + node pools via **Terraform**; **GitHub
  Actions** CI + **Argo CD** GitOps; environments (dev/staging/prod).
- **Dapr** installed (pub/sub, state, secrets, workflow); **.NET 10** service
  template scaffolded; shared `contracts` package.
- Auth service (OIDC/Entra), API gateway (YARP), observability baseline
  (OpenTelemetry, war-room dashboards).
- Azure Service Bus / Event Hubs, PostgreSQL, Redis provisioned.
- Remaining decision lock-in: buy-vs-build waiting room & bot management.

## Phase 1 — Core selling MVP, seated (weeks 3–10)
- Event & catalog service; simple storefront (browse + event page); seated
  data model + minimal seat map.
- Inventory & hold service with **atomic seated holds, TTL, reaper, ledger** —
  the no-oversell core (Redis Lua + Postgres optimistic concurrency).
- Order service + **payment saga** with one PSP (Stripe), idempotency,
  webhook inbox.
- Ticketing: generate + email tickets.
- **Goal:** end-to-end buy of a real ticket, correct under concurrent load
  tests. No queue yet (cap concurrency artificially).

## Phase 2 — Organizer & operations (weeks 10–16)
- Organizer dashboard: event creation, seat-map editor, pricing, publish.
- Refunds, transfers, cancellations, holdbacks, purchase limits.
- Reporting v1: real-time on-sale metrics + basic financial reports (CQRS read
  path).
- Wallet passes (Apple/Google), SMS notifications.

## Phase 3 — Scale & fairness (weeks 16–24)
- **Waiting room / virtual queue** (buy-integrate or build), admission tokens.
- **Bot & fraud defense** (edge bot mgmt, CAPTCHA, risk scoring).
- Pre-scaling automation, cache pre-warming, event-sharded inventory.
- Multi-PSP failover; multi-AZ hardening; DR runbook.
- Full-scale **load tests + chaos + game day**.
- **Goal:** survive a simulated mega on-sale (hundreds of thousands queued).

## Phase 4 — Platform & ecosystem (weeks 24+)
- Partner API + outbound webhooks; marketplace/payout (Stripe Connect).
- Advanced reporting/BI, anomaly alerts, forecasting.
- Access-control gate app (offline scanning).
- Dynamic/tiered pricing; presale/access-code flows at scale.
- Native mobile apps; controlled **resale marketplace**.
- SOC 2, deeper compliance.

## Recommended extra features (beyond the original 6)

| Feature | Why it matters |
|---------|----------------|
| **Anti-bot / fraud layer** | Without it, scalpers ruin fairness and your brand. Arguably not optional. |
| **Ticket wallet + rotating QR** | Delivery + anti-fraud at the gate. |
| **Access control / scanning** | Closes the loop; offline-capable for venue reality. |
| **Dynamic & tiered pricing** | Revenue optimization; matches demand. |
| **Presale / access codes** | Standard for big tours (fan clubs, cards). |
| **Controlled resale marketplace** | Capture (and legitimize) the secondary market. |
| **Multi-date / tour management** | Big acts sell tours, not single dates. |
| **Group / accessible booking** | Compliance + real-world needs. |
| **Waitlist / notify-me** | Recapture demand after sell-out & on returns. |

## Decisions

### Resolved (see [ADRs](adr/))

1. **Cloud + runtime** → **Azure, single-cloud SaaS** on **AKS from day one**
   ([ADR-0001](adr/0001-cloud-provider-azure.md), [ADR-0002](adr/0002-runtime-aks.md)).
2. **Language** → **.NET 10 (LTS)** ([ADR-0003](adr/0003-dotnet-10.md)).
3. **CI/CD, IaC, infra abstraction** → **GitHub Actions + Argo CD**, **Terraform**,
   **Dapr** ([ADR-0004](adr/0004-cicd-github-actions-argocd.md)–[ADR-0006](adr/0006-dapr.md)).
4. **Repo, decomposition, per-service pattern** → **Monorepo**, **DDD + database-per-service**,
   **Clean Architecture + Vertical Slices + CQRS** ([ADR-0007](adr/0007-monorepo.md)–[ADR-0009](adr/0009-service-internal-pattern.md)).
5. **Messaging & sagas** → **event-driven + orchestrated checkout saga + outbox**
   ([ADR-0010](adr/0010-messaging-and-sagas.md)).
6. **Tenancy** → **hybrid** (pooled + cell isolation) ([ADR-0011](adr/0011-tenancy-hybrid.md)).
7. **Payments** → **saga + idempotency + PCI SAQ-A** ([ADR-0012](adr/0012-payments.md)).
8. **Phase 1 scope** → **seated events first** ([ADR-0013](adr/0013-phase1-seated.md)).

### Still open

- **Waiting room: buy vs build.** Recommend **buy for v1** (Queue-it /
  Cloudflare) to de-risk; build later.
- **Bot management: buy vs build.** Recommend **buy**.
- **Regions / data residency.** Which markets first (affects PSPs, tax,
  compliance, latency)?
- **Resale stance.** Facilitate resale, block it, or cap it (varies by law &
  brand)?

---

### Suggested immediate next step

With the foundational decisions locked (see ADRs), the first build is a **thin
vertical slice** on the seated path: browse → atomic seat hold (no-oversell) →
pay (Stripe test mode) → ticket, with a load test proving zero oversell under
concurrency. That single slice de-risks the hardest part of the whole platform
and exercises the AKS + Dapr + saga stack end to end.
