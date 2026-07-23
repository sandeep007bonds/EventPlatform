# 09 — Delivery Roadmap

A phased plan that gets a **correct, safe** platform live first, then layers on
the extreme-scale machinery and the nice-to-haves. Each phase is shippable.

## Guiding sequencing principle

**Correctness before scale.** A platform that never oversells and never
double-charges at modest load is worth far more than a fast one that
occasionally does either. We nail the invariants first (Phase 1–2), then make
them survive a million people (Phase 3).

## Phase 0 — Foundations (weeks 0–3)
- Repo/mono-structure, CI/CD, IaC skeleton, environments (dev/staging/prod).
- Auth service (OIDC), API gateway, observability baseline (OTel, dashboards).
- Event bus (Kafka), Postgres, Redis provisioned.
- **Decision lock-in**: cloud + language stack, buy-vs-build waiting room & bot.

## Phase 1 — Core selling MVP (weeks 3–10)
- Event & catalog service; simple storefront (browse + event page).
- Inventory & hold service with **atomic holds, TTL, reaper, ledger** — the
  no-oversell core (GA + basic seated).
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

## Open decisions

These need your input before/early in build — they materially shape the design:

1. **Cloud + primary language.** Docs currently recommend **Azure + .NET**
   (given the AZ-204 context) with Go for the hot path. Alternatives: AWS/GCP,
   Java/Spring, Node. → *Your call.*
2. **Waiting room: buy vs build.** Recommend **buy for v1** (Queue-it /
   Cloudflare) to de-risk; build later. → *Your call.*
3. **Bot management: buy vs build.** Recommend **buy**. → *Your call.*
4. **Seated vs GA priority.** Which matters first for your target events
   (stadium seated football vs GA concerts)? Affects Phase 1 scope.
5. **Regions / data residency.** Which markets first (affects PSPs, tax,
   compliance, latency)?
6. **Resale stance.** Facilitate resale, block it, or cap it (varies by law &
   brand)?
7. **Build target now.** Do you want the next step to be a **working
   proof-of-concept of the no-oversell inventory + hold core** (the riskiest
   piece), or a **clickable storefront prototype**?

---

### Suggested immediate next step

Once you confirm decisions 1 and 7, a great first build is a **thin vertical
slice**: browse → hold (atomic, no-oversell) → pay (Stripe test mode) → ticket,
with a load test proving zero oversell under concurrency. That single slice
de-risks the hardest part of the whole platform.
