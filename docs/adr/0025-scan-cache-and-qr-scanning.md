# ADR-0025 — Warm-once local scan cache, real QR codes, and camera scanning

- **Status:** Accepted
- **Date:** 2026-08-05

## Context

Two follow-on requirements surfaced right after ADR-0024 shipped:

1. **Real QR codes.** `Ticket.Token` (a 128-bit CSPRNG string) was never
   actually rendered as a QR image anywhere — the buyer's order/ticket view
   showed the raw token as text, and `ScanTicketPage.tsx` only accepted
   typed/pasted/hardware-wedge input, no camera.
2. **Check-in must not be a bottleneck at extreme concurrency.** The
   concrete framing: "millions of users waiting in line... we can't keep
   them wait in queue." This directly invalidates the latency assumption
   ADR-0024 made explicitly — that scanning is a "low-frequency,
   latency-tolerant admin action" for which "a synchronous cross-service
   read... is the right trade-off." That was true for an occasional
   organizer check, not a mega-event's turnstiles under sustained load.
   `ScanTicketAsync` as shipped in ADR-0024 made **two live Dapr
   service-invocation calls per scan** (one to Catalog, one to Inventory for
   a general-admission ticket) — a real network hop, on the hot path, for
   every person walking through a gate.

Re-examining the data those calls fetched: the seat map (and its per-section
gate assignment) is immutable once `DefineSeatMap` runs (one-time,
Draft-only), and the event's check-in window is fixed once the event
publishes (`UpdateEventDetails` is Draft-only). Nothing a scan checks ever
changes after publish — the "live read" design wasn't just slow, it was
solving a problem that doesn't exist. The data is safe to resolve **once
per event, ahead of time**, and served forever after from Ticketing's own
database — the same fast-gate-over-durable-truth idea already proven in
this codebase (Inventory's `EventInventorySettings`, learned once from
`EventPublished` instead of queried live), applied to Ticketing for the
first time.

## Decision

### Supersedes ADR-0024's "live Dapr call at scan time" decision

Only that one decision is reversed — the event-scoping/window/gate
*checks* themselves are unchanged, only *where the data comes from* does.

- **`EventPublished`** (`building-blocks/EventPlatform.Contracts/Catalog/`)
  gains `StartsAt`/`EndsAt` (required) and `DoorsOpenAt` (optional) —
  `PublishEventHandler` passes them straight from the event. This removes
  any need to call Catalog for the window check at all.
- **Ticketing subscribes to `EventPublished`** (its second subscription,
  after `OrderConfirmed`). On receipt, `EventScanContextProvisioningService`
  — idempotent on redelivery, mirroring `InventoryProvisioningService`'s
  shape — does, **once per event, never once per scan**:
  1. Persists `EventScanContext` (window bounds) straight from the payload
     — zero cross-service calls for this part.
  2. Calls `ICatalogEventClient` (unchanged from ADR-0024, just invoked
     from here instead of from the scan endpoint) once, for the seat map's
     gate assignments, persisted as `SeatEntryGate` rows.
  3. Calls `IInventoryGaClient` (widened from a single-allocation lookup to
     a bulk per-event read) once, resolving every general-admission
     allocation's section to its gate in one pass, persisted as
     `GaAllocationGate` rows.
  - Inventory provisions GA allocations from the *same* `EventPublished`
    message, asynchronously — step 3 can race ahead of it. A bounded retry
    with a short delay covers the normal case; if allocations are still
    empty after retrying, the event is treated as having no GA gate
    restrictions rather than blocking — the same safe-degrade posture as an
    unmatched seat/section lookup already has.
- **`ScanTicketAsync` reads only from its own database now** — `EventScanContext`
  by event id (window check), then `SeatEntryGate`/`GaAllocationGate` by
  seat/allocation id (gate check). Same outcome/status-code mapping as
  before; only the data source changed, so this is behavior-preserving from
  the caller's point of view. Zero network hops, zero dependency on
  Catalog/Inventory being reachable or fast at the moment of the scan.
- **Deliberately not Redis-backed.** Unlike Inventory's hold gate (mutable,
  contended, needs atomic check-and-set), this data is read-only and
  immutable once warmed — a plain indexed Postgres table already serves
  extreme read QPS reliably with connection pooling, and durability across
  pod restarts/scale-out (a fresh pod needs no pub/sub "catch-up") matters
  more here than shaving microseconds off an already-fast indexed lookup.
- **Ops: Ticketing needs to scale out for a mega-event.**
  `deploy/base/ticketing/hpa.yaml` — a `HorizontalPodAutoscaler` on CPU
  (`minReplicas: 1`, `maxReplicas: 10` as starting defaults; the real
  ceiling is venue-dependent, a tunable not an architectural constant).
  Because the scan cache removes all cross-service calls, throughput now
  scales linearly with replica count — the documented recommendation
  (`services/ticketing/README.md`) is to pre-scale ahead of a known
  gate-open time rather than rely solely on reactive autoscaling.

