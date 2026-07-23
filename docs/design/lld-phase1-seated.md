# Low-Level Design (LLD) — Phase 1, Seated Slice

**Scope:** the Phase 1 vertical slice from [ADR-0013](../adr/0013-phase1-seated.md)
— the minimum needed to sell a **seated** ticket correctly under concurrency:
**browse → atomic seat hold → checkout → payment → ticket**, proven by a load
test showing **zero oversell**.
**Related:** [HLD](hld.md), [DFD](dfd.md), [ADR-0009](../adr/0009-service-internal-pattern.md),
[ADR-0010](../adr/0010-messaging-and-sagas.md), [ADR-0012](../adr/0012-payments.md).

Other services (Search, Waiting Room, Reporting, etc.) are **out of scope for
this LLD** and will be designed just-in-time per phase.

---

## 1. Services in the slice

| Service | Phase 1 role | Depth |
|---------|--------------|-------|
| **Catalog** | Publish a seated event; serve event + seat map (read) | Minimal |
| **Inventory & Hold** | Atomic seat hold, TTL, reaper, ledger, convert-to-sold | **Full** |
| **Order** | Order lifecycle + checkout saga orchestration | **Full** |
| **Payment** | Stripe (test) integration, idempotent charge, webhook inbox | **Full** |
| **Ticketing** | Generate ticket + QR on `OrderConfirmed`, email it | Minimal |
| **Auth** | Issue JWT with `tenant_id`/`user_id` claims | Reused |

No Waiting Room in Phase 1 — concurrency is capped artificially by load-test
config (per roadmap Phase 1 goal).

## 2. Standard service internal structure (ADR-0009)

Each service is a .NET 10 solution using Clean Architecture + Vertical Slices.
Reference layout (Order service shown):

```
services/order/
├─ src/
│  ├─ Order.Api/                # Minimal API host, Dapr, DI, middleware
│  │  └─ Program.cs
│  ├─ Order.Application/        # Vertical slices (features)
│  │  └─ Features/
│  │     ├─ CreateOrder/        # Command, Handler, Validator, Endpoint
│  │     ├─ GetOrder/
│  │     └─ Checkout/           # saga trigger
│  ├─ Order.Domain/            # Entities, value objects, domain events, invariants
│  ├─ Order.Infrastructure/    # EF Core, outbox, Dapr adapters, PSP client port impl
│  └─ Order.Workflow/          # Dapr Workflow: checkout saga + activities
├─ tests/
│  ├─ Order.UnitTests/
│  └─ Order.IntegrationTests/  # Testcontainers: Postgres + Redis + Dapr
├─ db/migrations/              # SQL migrations
├─ Dockerfile
└─ chart/                      # Helm chart (values per env)
```

- **Slices** own their command/query + handler + validator + endpoint. Adding a
  feature = adding a folder.
- **Domain** holds invariants; **Infrastructure** implements ports (DB, bus,
  PSP) as adapters.
- **Inventory** deviates: leaner (Minimal API, hand-tuned data access, no
  MediatR on the hold path) for latency (ADR-0009).

## 3. Data model (DDL sketch)

Per-service databases (ADR-0008). Money stored as integer **minor units**.

### Catalog (minimal for seated)
```sql
CREATE TABLE event (
  id            UUID PRIMARY KEY,
  tenant_id     UUID NOT NULL,
  venue_id      UUID NOT NULL,
  title         TEXT NOT NULL,
  starts_at     TIMESTAMPTZ NOT NULL,
  status        TEXT NOT NULL,        -- draft|published|on_sale|sold_out|...
  on_sale_at    TIMESTAMPTZ,
  currency      CHAR(3) NOT NULL
);
CREATE TABLE seat (
  id            UUID PRIMARY KEY,
  venue_id      UUID NOT NULL,
  section       TEXT NOT NULL,
  row_label     TEXT NOT NULL,
  seat_number   TEXT NOT NULL,
  x             INT, y INT           -- for map rendering
);
CREATE TABLE price_tier (
  id            UUID PRIMARY KEY,
  event_id      UUID NOT NULL,
  section       TEXT NOT NULL,
  price_minor   BIGINT NOT NULL,
  fee_minor     BIGINT NOT NULL DEFAULT 0
);
```

