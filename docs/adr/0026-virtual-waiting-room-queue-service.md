# ADR-0026 — Virtual waiting-room Queue service

- **Status:** Accepted
- **Date:** 2026-08-06

## Context

The last remaining item from `docs/progress-tracker.md`'s deferred
product-features table (P1): a virtual waiting room for high-demand
on-sales, gating seat selection behind a paced admission queue so a sudden
crush of concurrent buyers doesn't hit Inventory's hold-placement path all
at once. Opt-in per event — most events never need it.

Two existing patterns in this codebase map onto the problem directly:

1. **Inventory's Redis-backed hold system** (`RedisHoldStore`,
   `ExpiredHoldReaper`) already solves "an atomic, horizontally-safe Redis
   operation gates access to a scarce resource, with a `BackgroundService`
   doing periodic reconciling work" — the queue's admission logic is
   structurally the same problem, just gating admission to the hold path
   itself rather than gating a specific seat.
2. **ADR-0025's "propagate once, verify locally, zero cross-service calls
   on the hot path" philosophy** (just shipped for Ticketing's scan cache)
   applies directly to the token check inside `HoldService.PlaceHoldAsync`
   — a live call from Inventory to Queue at hold time would reintroduce
   exactly the anti-pattern ADR-0025 just removed.

## Decision

### New service: `services/queue/`

Full Clean Architecture layering (`Queue.Domain`/`Application`/
`Infrastructure`/`Api`/`tests`), mirroring Communication/Identity's shape —
not Media's deliberately flat shape, since this service has real business
logic (admission pacing, atomicity) worth separating from I/O. Port
5088/7088.

### `Event.RequiresQueue` (Catalog) is the single on/off source of truth

Follows the exact precedent already used for `OnSaleAt`/`BookingEndsAt`/
`MaxTicketsPerBuyer`: organizer-configurable on `Event`, Draft-only,
propagated via `EventPublished` to whichever services need to act on it —
here, both Inventory (to require a token at hold time) and Queue (to
provision `QueueSettings.Enabled` for that event). Queue's own settings
(`AdmissionRatePerInterval`/`IntervalSeconds`/`SessionTtlSeconds`) are
independently tunable via its own `PUT /settings` endpoint, but deliberately
carry **no** `Enabled` field — a second, independently-settable toggle would
risk disagreeing with `Event.RequiresQueue`, which is worse than having
only one. Unlike most Catalog-propagated settings, Queue's pacing knobs
**are** editable post-publish — pacing only matters once an event is
actually live/on-sale, so the Draft-only norm doesn't apply here.

### Redis-only waiting-room state — a deliberate asymmetry with ADR-0025

