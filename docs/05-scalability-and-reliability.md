# 05 — Scalability & Reliability

The whole system is shaped by one workload: a **scheduled flash sale**. Traffic
is predictable in *timing* but extreme in *magnitude*. That predictability is
our biggest advantage — we prepare for the spike instead of merely reacting.

## The load-shedding funnel

Requests are filtered down at every layer so the strongly-consistent core sees
only a trickle of the raw spike:

```
1,000,000 arrivals
      |  CDN cache + static assets (browsing never hits origin)
      v
   Edge WAF + bot management  --> bots/junk dropped
      v
   Waiting room (Redis)       --> holds everyone, admits at a safe rate
      v
   Admitted flow (e.g. 5k/s)  --> API gateway (authz, rate limit)
      v
   Inventory store (Redis atomics + Postgres) --> the only strongly-consistent hot path
```

Each layer is cheaper-per-request and scales more easily than the one below it.
The expensive, correctness-critical layer only ever sees the admitted flow.

## Scaling each layer

| Layer | Strategy |
|-------|----------|
| **CDN / edge** | Cache event pages, seat-map SVGs, JS/CSS, approximate availability. Effectively infinite read scale. |
| **Waiting room** | Lightweight, Redis-backed, scales horizontally; static assets on CDN. Sized to hold *everyone*. |
| **API gateway** | Stateless, autoscaled; per-user + global rate limiting; sheds excess. |
| **Stateless services** (event, order, ticketing, etc.) | Horizontal pod autoscaling; no sticky state. |
| **Inventory (Redis)** | Clustered, sharded **by event** so a hot event uses dedicated shards; atomic ops avoid locks. |
| **Databases** | Read replicas for reads; **partition/shard inventory & orders by event**; connection pooling (PgBouncer). |
| **Event bus (Kafka)** | Partitioned by event/entity; scales throughput linearly; buffers downstream. |
| **Reporting** | Fully separate read path; can never back-pressure selling. |

## Pre-scaling for scheduled on-sales

Autoscaling alone reacts too slowly for a 0→million spike in seconds. So:

- **Scheduled scale-out** of pods, Redis, and DB connections minutes before the
  published on-sale time.
- **Cache pre-warming**: event pages, seat maps, and read models warmed ahead.
- **Load tests** replaying realistic on-sale patterns against a staging
  environment before major events.
- **Cell isolation** (optional) for mega-events: dedicated capacity so one huge
  on-sale can't degrade everything else.

## No-oversell under concurrency (the crown jewel)

Recap of the mechanism (details in [Ticket Selling](feature-flows/02-ticket-selling.md)):

- **GA:** atomic `DECRBY` with revert-if-negative in Redis.
- **Seated:** atomic Lua check-and-set (all-or-nothing) in Redis, mirrored to
  Postgres with optimistic concurrency / `SELECT FOR UPDATE` as system of record.
- **Holds + TTL + reaper + ledger** guarantee inventory always returns and is
  never double-counted.
- **Postgres is the source of truth**; Redis is a fast cache; a **reconciler**
  repairs any drift. If Redis is ever lost, state is rebuilt from Postgres/ledger.

## Reliability & availability

| Concern | Approach |
|---------|----------|
| **AZ failure** | Multi-AZ deployment; synchronous replication for core transactional data. |
| **Region failure (DR)** | Active-passive standby region; async replication; documented failover; RTO < 15m, RPO < 1m. |
| **Service failure** | Stateless + autoscaled + health-checked; K8s reschedules. |
| **Dependency failure** | Timeouts, retries w/ jittered backoff, circuit breakers, bulkheads, PSP failover. |
| **Partial failures** | Sagas with compensation; the system self-heals to a safe state. |
| **Data loss** | Kafka retained log + Postgres PITR backups + append-only ledgers. |
| **Poison/overload** | Rate limiting, load shedding, graceful "try again shortly" over hard errors. |

## Graceful degradation ladder

When under stress, degrade in this order (protect selling + correctness last):

1. Serve **staler** availability/browse data from cache.
2. **Tighten** the queue admission rate.
3. Disable non-essential features (recommendations, live seat animations).
4. Defer all non-critical async work (analytics, marketing events).
5. As a last resort, **pause new admissions** — never oversell, never take
   money we can't confirm.

## Testing for the spike

- **Load & soak tests** simulating the funnel end-to-end.
- **Chaos testing**: kill nodes, inject PSP/Redis latency, drop a whole AZ.
- **Game days**: rehearse a mega on-sale with the on-call team and the war-room
  dashboards before it's real.
