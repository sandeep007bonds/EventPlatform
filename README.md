# EventPlatform — Enterprise Ticketing Platform

A high-performance, reliable ticketing platform for large-scale live events
(football matches, concerts like Coldplay, festivals) designed to handle
**flash-sale traffic** — hundreds of thousands of concurrent users competing
for a limited, perishable inventory of seats.

> **Status:** Planning / design phase. This repository currently contains the
> architecture and feature-flow documentation. No application code yet.

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

## Open decisions (to confirm before build)

See [09 — Delivery Roadmap](docs/09-delivery-roadmap.md#open-decisions). The
biggest one is **cloud & language stack** — this repo currently recommends a
cloud-native, Azure-friendly option (given the AZ-204 context) with clear
alternatives.
