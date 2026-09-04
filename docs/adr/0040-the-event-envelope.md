# ADR-0040 — Every event says where it came from, and every failure has somewhere to go

**Status:** Accepted · **Date:** 2026-09-04

## Context

A published integration event carried three things: its own id, when it happened, and the tenant it
belonged to. Nothing about **why** it existed.

That is fine while a question is asked in the moment — W3C `traceparent` propagates through
OpenTelemetry, so a live trace shows a request fanning out across services. It stops being fine the
moment the question is asked later:

1. **Traces expire and are sampled.** "Why does this ticket exist?" asked next quarter has no
   answer, because the trace that knew is gone. Nothing durable was written beside the row.
2. **A support ticket starts with a screenshot, not a trace id.** A buyer reports a failed checkout.
   There was no id on the response for them to quote, so the only way in is a timestamp and a guess.
3. **Audit cannot be built.** v9's AUD-007 wants correlation and causation on every audit record.
   Building the audit store first and retrofitting the ids means rebuilding it, which is why the
   alignment ledger listed this as the last structural gap.
4. **A contract could not be changed safely.** With no version on the wire, the only safe way to
   change what an event means is to deploy every producer and consumer together.
5. **A message that could not be handled was retried forever.** No subscription had a dead-letter
   topic and no resiliency policy capped retries, so Dapr's default — redeliver indefinitely —
   applied. One poison message blocks its topic for everything behind it, and the only trace is log
   noise nobody is watching.

## Decision

**Every published event carries an envelope: a correlation id, a causation id and a contract
version. The envelope travels beside the event, not inside it.**

- **`CorrelationId`** — one id for everything descending from a single originating action. A buyer
  pressing Pay produces an order, a payment, sold seats, tickets and an email; all of them carry it,
  across five databases.
- **`CausationId`** — the single message that directly triggered this one. Walking these one hop at
  a time reconstructs the *order* of what happened; the correlation id alone gives only the
  unordered set.
- **`EventVersion`** — from an `[EventVersion(n)]` attribute, defaulting to 1, so a producer and a
  consumer can move independently.

### Why beside, and not on `IntegrationEvent`

Widening the base record is the obvious move. It is also the wrong one here: all nineteen contract
records declare the base fields **positionally**, so adding three would rewrite every one of them
and every place one is constructed — a large, risky diff for plumbing no domain handler reads.

Instead the envelope lives on the outbox row, and `OutboxRelay` merges it into the published JSON
under a reserved `envelope` property. Two consequences make this the better trade:

- **Consumers did not change.** `System.Text.Json` ignores properties it does not know, so all
  eleven existing subscribers kept binding their typed record with no edit.
- **The stored payload stays the plain domain event.** The envelope is about *delivery*; keeping it
  out of the row means a payload can still be replayed into a handler unchanged.

It is plain JSON rather than CloudEvent extension attributes deliberately. Dapr is the broker today
and Service Bus or Kafka could be tomorrow (ADR-0004); a reserved property survives that, a
broker-specific header convention does not.

### Where the ids come from

`ICorrelationContext` sits in `EventPlatform.Auditing`, beside `IAuditContext`, because they are the
same shape of ambient per-scope value answering adjacent questions: **who** did this, and **which
piece of work** is this part of.

Three places fill it, and between them there is no path that produces an unattributed event:

| Entry point | Correlation | Causation |
|---|---|---|
| A browser request | The gateway's `X-Correlation-Id`, forwarded by YARP | none — a person started it |
| A pub/sub message | The envelope's correlation id | **the message's own id** |
| A background service | Self-seeded on first read | none — a timer started it |

The middle row is the one that makes a chain a chain. Note that the incoming *message* becomes the
causation, not the incoming envelope's causation field: what caused the work happening here is the
message that arrived.

### The id is also for people

`CorrelationContextMiddleware` echoes the id on every response, and it is added to ProblemDetails
**in every environment** — unlike the exception details beside it, which stay Development-only. It
leaks nothing: it is an opaque grouping key that means something only to someone who can already
query the databases. The gateway exposes the header through CORS, without which the SPA could never
read back an id it needs to show a buyer.

A caller-supplied id is trusted only as a grouping key. Nothing reads it to decide access or
tenancy, so a forged one can only muddle a trail the forger already appears in. An unparseable one
is replaced rather than rejected — failing a checkout over a malformed diagnostic header would be
the worse trade.

