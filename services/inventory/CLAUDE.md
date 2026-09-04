# CLAUDE.md — Inventory service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

The no-oversell system of record. Owns seat availability, general-admission
capacity pools, holds (with TTL, covering either or both in one hold), the
convert-to-sold path, and the immutable inventory ledger. Bounded context:
**Inventory & Hold** (ADR-0008). Generates inventory when Catalog publishes a
**performance**, and enforces that performance's booking cutoff.

## The grain: one performance, not one event (ADR-0039)

Everything here is keyed by `EventSessionId`. A three-night run is one Catalog
event with three performances, and each has its own inventory — the same
physical seat is three separately sellable rows, because it is.

Three things are deliberately **not** re-keyed, and each carries a
denormalised `CatalogEventId` for the purpose:

| Event-scoped, on purpose | Why |
|---|---|
| `MaxTicketsPerBuyer` counting | A cap counted per night lets one buyer take it three times over on a three-night run. `GetBuyerCommittedQuantityAsync` takes a `catalogEventId` and its parameter doc says so. |
| The Queue admission token | The waiting room gates the **on-sale**, and an on-sale covers the whole run. Validated against `settings.CatalogEventId`. |
| `OnSaleAt` / `RequiresQueue` | Event-level decisions, copied onto every performance's settings row so a hold needs one lookup, not two. |

**Redis keys are scoped to the performance** (`inv:{eventSessionId:N}:...`). This
is the one place where getting the grain wrong fails silently rather than
loudly: a seat id is a *Venue* seat — the same chair on Friday and Saturday — so
an event-scoped key would mark A1 taken for the whole run the moment anyone held
it for one night. That is not an oversell, so none of the contention tests would
catch it; `HoldingASeatForOnePerformance_LeavesTheSameSeatFreeForAnother` in
`RedisNoOversellTests` exists for exactly that.

## Owns

