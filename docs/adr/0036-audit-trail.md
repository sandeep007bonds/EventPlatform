# ADR-0036 — Audit trail: append-only, written in the same transaction, shipped to one store

**Status:** Proposed · **Date:** 2026-08-20

## Context

An inventory of all 34 domain entities across the nine services found the platform has
effectively no audit capability:

| Gap | State today |
|---|---|
| **Actor** | **Zero** `CreatedBy`/`UpdatedBy` on any entity in any service. Nothing anywhere records *who* did anything. |
| **Modified time** | Only `QueueSettings` has `UpdatedAt`. |
| **Created time** | 8 of 34 entities. Absent on every Catalog entity except `PromoCode` — including `Event` — and on `Ticket`. |
| **State transitions** | `Event.Publish()` sets a status and nothing else. `PauseSales()`/`ResumeSales()` flip a bool. `PromoCode.Deactivate()` leaves no timestamp. `UpdateDetails()` can rewrite 18 fields, including the tax rate, with no trace. |
| **Deletes** | `DELETE /seatmap/sections/{name}` is a hard delete. No tombstones anywhere. |
| **Consistency** | No `SaveChanges` interceptor. Every `CreatedAt` that exists is set by hand in a factory, so coverage is inconsistent by construction. |
| **Concurrency** | `Version` on Inventory's two hot entities only; everything else can lose an update silently. |

Two gaps stand out as more than bookkeeping. `Ticket.CheckedInAt` records *when* a ticket was
scanned but not *who* scanned it, so a disputed entry has nobody attached to it. And
`Event.UpdateDetails` rewriting pricing-adjacent fields untracked means a tax rate can change with
no record of who changed it or from what.

Three things already exist and are the right shape to build on rather than replace:

- **`Inventory.LedgerEntry`** — a genuine append-only ledger: `FromStatus`, `ToStatus`, `Cause`,
  `RefId`, `At`. Its only real gap is an actor.
- **`Communication.DeliveryLogEntry`** — channel, recipient, status, provider reference,
  correlation id, `SentAt`.
- **The transactional outbox** — `OccurredAt` plus payload for anything that publishes an
  integration event.

The outbox is *not* an audit log and must not be mistaken for one: it is a delivery mechanism,
its rows are pruned once published, and it covers only events a service chose to publish — not
reads, not failed attempts, not authentication.

**Scope was set as "full regulatory trail" without naming a regime.** The mechanism below is
regime-independent; the values a regime pins down (retention period, which events are mandatory,
whether tamper-evidence must be cryptographic, data residency) are configuration, and are called
out as such. **This ADR cannot be marked Accepted until a regime is named** — the design is
buildable now, the parameters are not.

## Decision

### Write locally in the same transaction, then ship to one store

Each service writes audit records into **its own database, in the same transaction as the change
being audited**. That is the only way to guarantee there is no change without its audit record —
a separate call, however reliable, can fail after the write commits.

Those records are then relayed to a **central Audit service** through the existing outbox, giving
one queryable place with one retention policy. This mirrors the platform's existing pattern
rather than inventing a second one, and keeps database-per-service intact: nobody reads anyone
else's tables.

Local write is the source of truth for *integrity*; the central store is the source of truth for
*querying and retention*.

### One record shape

`AuditEntry` (in a new `EventPlatform.Auditing` building block):

`Id` · `TenantId?` · `OccurredAt` · `Actor` · `ActorType` · `Action` · `EntityType` · `EntityId` ·
`Changes` (JSON, before/after per changed property) · `CorrelationId` · `ServiceName` ·
`PreviousHash` · `Hash`

### Actors, including the ones that are not people

`ActorType` is `User`, `Service`, or `System`. This matters more than it looks: much of what
happens in this platform is done by the checkout saga, the expired-hold reaper, or the queue
admission controller, none of which has a `ClaimsPrincipal`. Recording those as a null user would
make the log lie by omission. `IAuditContext` resolves the current actor from the request
principal where there is one, and services declare a fixed service identity where there is not.

### Captured by an interceptor, not by hand

An `ISaveChangesInterceptor` reads EF's change tracker and writes an `AuditEntry` per modified
entity, taking before/after from `OriginalValues`/`CurrentValues`. Hand-written audit calls were
rejected: they are exactly what produced today's 8-of-34 coverage.

Entities opt in via a marker so the log carries business facts rather than every row touched.
Domain-specific records that already exist (`LedgerEntry`, `DeliveryLogEntry`) stay where they
are — they are richer than a generic diff — and gain an actor.

### Authentication events need their own path

Logins, failed logins, OTP requests and verifications are not EF entity changes and the
interceptor will never see them. Identity records them through an explicit `IAuditWriter` call.
For a regulatory trail these are usually the *most* scrutinised records, so leaving them to fall
out of entity tracking would miss the point.

### Tamper evidence: hash chain

Each record stores a hash over its own content plus the previous record's hash, per service. That
makes silent modification or deletion of a record detectable by re-walking the chain. It is not
proof against an attacker who can rewrite the whole chain — that needs external anchoring
(periodic publication of the head hash to append-only storage), which is listed as future work.
The chain is worth having regardless: it converts "we think the log is intact" into something
checkable.

### PII and the erasure conflict, stated rather than buried

An immutable audit log is in direct tension with a data-subject erasure request. The resolution
here:

- The log stores **pseudonymous actor ids** (the `sub` GUID), never names, emails or phone numbers.
- Properties holding personal data are **redacted in `Changes`** — the property name and the fact
  it changed are recorded, the values are not.
- Erasing a subject deletes the subject's record; the audit entries referencing their id survive,
  which is the standard position for records kept under a legal obligation.

This is a real constraint on what the log may contain, not a footnote, and it has to be settled
with whoever owns the compliance question before the interceptor is written — once a field is
being captured, un-capturing it retroactively is not possible.

## Consequences

- **Every write path gains a second write.** Bounded, but it lands on the checkout hot path.
  Inventory's hot path is deliberately lean (see the service structure notes) and needs measuring
  before its high-frequency operations are audited by the generic interceptor — its `LedgerEntry`
  may remain the better instrument there.
- **Audit tables grow without bound** until retention is implemented. Retention needs a named
  regime, so the first cut ships with the data accumulating and pruning unimplemented — that has
  to be a conscious acceptance, not a surprise.
- **Every service gets a migration** adding its audit table, plus a new Audit service with its own
  database, deployment, gateway route and Argo CD manifests.
- **The entity-field gaps are still worth closing independently.** A central log answers "who
  changed this and when"; `CreatedAt`/`UpdatedAt` on the row answers it without a join, and the
  absence of `CreatedAt` on `Event` is a plain modelling gap regardless of any audit subsystem.
- **`Ticket` should record the scanning actor directly**, not only in the audit log. It is a
  domain fact about the check-in, not incidental metadata.

## Alternatives considered

- **A central Audit service only, called synchronously.** One store, no per-service migrations —
  but a change can then commit while its audit write fails, which defeats the purpose. Rejected
  on the integrity requirement.
- **Audit purely from the outbox / integration events.** Free, and already partly there. Covers
  only what services publish: no reads, no failed attempts, no authentication, and rows are pruned.
  Good enough for an activity feed, not for a regulatory trail.
- **Temporal tables** (`SYSTEM_VERSIONING`). The database keeps full row history with no
  application code. Genuinely attractive for before/after, but it records no actor and no intent —
  the two things most needed — and would tie the schema to a database feature that ADR-0029
  deliberately kept the platform loosely coupled to.
- **Event sourcing the aggregates.** The most complete answer and a rewrite of every service.
  Disproportionate.
