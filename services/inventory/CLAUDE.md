# CLAUDE.md — Inventory service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

The no-oversell system of record. Owns seat availability, general-admission
capacity pools, holds (with TTL, covering either or both in one hold), the
convert-to-sold path, and the immutable inventory ledger. Bounded context:
**Inventory & Hold** (ADR-0008). Generates inventory when Catalog publishes an
event, and enforces that event's booking cutoff.

## Owns

- **Data store:** PostgreSQL `inventory` DB (this service only) + Redis (hot path)
- **Public API:** REST `/v1/holds` (seats and/or general-admission quantities,
  Stage B), `/v1/events/{id}/inventory`, `/v1/events/{id}/inventory/seats`
  (`.AllowAnonymous()` — every seat's status), `/v1/events/{id}/inventory/block`,
  `/v1/events/{id}/inventory/unblock`; internal (saga-only, not gateway-routed)
  `POST /v1/holds/{holdId}/cancel` — releases a converted hold's sold
  seats/quantities back to available, called by Ordering's cancellation saga
- **Events published:** `SeatHeld`, `SeatReleased`, `SeatSold`, `SeatBlocked`,
  `SeatUnblocked` (via outbox) — seat-only today; general-admission holds don't
  yet appear on these events (no external consumer needs them this pass)
- **Events consumed:** `EventPublished` (Catalog) → provision seat inventory and
  general-admission allocations, and record the event's `BookingEndsAt`,
  `OnSaleAt`, `MaxTicketsPerBuyer`, and `RequiresQueue`

## General admission and the enforced booking cutoff

- **`GeneralAdmissionAllocation`** is the counter-based analogue of
  `InventoryItem` for a Catalog section with no individually addressable seats
  — `TotalCapacity`/`HeldCount`/`SoldCount` plus an optimistic-concurrency
  `Version`, same authority order as seats: Redis is the fast gate (a
  remaining-capacity counter per allocation, atomic Lua decrement), Postgres is
  the final authority. A hold can cover reserved seats and general-admission
  quantities together (`Hold.Items` + `Hold.GeneralAdmissionItems`).
  `GET /v1/events/{id}/inventory/general-admission`'s response carries
  `HeldCount`/`SoldCount` alongside `Remaining`/`TotalCapacity` — the admin
  seat panel (`SeatBlockPanel`) uses these plus Ticketing's per-event ticket
  list to show sold/held/checked-in counts per GA section, the same
  visibility Reserved seats already had via per-seat status/color.
- **Fail-closed Redis default for GA capacity** (the deliberate opposite of the
  sparse seat model's fail-open default): a capacity key that was never
  initialized, or was lost to a flush, reads as zero remaining rather than
  available. Safe either way because Postgres stays authoritative; GA capacity
  must be explicitly initialized at provisioning time, unlike seats.
- **`EventInventorySettings`** holds the event's enforced `BookingEndsAt` and
  `OnSaleAt`, both learned from `EventPublished`. `HoldService.PlaceHoldAsync`
  rejects new holds once `DateTimeOffset.UtcNow` passes `BookingEndsAt`
  (`PlaceHoldOutcome.BookingWindowClosed`), and rejects them while
  `DateTimeOffset.UtcNow` is still before `OnSaleAt`
  (`PlaceHoldOutcome.OnSaleNotStarted`) — both checked before touching
  Redis/Postgres. Changing either bound after an event is published is out of
  scope for this pass (`UpdateEventDetails` on Catalog stays Draft-only).
  **`EventInventorySettings.TenantId` is also the source of the tenant
  stamped on a placed `Hold`** — `PlaceHoldAsync` no longer takes a
  caller-supplied tenant; a buyer's own token may not carry one at all
  (ADR-0022).
- **`EventInventorySettings.MaxTicketsPerBuyer`** (per-buyer ticket limit, if
  set — ADR-0021) is checked right after the on-sale/booking-cutoff checks.
  `IInventoryRepository.GetBuyerCommittedQuantityAsync` sums the buyer's
  existing seat/GA commitment across their `Active` and `Converted` holds for
  the event (an explicit `HoldItem`/`HoldGeneralAdmissionItem` join to `Hold`,
  same style as `GetReconciliationStateAsync` — not navigated via
  `Hold.Items`/`Hold.GeneralAdmissionItems`); if the new request would push
  the total past the limit, `PlaceHoldAsync` returns
  `PlaceHoldOutcome.BuyerLimitExceeded` (409) before touching Redis/Postgres.
- **`EventInventorySettings.RequiresQueue`** (from Catalog's
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
- **Idempotent provisioning:** re-delivery of `EventPublished` is a no-op once an
  event has inventory.
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
Catalog seat-map client, the expiry reaper, the drift reconciler, outbox). `tests/` to follow.

## Local run

```bash
dapr run --app-id inventory \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/inventory/Inventory.Api
```

Run Catalog too (same Dapr setup) so `EventPublished` flows through pub/sub and
the seat-map client can reach Catalog by app-id.

## Do not

- Read another service's database (pull the seat map from Catalog via Dapr).
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Put MediatR on the hold path.