- **Data store:** PostgreSQL `inventory` DB (this service only) + Redis (hot path)
- **Public API:** REST `/v1/holds` (seats and/or general-admission quantities,
  Stage B; the request body carries `eventSessionId`),
  `/v1/sessions/{eventSessionId}/inventory`,
  `/v1/sessions/{eventSessionId}/inventory/seats` (`.AllowAnonymous()` — every
  seat's status), `.../inventory/general-admission`, `.../inventory/block`,
  `.../inventory/unblock`; internal (saga-only, not gateway-routed)
  `POST /v1/holds/{holdId}/cancel` — releases a converted hold's sold
  seats/quantities back to available, called by Ordering's cancellation saga;
  `POST /v1/holds/{holdId}/extend` — extends a hold's expiry for payment
  authentication, called by Ordering's checkout saga (ADR-0028)
- **Events published:** `SeatHeld`, `SeatReleased`, `SeatSold`, `SeatBlocked`,
  `SeatUnblocked` (via outbox) — seat-only today; general-admission holds don't
  yet appear on these events (no external consumer needs them this pass)
- **Events consumed:** `EventSessionPublished` (Catalog, one per performance) →
  provision seat inventory and general-admission allocations, and record the
  performance's `BookingEndsAt` plus the event's `OnSaleAt`,
  `MaxTicketsPerBuyer` and `RequiresQueue`; `EventSalesPaused`/
  `EventSalesResumed` (Catalog, both now carrying an `EventSessionId`) → update
  that performance's `SessionInventorySettings.SalesPaused` flag (see ADR-0027)

## General admission and the enforced booking cutoff

- **`GeneralAdmissionAllocation`** is the counter-based analogue of
  `InventoryItem` for a Venue admission area — a block with no individually
  addressable seats
  — `TotalCapacity`/`HeldCount`/`SoldCount` plus an optimistic-concurrency
  `Version`, same authority order as seats: Redis is the fast gate (a
  remaining-capacity counter per allocation, atomic Lua decrement), Postgres is
  the final authority. A hold can cover reserved seats and general-admission
  quantities together (`Hold.Items` + `Hold.GeneralAdmissionItems`).
  `GET /v1/sessions/{eventSessionId}/inventory/general-admission`'s response carries
  `HeldCount`/`SoldCount` alongside `Remaining`/`TotalCapacity` — the admin
  seat panel (`SeatBlockPanel`) uses these plus Ticketing's per-event ticket
  list to show sold/held/checked-in counts per GA section, the same
  visibility Reserved seats already had via per-seat status/color.
- **Fail-closed Redis default for GA capacity** (the deliberate opposite of the
  sparse seat model's fail-open default): a capacity key that was never
  initialized, or was lost to a flush, reads as zero remaining rather than
  available. Safe either way because Postgres stays authoritative; GA capacity
  must be explicitly initialized at provisioning time, unlike seats.
- **`SessionInventorySettings`** holds the performance's enforced
  `BookingEndsAt` and the event's `OnSaleAt`, both learned from
  `EventSessionPublished`. `HoldService.PlaceHoldAsync`
  rejects new holds once `DateTimeOffset.UtcNow` passes `BookingEndsAt`
  (`PlaceHoldOutcome.BookingWindowClosed`), and rejects them while
  `DateTimeOffset.UtcNow` is still before `OnSaleAt`
  (`PlaceHoldOutcome.OnSaleNotStarted`) — both checked before touching
  Redis/Postgres. Changing either bound after a performance is published is out
  of scope for this pass (Catalog's `UpdateSellingRules` and its session editing
  are both Draft-only).
  **`SessionInventorySettings.TenantId` is also the source of the tenant
  stamped on a placed `Hold`** — `PlaceHoldAsync` no longer takes a
  caller-supplied tenant; a buyer's own token may not carry one at all
  (ADR-0022).
- **`SessionInventorySettings.SalesPaused`** (an organizer's manual on/off toggle
  for an already-published event, independent of `OnSaleAt`/`BookingEndsAt` —
  ADR-0027) is checked first, before the on-sale/booking-cutoff checks —
  the most direct override an organizer can apply. Learned from
  `EventSalesPaused`/`EventSalesResumed` (not `EventSessionPublished` — sales
  are never paused at provisioning time), applied via
  `SessionSalesToggleService`,
  which updates the row directly rather than going through
  `InventoryProvisioningService.ProvisionAsync`'s one-time-only guard. Catalog
  fans an event-wide pause out to one message per performance, so pulling one
  night and pulling the run are the same mechanism. A performance paused this
  way returns `PlaceHoldOutcome.SalesPaused` (409) for new holds;
  already-held/converted seats and issued tickets are untouched.
- **`SessionInventorySettings.MaxTicketsPerBuyer`** (per-buyer ticket limit, if
  set — ADR-0021) is checked right after the on-sale/booking-cutoff checks.
  `IInventoryRepository.GetBuyerCommittedQuantityAsync` sums the buyer's
  existing seat/GA commitment across their `Active` and `Converted` holds for
  the whole **event** — every performance of the run, which is the point (an
  explicit `HoldItem`/`HoldGeneralAdmissionItem` join to `Hold`,
  same style as `GetReconciliationStateAsync` — not navigated via
  `Hold.Items`/`Hold.GeneralAdmissionItems`); if the new request would push
  the total past the limit, `PlaceHoldAsync` returns
  `PlaceHoldOutcome.BuyerLimitExceeded` (409) before touching Redis/Postgres.
- **`SessionInventorySettings.RequiresQueue`** (from Catalog's
  `Event.RequiresQueue`, the single on/off source of truth — ADR-0026), if
  set, requires `PlaceHoldAsync`'s caller to present a valid Queue-service
  admission token — checked right after the buyer-limit check, before
  touching Redis/Postgres. `IQueueAdmissionTokenValidator`
  (`HmacQueueAdmissionTokenValidator`) verifies the token **locally** via a
  shared HMAC secret (`QueueAdmission:HmacKey`, identical in both services'
  config) — no call to the Queue service itself, the same zero-hot-path-call
  philosophy ADR-0025 established for Ticketing's scan. An invalid/missing/
  expired token returns `PlaceHoldOutcome.QueueAdmissionRequired` (409).
- **`HoldService.CancelSoldAsync`** (a buyer-initiated cancellation/refund, the
  reverse of `ConvertToSoldAsync`) releases a converted hold's sold
  seats/quantities back to available (`InventoryItem.ReleaseSold()`/
  `GeneralAdmissionAllocation.ReleaseSold(quantity)`, plus the matching Redis
  release scripts). Idempotent via a genuine new `HoldStatus.Cancelled` state
  as a single gate — `ConvertToSoldAsync`'s per-item `Sold`-status filtering
  isn't safe to reuse here because GA capacity is a shared pool counter, not
  individually addressable like a seat. Called by Ordering's cancellation
  saga (an activity, so retried/replayed on crash-recovery) via
  `POST /v1/holds/{holdId}/cancel`.
- **Hold extension for async payment (ADR-0028):** `HoldOptions.PaymentExtensionTtl`
  (default 15 minutes, longer than the base 2-minute `Ttl`) governs how long a
  hold is extended once checkout submits and payment authentication begins (a
  3-D Secure challenge, a UPI app-switch) — losing seats after the buyer has
  committed to paying is worse than a strict pre-payment countdown.
  `HoldService.ExtendHoldAsync` (called by Ordering's checkout saga via the
  internal `POST /v1/holds/{id}/extend`, saga-only, not gateway-routed) uses a
  plain `SaveChangesAsync` — `Hold` carries no optimistic-concurrency token,
  unlike `InventoryItem`/`GeneralAdmissionAllocation` — then extends Redis's
  bookkeeping-key TTLs (`IHoldStore.ExtendAsync`; the per-seat/per-allocation
  markers themselves carry no TTL and need no change). `Hold.Extend` only ever
  moves `ExpiresAt` forward, so a replayed/retried call is a safe no-op.
  **`ExpiredHoldReaper`/`HoldService.ReapHoldAsync` re-checks `hold.ExpiresAt`**
  (not just `Status`) before reaping — without this, a hold extended in the
  narrow race window between the reaper's batch query and its per-hold reap
  call would be incorrectly reaped anyway, defeating the extension.
- **Not extended for GA in this pass:** `InventoryReconciler` only rebuilds the
  seat fast gate from Postgres after a flush. A GA capacity counter lost to a
  flush degrades fast-path availability (Redis under-reports remaining
  capacity as zero) until the next successful hold/release touches it, but
  never causes oversell — Postgres's `GeneralAdmissionAllocation.Hold(quantity)`
  is unconditional regardless of what Redis says.

## Design notes (ADR-0009)

- **Lean on the hot path:** Minimal API, hand-tuned data access, **no MediatR**.
- **No-oversell:** Redis Lua atomic check-and-set is the fast gate; Postgres
  optimistic concurrency (`InventoryItem.Version`) is the final authority; a hold
  TTL + reaper reclaims abandoned holds. See [LLD §4–5](../../docs/design/lld-phase1-seated.md).
- **Idempotent provisioning:** re-delivery of `EventSessionPublished` is a no-op
  once a performance has a settings row. The guard is the settings row, not a
  seat count — a performance sold entirely as general admission provisions zero
  seats, so counting them would re-provision it forever.
- **Provisioning joins two services by code.** Venue's seat map says which seats
  exist and which block each is in; Catalog's `SessionAllocation` list says
  which ticket type each block sells as and at what price. Neither knows both,
  and the block **code** is the only thing they agree on — which is why Venue
  keeps it stable across renames. A seat in a block with no allocation is
  skipped, never priced by guess: Catalog refuses to publish a performance with
  an unallocated block, so reaching that case means the two services disagree,
  and inventing a price would turn a disagreement into a wrong sale.
- **Drift reconciliation:** Redis holds only a sparse cache; a restart/flush loses
  it. `InventoryReconciler` (a background service) detects this via a sentinel key
  and rebuilds the fast gate from Postgres — writing back held (with remaining TTL),
  sold, and blocked seats. **Safety invariant:** the rebuild only adds restrictions,
  never frees a seat, so it can never cause oversell even if it races a live hold.
- **Organizer seat blocking:** `SeatBlockingService` moves a seat `Available` ↔
  `Blocked` (`InventoryItem.Block()`/`Unblock()`), all-or-nothing across the
  requested seats. Follows the same authority order as release/convert — Postgres
  commits first (optimistic concurrency), then the Redis marker (`B`) follows —
  because blocking is an admin action, not the flash-sale hot path, so there's no
  need for Redis-first speed. A seat already held by a buyer can't be blocked out
  from under them (only `Available` seats are eligible). No RBAC yet (tracked
  separately) — any authenticated caller in the tenant can block/unblock, same as
  other organizer-facing endpoints today.

## Structure

Layers directly under this folder (no `src/`): `Inventory.Api` (host + endpoints +
Dapr subscription), `Inventory.Application` (ports + provisioning + blocking +
reconciliation), `Inventory.Domain` (`InventoryItem`, `Hold`, `LedgerEntry` +
invariants), `Inventory.Infrastructure` (EF Core + Postgres, Redis hold store, the
**Venue** seat-map client, the expiry reaper, the drift reconciler, outbox).
`tests/Inventory.Tests` covers the general-admission counter invariants, the
`PlaceHoldAsync` gate sequence (each gate rejecting with its own outcome, and
none of them reaching the Redis fast gate), layering, and — against a real
Redis via Testcontainers — no-oversell under contention: many buyers racing
for one seat, more buyers than seats, all-or-nothing multi-seat holds, and the
same for general-admission capacity, and that the same seat on two nights is two
separate pieces of inventory. **If you change a Lua script in `RedisHoldStore`,
those are the tests that decide whether you got it right.**

## Local run

```bash
dapr run --app-id inventory \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/inventory/Inventory.Api
```

Run Catalog **and Venue** too (same Dapr setup): `EventSessionPublished` flows
from Catalog through pub/sub, and provisioning then calls Venue by app-id for
the pinned seat-map version.

## Dead letters

Every subscription here goes through `.SubscribesTo(topic, DeadLetterTopic)`, which adopts the
message's correlation chain and names `deadletter-inventory` for anything this service cannot handle
(ADR-0040). Dapr retries five times first — a resiliency policy caps it, without which a poison
message would be redelivered forever and never reach the dead letter at all.

`OnDeadLetterAsync` drains that topic into the `dead_letters` table and logs at Error. There is no
read API for it yet: it is an operator's view of message payloads and this platform has no operator
role.

## Do not

- Read another service's database (pull the seat map from **Venue** via Dapr,
  and take prices from the allocation list Catalog puts on the event).
- Key anything — a row, a Redis key, a route — on the event when it means the
  performance. The three event-scoped exceptions are named above; there are no
  others.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Put MediatR on the hold path.