### Inventory (system of record + ledger)
```sql
CREATE TABLE inventory_item (
  id            UUID PRIMARY KEY,
  tenant_id     UUID NOT NULL,
  event_id      UUID NOT NULL,
  seat_id       UUID NOT NULL,
  price_tier_id UUID NOT NULL,
  status        TEXT NOT NULL,        -- available|held|sold|blocked
  version       INT  NOT NULL DEFAULT 0,   -- optimistic concurrency
  UNIQUE (event_id, seat_id)
);
CREATE INDEX ix_inv_event_status ON inventory_item(event_id, status);
-- RLS: enforce tenant_id on every query
ALTER TABLE inventory_item ENABLE ROW LEVEL SECURITY;

CREATE TABLE hold (
  id            UUID PRIMARY KEY,
  tenant_id     UUID NOT NULL,
  event_id      UUID NOT NULL,
  user_id       UUID NOT NULL,
  order_id      UUID,                 -- set when checkout starts
  expires_at    TIMESTAMPTZ NOT NULL,
  status        TEXT NOT NULL,        -- active|converted|released
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE hold_item (
  hold_id       UUID NOT NULL REFERENCES hold(id),
  inventory_item_id UUID NOT NULL,
  PRIMARY KEY (hold_id, inventory_item_id)
);

CREATE TABLE inventory_ledger (      -- append-only, immutable
  id            BIGSERIAL PRIMARY KEY,
  inventory_item_id UUID NOT NULL,
  from_status   TEXT, to_status TEXT NOT NULL,
  cause         TEXT NOT NULL,        -- hold|release|sold|reap
  ref_id        UUID,                 -- holdId / orderId
  at            TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### Order & Payment
```sql
CREATE TABLE orders (
  id              UUID PRIMARY KEY,
  tenant_id       UUID NOT NULL,
  user_id         UUID NOT NULL,
  event_id        UUID NOT NULL,
  hold_id         UUID NOT NULL,
  status          TEXT NOT NULL,      -- pending|awaiting_payment|confirmed|failed|refunded
  total_minor     BIGINT NOT NULL,
  currency        CHAR(3) NOT NULL,
  idempotency_key TEXT NOT NULL,
  created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  expires_at      TIMESTAMPTZ NOT NULL,
  UNIQUE (tenant_id, idempotency_key)
);
CREATE TABLE order_line (
  id UUID PRIMARY KEY, order_id UUID NOT NULL,
  inventory_item_id UUID NOT NULL, price_minor BIGINT, fee_minor BIGINT
);
CREATE TABLE payment (
  id UUID PRIMARY KEY, order_id UUID NOT NULL,
  psp TEXT NOT NULL, psp_intent_id TEXT,
  idempotency_key TEXT NOT NULL,
  status TEXT NOT NULL,               -- initiated|captured|failed|refunded
  amount_minor BIGINT NOT NULL,
  UNIQUE (order_id, idempotency_key)
);
CREATE TABLE webhook_inbox (
  provider TEXT NOT NULL, event_id TEXT NOT NULL,
  received_at TIMESTAMPTZ DEFAULT now(), processed BOOL DEFAULT false,
  PRIMARY KEY (provider, event_id)     -- dedupe
);
-- Transactional outbox (in every writing service)
CREATE TABLE outbox (
  id BIGSERIAL PRIMARY KEY, aggregate_id UUID, type TEXT,
  payload JSONB, created_at TIMESTAMPTZ DEFAULT now(), published BOOL DEFAULT false
);
```

## 4. Redis structures (Inventory hot path)

| Key | Type | Purpose |
|-----|------|---------|
| `inv:{eventId}:seat:{seatId}` | string | status: `A` (available) / `H:{holdId}` (held) / `S` (sold) |
| `inv:{eventId}:hold:{holdId}` | string, **EX ttl** | hold marker; TTL drives expiry |
| `inv:{eventId}:hold:{holdId}:seats` | set | seatIds in the hold (for reaper) |

Redis is the fast gate; **Postgres is truth**. A reconciler repairs drift.

### Atomic hold — Lua script (executed via `EVAL`, atomic)
```lua
-- KEYS: seat status keys...  ARGV[1]=holdId, ARGV[2]=ttlSeconds, ARGV[3..]=seatIds
-- 1) verify ALL seats available
for i, key in ipairs(KEYS) do
  if redis.call('GET', key) ~= 'A' then
    return {err = 'CONFLICT:' .. ARGV[i+2]}
  end
