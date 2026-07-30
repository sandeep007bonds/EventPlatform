# EventPlatform — Enterprise Ticketing Platform

A high-performance, reliable ticketing platform for large-scale live events
(football matches, concerts like Coldplay, festivals) designed to handle
**flash-sale traffic** — hundreds of thousands of concurrent users competing
for a limited, perishable inventory of seats.

> **Status:** Design complete; scaffolding in progress. The repo holds the full
> architecture, ADRs, and detailed design, plus five working services (Catalog,
> Inventory, Ordering, Payments, Ticketing) and a local Docker + Dapr dev stack.
> Run the whole thing with one command — `./scripts/dev-up.sh` — and drive a
> full purchase end to end: see [Local Development](docs/local-development.md)
> and the [local end-to-end walkthrough](docs/local-e2e-walkthrough.md). No
> Azure needed.

## The core problem

Ticketing is not a normal e-commerce workload. It has three properties that,
combined, make it one of the hardest consumer-facing systems to build:

1. **Extreme, predictable spikes.** Traffic goes from near-zero to millions of
   requests in the first seconds of an on-sale. A stadium show can sell 60,000
   seats in minutes.
2. **Perishable, finite, contested inventory.** A specific seat can be sold to
   exactly one person. Overselling is a legal and reputational failure;
   underselling wastes revenue.
3. **Fairness and trust.** Real fans must have a fair shot against bots and
   scalpers, and every payment must be correct to the cent.

Everything in this design — the virtual waiting room, the inventory/hold model,
the payment saga, the read/write split — exists to satisfy those three
constraints at the same time.

## Feature scope

| # | Feature | Doc |
|---|---------|-----|
| 1 | Event creation & organizer dashboard | [feature-flows/01](docs/feature-flows/01-event-creation-dashboard.md) |
| 2 | Ticket selling (browse → hold → checkout) | [feature-flows/02](docs/feature-flows/02-ticket-selling.md) |
| 3 | Virtual waiting queue | [feature-flows/03](docs/feature-flows/03-waiting-queue.md) |
| 4 | Third-party integrations | [feature-flows/04](docs/feature-flows/04-third-party-integrations.md) |
| 5 | Reporting & analytics | [feature-flows/05](docs/feature-flows/05-reporting.md) |
| 6 | Payments | [feature-flows/06](docs/feature-flows/06-payments.md) |

Additional features we recommend adding (details in the docs): dynamic/tiered
pricing, access codes & presales, a controlled resale marketplace, seat maps,
mobile ticket wallet (Apple/Google Wallet), access control / gate scanning, and
a robust anti-bot layer.

## Documentation map

- [00 — Vision & Scope](docs/00-vision-and-scope.md)
- [01 — Requirements (functional & non-functional)](docs/01-requirements.md)
- [02 — System Architecture](docs/02-architecture.md)
- [03 — Technology Stack](docs/03-tech-stack.md)
- [04 — Data Model](docs/04-data-model.md)
- [Feature Flows](docs/feature-flows/) — one document per feature, with sequence diagrams
- [05 — Scalability & Reliability](docs/05-scalability-and-reliability.md)
- [06 — Security & Compliance](docs/06-security-and-compliance.md)
- [07 — Observability](docs/07-observability.md)
- [08 — API Design](docs/08-api-design.md)
- [09 — Delivery Roadmap](docs/09-delivery-roadmap.md)
- [Architecture Decision Records (ADRs)](docs/adr/) — the locked decisions and why
- **Detailed design** ([docs/design/](docs/design/)):
  [HLD](docs/design/hld.md) · [DFD](docs/design/dfd.md) · [LLD — Phase 1 seated](docs/design/lld-phase1-seated.md)
- [Engineering Guidelines (golden rules)](docs/engineering-guidelines.md) · [root CLAUDE.md](CLAUDE.md)
- [Local Development (Docker, no Azure needed)](docs/local-development.md)
- [SOP — Onboarding a new service](docs/onboarding-new-service.md)

## Guiding principles

- **Protect the inventory.** The system of record for "who owns this seat" must
  never oversell, even under massive concurrency. Correctness beats latency.
- **Shed load at the edge.** The waiting room and CDN absorb the spike so the
  core services only ever see a controlled, admitted flow of users.
- **Everything money-related is idempotent and auditable.** Retries are normal;
  double-charges are not.
- **Read and write paths are separated.** Selling is write-heavy and must stay
  fast; reporting is read-heavy and must never slow down selling.
- **Design for failure.** Every external dependency (payment gateway, email,
  SMS) will fail sometimes; the system degrades gracefully, never loses money,
  and never loses a sold seat.

## Architecture decisions (locked)

The foundational decisions are made and recorded as
[ADRs](docs/adr/). In short:

- **Azure**, single-cloud SaaS ([ADR-0001](docs/adr/0001-cloud-provider-azure.md))
- **AKS from day one** for hot-path cell isolation ([ADR-0002](docs/adr/0002-runtime-aks.md))
- **.NET 10 (LTS)** ([ADR-0003](docs/adr/0003-dotnet-10.md))
- **GitHub Actions + Argo CD (GitOps)** ([ADR-0004](docs/adr/0004-cicd-github-actions-argocd.md))
- **Terraform** ([ADR-0005](docs/adr/0005-iac-terraform.md)) and **Dapr** ([ADR-0006](docs/adr/0006-dapr.md))
- **Monorepo** with path-filtered pipelines ([ADR-0007](docs/adr/0007-monorepo.md))
- **DDD bounded contexts, database-per-service** ([ADR-0008](docs/adr/0008-microservices-ddd.md))
- **Clean Architecture + Vertical Slices + CQRS** ([ADR-0009](docs/adr/0009-service-internal-pattern.md))
- **Event-driven + orchestrated saga + outbox** ([ADR-0010](docs/adr/0010-messaging-and-sagas.md))
- **Hybrid multi-tenancy** ([ADR-0011](docs/adr/0011-tenancy-hybrid.md))
- **Payments: saga + idempotency + PCI SAQ-A** ([ADR-0012](docs/adr/0012-payments.md))
- **Phase 1 = seated events** ([ADR-0013](docs/adr/0013-phase1-seated.md))
- **MediatR pinned to v12.5.0** (last free/OSS release) ([ADR-0014](docs/adr/0014-mediator-mediatr-v12.md))

A few product-level questions remain open (target regions, resale stance) —
see [09 — Delivery Roadmap](docs/09-delivery-roadmap.md#open-decisions).

## Engineering guidelines

Before writing code, read the [Engineering Guidelines](docs/engineering-guidelines.md)
and [root CLAUDE.md](CLAUDE.md). The golden rules (one class per file, StyleCop,
XML docs on public API, Central Package Management, layer boundaries, idempotency,
a tracking issue per unit of work) are enforced by `Directory.Build.props`,
`.editorconfig`, `Directory.Packages.props`, and the PR template — not left to
memory.