### And a dead letter goes somewhere

Every subscription now names a dead-letter topic, **one per service rather than one per topic**.
Dapr delivers a dead letter back to the app that failed, so per-service means one drain endpoint per
service instead of one per subscription — and the message's own envelope already says which topic it
came from.

Both halves of a subscription are applied by a single `SubscribesTo(topic, deadLetterTopic)` call,
because they are only useful together and each fails silently alone. `check-endpoint-conventions.py`
rejects a bare `.WithTopic(...)` for the same reason.

Two things make this real rather than decorative:

- **A resiliency policy caps retries** (`platform/dapr/components/resiliency.yaml`). Without it
  Dapr retries forever and a message never *reaches* the dead-letter topic — the DLQ would exist and
  never receive anything. Five attempts over roughly ten seconds: long enough to ride out a service
  still starting or a database not yet accepting connections, short enough that a genuinely poisoned
  message stops blocking the topic.
- **A drain reads the topic.** `DeadLetterDrain` records the message verbatim — envelope, payload,
  correlation, causation — and logs at Error. A dead-letter topic nobody reads is just a quieter
  silence than an infinite retry loop.

The dead-letter store is deliberately **separate from the outbox**: the outbox is about producing
and this is about consuming, and the two sets of services are not the same. Communication and Queue
subscribe without ever publishing, and making them carry an outbox — plus a relay polling a table
that is always empty — to get a dead-letter table would be paying for the wrong thing.

**There is no read API for dead letters yet, on purpose.** It is an operator's view of other
tenants' message payloads, and this platform has no operator role — only organizer and buyer. Behind
`RequireOrganizer` it would leak one tenant's messages to another, which is worse than the
inconvenience of reading the table directly. It lands with the permissions work.

## Consequences

- **PLAT-015 closes.** Correlation ids exist platform-wide, surface in ProblemDetails, and are
  written to the database rather than only to a trace.
- **AUD-007 is unblocked.** Audit still does not exist; when it does, the ids it needs are already
  on the wire and in the outbox.
- **Communication's `CorrelationId` was renamed to `CausationId`**, which is what it has always
  held: the id of the triggering event. One name for two meanings is how a trail becomes
  unreadable. It now carries both, correctly.
- **The outbox grows three columns**, and `CorrelationId` is indexed — the question it exists to
  answer would otherwise be a full scan of a table that only ever grows.
- **A message with no readable envelope is still handled**, with a fresh correlation id and no
  causation. An untraceable seat sale is a gap in a record; a rejected one is a customer without
  their seat.
- **We now buffer the request body on subscriber endpoints** to read the envelope and rewind. Pub/sub
  messages here are small and bounded, so this costs nothing that matters — but it is a real
  constraint on ever publishing a large payload.
- **A failing subscriber now gives up after five attempts** instead of retrying forever. That is the
  point, and it is also a behaviour change: an outage longer than ~10 seconds in a downstream
  dependency will dead-letter messages that would previously have eventually succeeded. The drain
  keeps them, so nothing is lost — but replaying them is a manual step until a replay path exists.
- **Every subscribing service gains a `dead_letters` table**, including Communication and Queue,
  which had no messaging tables at all before.

## Alternatives considered

**Widen `IntegrationEvent`.** Rejected above: nineteen records and ~sixty construction sites, for
fields no handler reads.

**Rely on `traceparent` alone.** Rejected. It is sampled, it expires, and it is not written beside
the row it explains. The two are complements: the trace is for now, the envelope is for later.

**A separate `messages` table per service, joined on ids.** More faithful to a message-log design,
but it duplicates the outbox for no gain — the outbox row is already the record of what was
published, and it already has a lifetime.

**One shared dead-letter topic for the whole platform.** Simpler to name, but every service
subscribing to it would receive every other service's failures, and Dapr gives no way to filter
that. Per-service is the only shape that delivers a failure back to the service that caused it.

**Automatic replay from the drain.** Tempting and wrong for now: a dead letter is usually a bug, and
replaying it before the bug is fixed just fails again — while replaying a *money* message that
partially succeeded could double-charge. Replay needs a person deciding, and a person needs the read
API that does not exist yet.
