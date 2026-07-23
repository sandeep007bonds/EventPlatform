# Data Flow Diagrams (DFD)

**System:** EventPlatform
**Related:** [HLD](hld.md), [LLD (Phase 1)](lld-phase1-seated.md)

DFDs describe how **data moves** between external entities, processes, and data
stores. Notation used here (rendered with Mermaid):

- **External entity** — rounded/stadium node `([ ])`
- **Process** — rectangle `[ ]`
- **Data store** — cylinder `[( )]`
- **Data flow** — arrow, labelled with the data in motion
- **Trust boundary** — subgraph box

---

## Level 0 — Context diagram

The whole platform as a single process, showing external actors and systems and
the data crossing the boundary.

```mermaid
flowchart TB
    Fan([Fan / Buyer])
    Org([Organizer])
    Gate([Gate Staff])

    P0[EventPlatform]

    PSP([Payment Gateway])
    MSG([Email / SMS / Wallet])
    BOT([Bot Mgmt])
    IDP([Identity Provider])

    Fan -->|search, queue join, seat selection, payment details| P0
    P0 -->|queue position, held seats, order status, tickets| Fan

    Org -->|event config, pricing, holdback release| P0
    P0 -->|live sales, reports, payouts| Org

    Gate -->|scanned ticket token| P0
    P0 -->|valid / invalid, entry status| Gate

    P0 -->|charge / refund request| PSP
    PSP -->|auth result, settlement webhooks| P0

    P0 -->|notifications, passes| MSG
    Fan -->|challenge response| BOT
    BOT -->|bot score / verdict| P0
    P0 <-->|token validation| IDP
```

## Level 1 — Major processes

Decomposes the platform into its principal processes and the data stores they
read/write. This is the system's data-flow backbone.

```mermaid
flowchart TB
    Fan([Fan])
    Org([Organizer])

    subgraph Platform
        P1[1.0 Discovery / Browse]
        P2[2.0 Waiting Room / Admission]
        P3[3.0 Seat Hold]
        P4[4.0 Checkout & Payment]
        P5[5.0 Ticketing & Delivery]
        P6[6.0 Event Setup]
        P7[7.0 Reporting]
    end

    D1[(Catalog store)]
    D2[(Queue store - Redis)]
    D3[(Inventory: Redis + Postgres + Ledger)]
    D4[(Order & Payment store)]
    D5[(Ticket store)]
    D6[(Analytics warehouse)]
    BUS{{Event Bus}}
    PSP([PSP])

    Org -->|event, seats, pricing| P6 -->|inventory items| D3
    P6 -->|catalog record| D1
    P6 -->|EventPublished| BUS

    Fan -->|search| P1 -->|read| D1
    P1 -->|approx availability| Fan

    Fan -->|on-sale arrival| P2 <-->|enqueue, rank| D2
    P2 -->|admission token| Fan
    P2 -->|UserAdmitted| BUS

    Fan -->|select seats + token| P3 <-->|atomic hold check-set| D3
    P3 -->|held / TTL| Fan
    P3 -->|SeatHeld / HoldReleased| BUS

    Fan -->|checkout + idempotency key| P4 <-->|order, payment| D4
    P4 <-->|validate hold, convert to sold| D3
    P4 <-->|charge| PSP
    P4 -->|OrderConfirmed / OrderFailed| BUS

    BUS -->|OrderConfirmed| P5 -->|ticket, QR| D5
    P5 -->|tickets / passes| Fan

    BUS -->|all domain events| P7 -->|aggregates| D6
    P7 -->|dashboards, reports| Org
```

Notes:
- **Reads (1.0)** touch only cached catalog + read models — never the inventory
  write store.
- **The bus** decouples producers from read-side consumers (Search, Reporting,
  Ticketing, Notification).
- **Exact availability** is only ever resolved inside **3.0 Seat Hold**,
  atomically.

## Level 2 — Drill-down: Checkout & Payment (process 4.0)

The riskiest flow, expanded. Shows idempotency keys, the hold conversion, and
the compensation path. (Sequenced step-by-step in the [LLD](lld-phase1-seated.md).)

