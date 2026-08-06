# ADR-0027 — Manual sales pause/resume for a published event

- **Status:** Accepted
- **Date:** 2026-08-06

## Context

An organizer needs to stop new sales for a live, already-`Published` event —
e.g. during a technical issue or a pricing correction — without cancelling
the event, touching already-placed holds/tickets, or waiting for the
enforced `OnSaleAt`/`BookingEndsAt` time window to do it for them. This is
the first of several requested post-publish edits (the others — changing
`BookingEndsAt`/`DoorsOpenAt`/price after publish — remain deliberately out
of scope; see Alternatives).

## Decision

### `Event.SalesPaused` — a boolean toggle, not a new `EventStatus`

`EventStatus` already has an unused `OnSale` value, but pausing isn't a new
lifecycle stage the way Draft→Published→SoldOut/Cancelled/Completed are —
it's a reversible toggle layered on top of `Published`, orthogonal to the
`OnSaleAt`/`BookingEndsAt` enforced time window (both can be true or false
independently). `Event.PauseSales()`/`ResumeSales()` guard on
`Status == Published` and on the flag's current value (409 `AlreadyPaused`/
`NotPaused` otherwise) — the same opaque-conflict shape `PublishEventHandler`
already uses for `NotDraft`.

### `PauseSales`/`ResumeSales` — new vertical slices, same shape as `PublishEvent`

`Features/PauseSales/`, `Features/ResumeSales/` (Command/Handler/Outcome),
`POST /v1/events/{id}/pause-sales`/`resume-sales`, tenant-ownership-checked
the same way `PublishEvent`/`UpdateEventDetails` are (404 on a mismatch).

### New integration events, not `EventPublished` redelivery or `EventUpdated`

Inventory's `InventoryProvisioningService.ProvisionAsync` is a one-time,
idempotency-gated operation (`ExistsForEventAsync` short-circuits before
touching `EventInventorySettings` again), so redelivering `EventPublished`
can never propagate a post-publish change — and `EventUpdated` has no
consumer and carries no payload beyond the event id. `EventSalesPaused`/
`EventSalesResumed` are new, minimal integration events (just the ids)
consumed by a new pair of Inventory Dapr subscriptions
(`OnEventSalesPausedAsync`/`OnEventSalesResumedAsync`) that call a new
`EventSalesToggleService`, updating the cached
`EventInventorySettings.SalesPaused` flag directly — bypassing
`ProvisionAsync`'s one-time guard entirely, since this is a genuine
post-provisioning update, not re-provisioning.

### `HoldService.PlaceHoldAsync` gains one more settings-derived check

Checked first, before `OnSaleAt`/`BookingEndsAt`/`MaxTicketsPerBuyer`/
`RequiresQueue` — a manual pause is the most direct override an organizer
can apply, so it short-circuits earliest. New `PlaceHoldOutcome.SalesPaused`
→ 409. Only new holds are rejected; already-held/converted seats and
already-issued tickets are untouched, matching seat blocking's precedent of
never disturbing existing state.

### Frontend: a toggle button on the admin event page, gated on `Published`

`AdminEventDetailPage`'s header `extra` gains a "Pause sales"/"Resume sales"
button (only rendered for `Published` events, mirroring the existing
Draft-only `Publish` button's conditional). Buyer-facing pages
(`EventDetailPage`, `SeatSelectionPage`) gate on `event.salesPaused` the same
way they already gate on `onSaleAt`, including the direct-URL-hit defense in
`SeatSelectionPage` (the server enforces it either way).

## Consequences

- `EventInventorySettings` gains a `SalesPaused` bool and a `SetSalesPaused`
  mutator — the first genuinely reachable post-provisioning update to that
  row (the existing `Update` method stays dead code, unrelated to this
  change).
- Two new integration events, two new Dapr subscriptions in Inventory — a
  small but real addition to the pub/sub surface area for what is, on the
  Catalog side, a two-field toggle.
- Pausing/resuming is entirely independent of `OnSaleAt`/`BookingEndsAt`:
  an organizer can pause a currently-open sales window, or resume one that
  would otherwise still be blocked by the time window — the checks stack,
  they don't replace each other.

## Alternatives considered

- **Reusing `EventStatus.OnSale`** as a "paused" signal — rejected; it's
  already unused/undefined behavior in this codebase, and conflating a
  manual toggle with the lifecycle enum would make `Status` mean two
  different things depending on context.
- **Riding `EventPublished` redelivery** — rejected; `ProvisionAsync`'s
  idempotency guard makes this a dead path today, and repurposing it would
  require weakening a guard that exists specifically to keep provisioning
  a one-time operation.
- **A live Dapr call from Inventory to Catalog at hold time** instead of a
  propagated+cached flag — rejected for the same hot-path reasons ADR-0025/
  ADR-0026 already rejected it: a network hop on `PlaceHoldAsync` is exactly
  what this codebase's "propagate once, verify/read locally" convention
  exists to avoid.
- **Also shipping gate/doors-open-time and price changes post-publish in
  this pass** — deferred; both need real propagation redesign (Ticketing's
  scan-window cache and Inventory's baked-in `InventoryItem`/
  `GeneralAdmissionAllocation` price are both populated once at publish and
  never re-read), a larger change than this ADR's scope.

## References

- ADR-0021 — the `MaxTicketsPerBuyer` settings-derived-check-in-`HoldService`
  precedent this slots into.
- ADR-0025/ADR-0026 — the "propagate once, verify/read locally, zero
  cross-service calls on the hot path" convention this follows.
- `services/inventory/CLAUDE.md`, `services/catalog/CLAUDE.md` — updated
  service docs.
