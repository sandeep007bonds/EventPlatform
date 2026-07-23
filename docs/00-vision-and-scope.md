# 00 — Vision & Scope

## Vision

Build the platform that powers on-sales for the world's largest live events —
where fairness, reliability, and raw throughput matter more than anything else.
When 500,000 people show up in the same second to buy 60,000 seats, the
platform stays up, sells every seat exactly once, treats fans fairly, and takes
payment correctly.

## Who uses it

| Actor | What they do |
|-------|--------------|
| **Fan / buyer** | Browses events, joins the queue, picks seats, pays, receives tickets, enters the venue. |
| **Event organizer** | Creates events, configures venues/pricing/inventory, runs presales, watches live sales, pulls reports, issues refunds. |
| **Venue / gate staff** | Scan and validate tickets at entry (online or offline). |
| **Platform admin** | Manages organizers, fees, fraud rules, and global config. |
| **Partner / 3rd party** | Payment gateways, messaging, wallets, anti-fraud, resale partners, and API consumers. |

## In scope (v1 → v2)

- Event & venue setup, seat maps, ticket types, pricing tiers.
- Public storefront: discovery, event pages, seat selection.
- Virtual waiting room for high-demand on-sales.
- Inventory management with holds, TTL, and guaranteed no-oversell.
- Checkout & payment with a reliable payment saga.
- Ticket issuance (QR / barcode / wallet passes) and delivery.
- Access control / scanning at the gate.
- Organizer dashboard + reporting/analytics.
- Third-party integrations (payments, messaging, anti-bot, wallets).
- Refunds, cancellations, and event rescheduling.

## Explicitly out of scope (initially)

- Full secondary-market (resale) exchange — **designed for, but delivered
  later** as a controlled marketplace.
- Native mobile apps — v1 is a responsive PWA; native apps follow.
- Livestreaming / virtual events.
- Sponsorship/ad management.

## Success metrics (targets)

| Metric | Target |
|--------|--------|
| Oversell rate | **0** (hard invariant) |
| On-sale availability | 99.95%+ during peak windows |
| Queue → checkout admission latency | Controlled; users see accurate position within ~1s |
| Checkout p99 latency (post-admission) | < 2s |
| Payment double-charge rate | **0** |
| Peak concurrent users supported | 500k+ in queue, 10k+/s admitted checkout attempts (tunable) |
| Ticket delivery time after purchase | < 30s (async, guaranteed) |

## Scale assumptions (design envelope)

These are the numbers the architecture is sized against. Tune per event.

- **Queue:** up to ~1,000,000 waiting users per event.
- **Admission rate into the store:** configurable, e.g. 1,000–10,000 users/min,
  set to match backend/inventory capacity.
- **Inventory operations:** tens of thousands of atomic hold/release ops per
  second, per hot event.
- **Catalog:** millions of events over time; a handful "hot" at once.
- **Reads vs writes:** browse/read traffic dwarfs writes; the read path is
  cached and CDN-fronted aggressively.