`RedisQueueStore` holds a per-event FIFO sorted set (`queue:{eventId}:waiting`,
scored by Redis's own `TIME` command) and per-session admission markers
(`queue:{eventId}:admitted:{sessionId}`, a TTL'd string — presence alone
means admitted, the same "presence/absence carries the state" convention
`RedisHoldStore` already uses). None of this is persisted to Postgres.
This is the opposite choice from ADR-0025's durable scan cache, on purpose:
losing a queue position on a Redis restart means "back of the line," not a
lost sale or broken check-in — the added durability ADR-0025 needed isn't
worth the complexity here. Only `QueueSettings` (organizer config,
low-frequency, needs real durability) gets a Postgres table — Queue's own
small `queue` database, one table.

`QueueAdmissionController` (a `BackgroundService`) mirrors `ExpiredHoldReaper`'s
shape: one shared `PeriodicTimer` tick (2s) drives many independently-paced
events, tracked via an in-process (not persisted) last-promoted-at map —
losing that on a restart just means one event's next promotion happens
slightly early, never a correctness problem. Promotion itself
(`IQueueStore.PromoteBatchAsync`, backed by `ZPOPMIN`) is atomic per call,
so concurrent admission-controller ticks — even across multiple Queue.Api
replicas, should replica count ever increase beyond this repo's current
`replicas: 1` default — can never double-promote the same session; each
concurrent caller gets a disjoint slice of the front of the line, with no
extra locking required.

### HMAC-signed admission tokens, verified locally by Inventory

An admitted session is issued a token
(`{eventId:N}.{sessionId:N}.{expUnixSeconds}.{signatureBase64}`, HMAC-SHA256)
by `HmacAdmissionTokenIssuer`. `HoldService.PlaceHoldAsync` checks
`settings.RequiresQueue` and, if set, verifies the presented token
**locally** via a new `IQueueAdmissionTokenValidator` port
(`HmacQueueAdmissionTokenValidator` in `Inventory.Infrastructure`) —
recomputing the same HMAC against a **shared secret**
(`QueueAdmission:HmacKey`, identical dev value committed in both services'
`appsettings.Development.json`, a single Terraform-generated Key Vault
secret in the real environment). Zero network call to Queue at
hold-placement time — the same "propagate once, verify locally" hot-path
philosophy ADR-0025 established for Ticketing's scan, applied here to a
capability token instead of a cache. A full RSA/JWKS/OIDC issuer
(Identity's approach) would be overkill for a short-lived, two-service-only
token — a keyed HMAC is the right-sized primitive, the same reasoning
already used for `HmacOtpHasher`.

### Frontend: an anonymous waiting-room page, gating "Hold selection"

A buyer joins with a client-generated session id (`sessionStorage`-backed,
so a page refresh resumes the same position instead of re-enqueueing at the
back) via `POST /v1/events/{eventId}/queue/join` — anonymous, no login
required, consistent with browsing already being anonymous (ADR-0016). The
new `QueueWaitingRoomPage` polls `GET .../queue/status` (no max-attempts
cap, unlike ticket-issuance polling — a queue can legitimately take a long
time) until admitted, then stashes the admission token and auto-navigates
to seat selection. `EventDetailPage`'s "Select seats" routes through the
queue page first when `event.requiresQueue`; `SeatSelectionPage` defends
against a direct URL hit with the same redirect, and reads the stashed
token when placing a hold.

## Consequences

- A new, ninth backend service, with its own database, first new local
  Postgres schema addition since ADR-0025.
- `HoldService.PlaceHoldAsync` gains one more settings-derived check (after
  `MaxTicketsPerBuyer`, before touching Redis/Postgres) and a new
  `PlaceHoldOutcome.QueueAdmissionRequired` → 409.
- `QueueAdmission:HmacKey` becomes the first secret in this repo genuinely
  shared, verbatim, between two different services — a departure from the
  otherwise strict per-service secret ownership, and worth flagging
  explicitly for anyone extending this pattern later.
- Most events are entirely unaffected: `Event.RequiresQueue` defaults to
  `false`, and the whole queue path — provisioning, join/status, the
  admission controller — degrades to an immediate-admit no-op with zero
  `IQueueStore` calls when disabled.

## Alternatives considered

- **A live Dapr call from Inventory to Queue at hold-placement time**
  (mirroring ADR-0024's original scan design) — rejected for the same
  reason ADR-0025 just rejected it for Ticketing: a real network hop
  directly on the hot path this whole feature exists to protect.
- **Persisting queue sessions in Postgres**, matching the scan cache's
  durability — rejected; a queue position has no long-term significance
  the way check-in state does, so the added complexity buys nothing.
- **A single, richer `INotificationSender`-style unified port instead of
  Redis primitives** — not applicable here; the closest analogue considered
  was denormalizing admission state onto `Hold` itself, rejected as a
  larger, riskier change touching Inventory's schema for data that's
  cheaper to keep entirely in Queue's own store.
- **Full RSA/JWKS token issuance for admission tokens** (mirroring
  Identity) — rejected as disproportionate for a short-lived, two-service
  capability token; a shared HMAC secret is the right-sized primitive.

## References

- `services/queue/CLAUDE.md`, `services/queue/README.md` — service design
  notes and endpoint reference.
- ADR-0024/ADR-0025 — the "resolve once, verify/read locally, zero
  cross-service calls on the hot path" precedent this design follows.
- ADR-0021 — `MaxTicketsPerBuyer`'s settings-derived-check-in-`HoldService`
  pattern, which the new `QueueAdmissionRequired` check slots into.