end
-- 2) all-or-nothing: flip to held
for i, key in ipairs(KEYS) do
  redis.call('SET', key, 'H:' .. ARGV[1])
end
-- 3) hold marker with TTL (hold key passed in ARGV) + seat set for the reaper
redis.call('SET', holdKey, '1', 'EX', tonumber(ARGV[2]))
return {ok = 'HELD'}
```
The check-and-set is atomic because Redis runs the whole script single-threaded
— no interleaving between the availability check and the flip. This is the core
of no-oversell.

## 5. Sequence — Seat hold

```mermaid
sequenceDiagram
    participant U as Client
    participant INV as Inventory API
    participant R as Redis
    participant PG as Postgres

    U->>INV: POST /holds {eventId, seatIds} + JWT
    INV->>R: EVAL hold.lua (atomic check+set, TTL)
    alt all seats available
        R-->>INV: HELD
        INV->>PG: BEGIN; UPDATE inventory_item SET status=held, version+1<br/>WHERE id=ANY(items) AND status=available; INSERT hold+ledger; COMMIT
        alt rows updated == requested
            INV->>INV: outbox: SeatHeld
            INV-->>U: 201 {holdId, expiresAt}
        else lost the race in PG
            INV->>R: revert seats to A, del hold key
            INV-->>U: 409 Conflict
        end
    else conflict
        R-->>INV: CONFLICT:seatId
        INV-->>U: 409 Conflict {seatId}
    end
```

Redis is the fast rejecter (99% of contention); Postgres optimistic update is
the final authority. If Postgres disagrees (rare), Redis is reverted.

## 6. Sequence — Checkout saga (Dapr Workflow, orchestrated)

```mermaid
sequenceDiagram
    participant U as Client
    participant ORD as Order (Workflow)
    participant INV as Inventory
    participant PAY as Payment
    participant PSP as Stripe
    participant BUS as Bus

    U->>ORD: POST /checkout {holdId} + Idempotency-Key
    ORD->>ORD: dedupe on (tenant, idempotency_key)
    ORD->>INV: validate hold (owned, active, not expired)
    INV-->>ORD: OK + line prices
    ORD->>ORD: enforce per-user limit; order=awaiting_payment
    ORD->>PAY: Charge(order, key)
    PAY->>PSP: PaymentIntent(amount, key)
    PSP-->>U: 3DS (if required, client-side)
    PSP-->>PAY: succeeded (+ webhook to inbox)
    PAY-->>ORD: PaymentCaptured
    ORD->>INV: ConvertHoldToSold(holdId)  %% idempotent
    INV-->>ORD: Sold
    ORD->>ORD: order=confirmed
    ORD->>BUS: OrderConfirmed
    Note over ORD: Compensation on failure
    ORD-->>INV: ReleaseHold(holdId)
    ORD-->>PAY: Refund (only if captured)