### Real QR codes

New `GET /v1/tickets/{id}/qrcode` (auth: the ticket's own buyer via the
`sub` claim, or the owning tenant — opaque 404 on a mismatch, matching the
"never reveal existence" pattern used elsewhere) generates a PNG on demand
via `QRCoder` (MIT, pure-managed, no native dependency), encoding the
ticket's existing `Token` string verbatim. The buyer's order page
(`OrderPage.tsx`) fetches it as an authenticated blob (a plain `<img src>`
can't carry the bearer token the endpoint requires) and renders it inline,
alongside the existing raw-token text (kept for manual/hardware-wedge
entry and as a fallback). The QR payload is the raw opaque token, not a
signed/expiring payload — matches the pre-existing deferred tracker note
in `services/ticketing/CLAUDE.md`, not a new gap introduced here.

### Camera-based scanning

`ScanTicketPage.tsx` gains a "Scan with camera" toggle. Decoding prefers
the native Barcode Detection API (`BarcodeDetector`, Chrome/Edge/Android —
hardware-accelerated, zero extra JS) where available, falling back to
`jsQR` (pure JS, MIT, no WASM, decodes from raw `ImageData`) elsewhere
(Safari/Firefox) via runtime feature detection. `BarcodeDetector` isn't yet
part of TypeScript's bundled DOM lib, so a small ambient declaration
(`src/types/barcode-detector.d.ts`) types it. On a successful decode the
camera stops and the same `handleScan` path the manual/hardware-wedge flow
already used fires automatically — no duplicated submit logic.

**The manual/hardware-wedge token input is kept as a first-class option,
not hidden behind the camera toggle.** For "millions of people in line,"
dedicated hardware laser/2D-imager turnstile scanners — wired in as
keyboard-wedge devices, which the existing manual-input path already
supports with zero further work — are the realistic mechanism for
sustained mass throughput: faster decode, hands-free mounting, no
phone-holding/battery/glare problems. The camera path is the right fit for
staff walking the line with a phone/tablet, or a lower-volume gate, not the
primary answer to extreme concurrent check-in volume by itself — Part A
(the cache) is what actually answers that requirement.

## Consequences

- `ScanTicketAsync` makes zero calls to Catalog or Inventory; throughput is
  bounded by Ticketing's own Postgres connection pool and horizontal
  replica count, not by another service's availability or latency.
- `ICatalogEventClient`/`IInventoryGaClient` are called exactly once per
  event (from `EventScanContextProvisioningService`), never from the scan
  endpoint — their shapes changed to bulk/gate-map-oriented reads
  accordingly (`GetScanContextAsync` → `GetGateMapAsync`;
  `GetCatalogSectionIdAsync(eventId, allocationId)` →
  `GetAllocationCatalogSectionsAsync(eventId)`).
- Three new small Ticketing-owned tables (`event_scan_context`,
  `seat_entry_gate`, `ga_allocation_gate`) — local dev DB rebuild required.
- Buyers get a real, scannable QR code on their ticket; organizers get a
  camera-scanning option alongside the existing token-entry/hardware-wedge
  flow.
- A `HorizontalPodAutoscaler` exists for Ticketing for the first time in
  this repo — no other service has one yet; revisit for the others if a
  similar throughput requirement surfaces.

## Alternatives considered

- **Keep the ADR-0024 live-call design, add Redis caching in front of it** —
  rejected: still a network hop (Ticketing → Redis) on every scan, and adds
  a new infra dependency Ticketing doesn't have today, for data that's
  cheaper to just persist once in the database it already has.
- **Denormalize the gate/window data directly onto `Ticket` at issuance**
  (propagated through Inventory's `HoldView`, Ordering's `OrderLine`/
  `OrderConfirmed`) — rejected: a materially larger, riskier change
  touching three services' schemas and contracts for data that's already
  reachable per-event from Ticketing's own new tables; the per-event cache
  achieves the identical "zero cross-service calls at scan time" outcome
  with a much smaller blast radius.
- **In-memory-only cache instead of Postgres tables** — rejected: a newly
  started/scaled-out pod has no way to "catch up" on a past `EventPublished`
  delivery (Dapr pub/sub doesn't guarantee redelivery to every replica in a
  consumer group), so an in-memory-only cache would be empty exactly when a
  mega-event's autoscaling adds new pods — the opposite of what's needed.

## References

- `services/ticketing/CLAUDE.md`, `services/ticketing/README.md` — updated
  design notes and the mega-event scaling runbook.
- ADR-0024 — the decision this ADR partially supersedes (scan-time data
  source only; the checks themselves are unchanged).
