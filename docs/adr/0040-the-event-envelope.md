# ADR-0040 — Every event says where it came from

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

## Alternatives considered

**Widen `IntegrationEvent`.** Rejected above: nineteen records and ~sixty construction sites, for
fields no handler reads.

**Rely on `traceparent` alone.** Rejected. It is sampled, it expires, and it is not written beside
the row it explains. The two are complements: the trace is for now, the envelope is for later.

**A separate `messages` table per service, joined on ids.** More faithful to a message-log design,
but it duplicates the outbox for no gain — the outbox row is already the record of what was
published, and it already has a lifetime.