```

**Workflow activities** (each idempotent, each with a compensation):
`ValidateHold` → `CreateOrder` → `ChargePayment` (comp `Refund`) →
`ConvertToSold` → `ConfirmOrder`. The Dapr Workflow persists state, so a crash
mid-saga resumes deterministically.

## 7. Sequence — Hold expiry (reaper)

```mermaid
sequenceDiagram
    participant R as Redis
    participant RP as Reaper
    participant PG as Postgres
    participant BUS as Bus

    R-->>RP: keyspace expiry: inv:{e}:hold:{holdId}
    RP->>R: read hold seat-set; SET seats A (if still H:holdId)
    RP->>PG: UPDATE inventory_item status=available WHERE held;<br/>UPDATE hold status=released; INSERT ledger(cause=reap) -- idempotent by holdId
    RP->>BUS: HoldReleased
```
Idempotent release keyed by `holdId`: replays/late expiries are no-ops. A
periodic sweep backstops missed keyspace notifications.

## 8. API contracts (Phase 1)

```
POST /v1/holds                      -> 201 {holdId, expiresAt} | 409 | 403
  body: {eventId, seatIds[]}        headers: Authorization, X-Admission-Token (later)
DELETE /v1/holds/{holdId}           -> 204
POST /v1/checkout                   -> 202 {orderId, status}
  body: {holdId}                    headers: Idempotency-Key
POST /v1/orders/{id}/payment        -> 200 {clientSecret}   headers: Idempotency-Key
GET  /v1/orders/{id}                -> 200 {status, tickets?[]}
POST /internal/webhooks/stripe      -> 200 (signature-verified, inbox-deduped)
GET  /v1/events/{id}                -> 200 (cached)
GET  /v1/events/{id}/seatmap        -> 200 (cached, seat status best-effort)
```
Errors: RFC 7807 problem+json. All POSTs creating money/inventory require
idempotency keys.

## 9. Concurrency & correctness matrix

| Risk | Mechanism |
|------|-----------|
| Two buyers, same seat | Redis Lua atomic check-set + Postgres optimistic `version` update; all-or-nothing |
| Double-click checkout | `UNIQUE(tenant_id, idempotency_key)` on orders |
| Double charge | PSP idempotency key + payment `UNIQUE(order_id, idempotency_key)` |
| Convert-to-sold replay | Idempotent by `holdId`/`orderId`; ledger guards |
| Hold never released | Redis TTL + reaper + periodic sweep; idempotent release |
| Webhook replay | `webhook_inbox` PK dedupe |
| Charged but hold expired | TTL(hold) > max payment time; else auto-refund compensation |
| Redis/Postgres drift | Postgres is source of truth; reconciler repairs Redis |
| Cross-tenant access | JWT `tenant_id` claim + Postgres RLS |

## 10. Config (defaults, tunable per event)

| Setting | Default |
|---------|---------|
| Hold TTL | 600s (10 min) |
| Max seats per hold | 6 |
| Per-user limit / event | 4 |
| Order (awaiting_payment) TTL | 900s |
| Payment timeout | 30s + 3DS window |
| Reaper sweep interval | 30s (backstop to keyspace events) |

## 11. Test plan (the Phase 1 exit criteria)

1. **Unit** — hold invariants, saga compensations, idempotent handlers.
2. **Integration** (Testcontainers: Postgres + Redis + Dapr) — hold→checkout→
   ticket happy path; expiry/reaper; webhook dedupe.
3. **Concurrency / load** — the headline test: N virtual users contend for a
   fixed seat inventory (e.g., 1,000 users, 200 seats).
   - **Pass = seats sold == seats available, oversell == 0**, every loser gets a
     clean 409, no stuck holds after TTL, no double charges (assert against
     Stripe test dashboard + `payment` table).
4. **Chaos** — kill the Order pod mid-saga; assert the workflow resumes and the
   invariants still hold.

Meeting criterion 3 with **zero oversell under load** is the definition of done
for Phase 1 and de-risks the hardest part of the platform.
