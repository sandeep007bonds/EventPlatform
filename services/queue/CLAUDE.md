# CLAUDE.md — Queue service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

An opt-in virtual waiting room in front of Inventory's hold-placement path
for high-demand on-sales. Bounded context: **Queue** (ADR-0026). Most events
never have `Event.RequiresQueue` set and never touch this service at all.

## Owns

- **Data store:** PostgreSQL `queue` DB (this service only, one table:
  `QueueSettings`) + Redis (the hot waiting-room state — deliberately not
  durable; see Design notes).
- **Public API:** `POST /v1/events/{eventId}/queue/join`,
  `GET /v1/events/{eventId}/queue/status` (both `.AllowAnonymous()` — joining
  a queue needs no login, same posture as anonymous event browsing);
  `GET`/`PUT /v1/events/{eventId}/queue/settings` (tenant-owned — 401
  without a tenant, 404 on a mismatch, same opaque pattern as
  `DefineSeatMap`). `PUT` only tunes `AdmissionRatePerInterval`/
  `IntervalSeconds`/`SessionTtlSeconds` — `Enabled` is not settable here.
- **Events published:** none.
- **Events consumed:** `EventPublished` (Catalog) → provision this event's
  `QueueSettings`, `Enabled` fixed from `EventPublished.RequiresQueue`.

## Design notes

- **`Event.RequiresQueue` (Catalog) is the single on/off source of truth.**
  Queue's own `QueueSettings.Enabled` is set once at provisioning time from
  that flag and is never independently re-toggled by this service's own
  `PUT /settings` — a second, disagreeing toggle would be worse than none.
  Pacing knobs (`AdmissionRatePerInterval`/`IntervalSeconds`/
  `SessionTtlSeconds`) ARE tunable post-publish, a deliberate exception to
  the Draft-only norm most Catalog-propagated settings follow, because
  pacing only matters once an event is actually live.
- **Redis-only waiting-room state — no Postgres durability for sessions.**
  A queue position is ephemeral: losing it on a Redis restart means "back
  of the line," not a lost sale, unlike Ticketing's scan cache (ADR-0025),
  which is durably persisted precisely because losing it would break
  check-in. This is a deliberate asymmetry, not an oversight — see
  ADR-0026.
- **`RedisQueueStore`** mirrors `RedisHoldStore`'s Lua-script-per-operation
  shape. Key scheme: `queue:{eventId:N}:waiting` (a sorted set, score from
  Redis's own `TIME` command — no separate sequence key), `queue:{eventId:N}:admitted:{sessionId:N}`
  (a plain string whose mere presence, with a TTL, means admitted — the
  same "presence/absence carries the state" convention `RedisHoldStore`
  already uses for seats). `ZPOPMIN`'s atomicity is what actually prevents
  double-promotion under concurrent admission-controller ticks (e.g.
  multiple replicas) — not any additional locking.
- **`QueueAdmissionController`** (a `BackgroundService`) mirrors
  `ExpiredHoldReaper`'s shape: one shared `PeriodicTimer` tick drives many
  independently-paced events, tracked via an in-process (not persisted)
  last-promoted-at map — losing that on a restart just means one event's
  next promotion happens slightly early, never a correctness problem.
- **Admission tokens are HMAC-SHA256-signed, not RSA/JWKS.** A full
  OIDC-style issuer (Identity's approach) is overkill for a short-lived,
  two-service-only capability token — the same "keyed HMAC is the right
  primitive here" reasoning already used for `HmacOtpHasher`. Format:
  `{eventId:N}.{sessionId:N}.{expUnixSeconds}.{signatureBase64}`. Verified
  **locally by Inventory** (`HmacQueueAdmissionTokenValidator`) against the
  **same shared secret** (`QueueAdmission:HmacKey`, identical dev value in
  both services' `appsettings.Development.json`) — zero cross-service call
  at hold-placement time, the same "propagate once, verify locally"
  hot-path philosophy ADR-0025 established for Ticketing's scan.
- **Join rate limiting is charged per session *created*, not per request.**
  The waiting room paces access but says nothing about who is asking, so a
  script minting fresh session ids could take as many places in line as it
  liked. `IJoinRateLimiter`/`RedisJoinRateLimiter` caps new sessions per
  client per event (default 10/minute, deliberately generous — shared
  addresses are ordinary, and turning away real buyers mid-purchase costs
  more than letting a script hold a few places). A buyer refreshing the page
  resumes the same session and is never charged, which is why the enqueue
  script reports `CREATED` vs `RESUMED` and `QueueStoreResult.WasCreated`
  exists. The counter is in Redis, not process memory, so the allowance is
  not multiplied by the replica count; `INCR` and `EXPIRE` run in one script
  so a crash between them cannot leave a TTL-less counter locking a client
  out permanently. Both limiter operations **fail open** — this is abuse
  mitigation, not a security control, and denying genuine buyers during a
  Redis blip would do more damage than the abuse.
- **`Queue.Api` runs `UseForwardedHeaders`** so the limiter sees the real
  caller rather than the gateway, which would otherwise bucket every buyer
  together and let a handful of joins close the waiting room. The proxy
  allow-list is cleared because a pod address is not knowable ahead of time,
  so anything able to reach this service directly can choose its own bucket
  — tolerable for a limiter, and **not** to be relied on by anything that
  grants access on the strength of a caller's address.
- **No replay/one-time-use enforcement on a token.** An admitted buyer could
  technically place more than one hold within their admission window — not
  fixed here; a hold is still fully capacity/limit-checked on its own
  merits (`BookingEndsAt`/`MaxTicketsPerBuyer`/actual availability), so the
  queue's job is pacing *access*, not acting as a second purchase-limit
  mechanism.

## Structure

`Queue.Api` (host + endpoints + Dapr subscription — no outbound Dapr calls
of its own) · `Queue.Application` (`Queueing/` join+status handlers,
`Provisioning/`, `Abstractions/` ports) · `Queue.Domain` (`QueueSettings`) ·
`Queue.Infrastructure` (EF Core + Postgres, the Redis waiting-room store,
`Admission/` — HMAC token issuer + the admission background service — no
outbox). `tests/Queue.Tests`.

## Local run

```bash
dapr run --app-id queue \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/queue/Queue.Api
```

Run Catalog too (same Dapr setup) so `EventPublished` flows. Reuses
Inventory's Redis instance (`localhost:6380`) under its own `queue:` key
prefix — no separate Redis container needed locally.

## Dead letters

Every subscription here goes through `.SubscribesTo(topic, DeadLetterTopic)`, which adopts the
message's correlation chain and names `deadletter-queue` for anything this service cannot handle
(ADR-0040). Dapr retries five times first — a resiliency policy caps it, without which a poison
message would be redelivered forever and never reach the dead letter at all.

`OnDeadLetterAsync` drains that topic into the `dead_letters` table and logs at Error. There is no
read API for it yet: it is an operator's view of message payloads and this platform has no operator
role.

## Do not

- Give this service its own independent `Enabled` toggle — `Event.RequiresQueue`
  (Catalog) is the only on/off switch; `PUT /settings` tunes pacing only.
- Call this service from Inventory's hold-placement path — the admission
  token is verified locally there; a live call would reintroduce exactly
  the hot-path cross-service call ADR-0025 removed from Ticketing.
- Persist individual queue sessions to Postgres — the waiting-room state is
  deliberately Redis-only/ephemeral (see Design notes).
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
