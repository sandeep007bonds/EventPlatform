# Queue service

A virtual waiting room for high-demand on-sales. Opt-in per event
(`Event.RequiresQueue`, organizer-configured on Catalog) — most events never
touch this service at all.

## How it works

1. Catalog publishes an event. Queue subscribes to `EventPublished` (its
   only Dapr use) and provisions per-event `QueueSettings`, `Enabled` fixed
   from `EventPublished.RequiresQueue` at that moment, with sensible pacing
   defaults an organizer can tune afterward.
2. A buyer who wants to select seats for a queueing-enabled event first
   calls `POST /v1/events/{eventId}/queue/join` with a client-generated
   session id (anonymous — no login required, same posture as browsing).
   If queueing isn't enabled for the event, this immediately returns an
   admission token — a one-branch no-op, not a special code path.
3. Otherwise the session joins a Redis-backed FIFO sorted set. The buyer
   polls `GET /v1/events/{eventId}/queue/status` until admitted.
4. A background `QueueAdmissionController` promotes the longest-waiting
   sessions on each event's own configured pace, minting a short-lived
   HMAC-signed admission token for each.
5. The buyer presents that token when placing a hold
   (`POST /v1/holds`, Inventory) — Inventory verifies the signature and
   expiry **locally**, with zero call back to Queue, the same
   "propagate once, verify locally" hot-path philosophy ADR-0025 already
   established for Ticketing's scan cache.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/integration/catalog/event-published` | Dapr pub/sub topic `EventPublished` → provision this event's queue settings |
| POST | `/v1/events/{eventId}/queue/join` | Join or resume a waiting-room session (anonymous) |
| GET | `/v1/events/{eventId}/queue/status` | Poll a session's current status (anonymous) |
| GET | `/v1/events/{eventId}/queue/settings` | Read an event's pacing configuration (tenant-owned) |
| PUT | `/v1/events/{eventId}/queue/settings` | Tune admission rate/interval/session TTL (tenant-owned; `Enabled` is not editable here) |

## Local run

```bash
dapr run --app-id queue \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/queue/Queue.Api
```

Run Catalog too (same Dapr setup) so `EventPublished` flows through pub/sub.
Needs the same Redis instance Inventory already uses locally (`localhost:6380`)
under its own `queue:` key prefix — no separate Redis container needed.

## Layers

`Queue.Api` (host + endpoints + Dapr subscription) · `Queue.Application`
(join/status handlers + settings provisioning + ports) · `Queue.Domain`
(`QueueSettings`) · `Queue.Infrastructure` (EF Core + Postgres for
settings, the Redis waiting-room store, HMAC admission-token issuance, the
admission background service — no outbox, Queue never publishes an
integration event).

See [service CLAUDE.md](CLAUDE.md) and
[ADR-0026](../../docs/adr/0026-virtual-waiting-room-queue-service.md).