```mermaid
flowchart TB
    Fan([Fan])
    PSP([PSP])

    subgraph P4[4.0 Checkout & Payment]
        P41[4.1 Validate hold + limits]
        P42[4.2 Create order - idempotent]
        P43[4.3 Charge PSP - idempotent]
        P44[4.4 Convert hold to sold]
        P45[4.5 Confirm order]
        P4C[4.C Compensate: release hold / refund]
    end

    D3[(Inventory + Ledger)]
    D4[(Order & Payment)]
    DINBOX[(Webhook inbox)]
    BUS{{Event Bus}}

    Fan -->|checkout, holdId, idempotencyKey| P41
    P41 <-->|hold valid & owned?| D3
    P41 -->|limit counter| D4
    P41 --> P42 -->|order = awaiting_payment| D4
    P42 --> P43 -->|PaymentIntent + key| PSP
    PSP -->|succeeded| P43
    PSP -->|webhook, signed| DINBOX -->|deduped| P43
    P43 --> P44 <-->|status held to sold - idempotent| D3
    P44 -->|ledger entry| D3
    P44 --> P45 -->|order = confirmed| D4
    P45 -->|OrderConfirmed| BUS
    P45 -->|success| Fan

    P43 -.payment failed/timeout.-> P4C
    P41 -.hold invalid.-> P4C
    P4C -->|release hold / refund| D3
    P4C -->|order = failed / refunded| D4
    P4C -->|OrderFailed| BUS
```

## Level 2 — Drill-down: Seat Hold (process 3.0)

```mermaid
flowchart TB
    Fan([Fan])

    subgraph P3[3.0 Seat Hold]
        P31[3.1 Verify admission token]
        P32[3.2 Atomic check-and-hold - Redis Lua]
        P33[3.3 Persist hold + ledger - Postgres]
        P34[3.4 Reaper: expire holds]
    end

    RED[(Redis: seat status, TTL)]
    PG[(Postgres: inventory_item, hold)]
    LED[(inventory_ledger)]
    BUS{{Event Bus}}

    Fan -->|seatIds + admission token| P31 --> P32
    P32 <-->|all available? set held+TTL| RED
    P32 -->|success| P33
    P33 -->|optimistic update version| PG
    P33 -->|append transition| LED
    P33 -->|SeatHeld| BUS
    P33 -->|held + expiresAt| Fan
    P32 -.conflict.-> Fan

    RED -.key expiry event.-> P34
    P34 -->|status -> available| RED
    P34 -->|release, keyed by holdId| PG
    P34 -->|append release| LED
    P34 -->|HoldReleased| BUS
```

## Data store catalog

| Store | Owner | Contents | Consistency |
|-------|-------|----------|-------------|
| Catalog store | Catalog | events, venues, seat maps, pricing | Strong (writes), cached reads |
| Queue store (Redis) | Waiting Room | queue sorted-sets, admission tokens | Ephemeral |
| Inventory (Redis) | Inventory | hot seat/GA status, holds w/ TTL | Strong, atomic |
| Inventory (Postgres) | Inventory | durable inventory_item, hold | Strong (system of record) |
| inventory_ledger | Inventory | append-only state transitions | Strong, immutable |
| Order & Payment store | Order/Payment | orders, order_lines, payments, refunds | Strong, ACID |
| Webhook inbox | Payment | raw PSP events, deduped | Strong |
| Ticket store | Ticketing | tickets, scans, transfers | Strong |
| Analytics warehouse | Reporting | aggregates, historical | Eventual (OLAP) |
| Event log (Hubs/Kafka) | Platform | retained domain events | Durable, replayable |

## Trust boundaries

```mermaid
flowchart LR
    subgraph Internet
        U([Users / Bots])
    end
    subgraph EdgeZone[Edge - untrusted -> filtered]
        CDN[CDN + WAF + Bot Mgmt]
        WR[Waiting Room]
    end
    subgraph AppZone[Private cluster - authenticated]
        GW[API Gateway]
        SVC[Services]
    end
    subgraph DataZone[Data - private subnets]
        DB[(Postgres / Redis / Bus)]
    end
    subgraph ThirdParty[External processors]
        PSP([PSP])
    end

    U --> CDN --> WR --> GW --> SVC --> DB
    SVC <-->|TLS + idempotency| PSP
```

- Card data **never** enters AppZone/DataZone — it goes browser → PSP directly
  (PCI SAQ-A).
- `tenant_id` is only trusted from a validated JWT at the gateway, never from
  request bodies.
- DataZone is reachable only from AppZone (private subnets, no public ingress).
