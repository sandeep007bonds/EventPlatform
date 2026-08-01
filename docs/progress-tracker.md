# Progress & Tech-Debt Tracker

Single place to track what's done, what's in progress, and — importantly — the
**deferred cloud tasks and tech debt**, so nothing is lost while we focus on
local development. Update this with each meaningful change.

**Legend:** ✅ done · 🚧 in progress · ⏸️ deferred (on hold) · ⬜ not started

---

## Progress

### Foundations & documentation
- ✅ Architecture, requirements, feature flows (6)
- ✅ ADRs 0001–0014
- ✅ HLD, DFD, LLD (Phase 1 seated)
- ✅ Engineering guidelines + golden rules
- ✅ Build config: `.editorconfig`, `Directory.Build.props`, CPM, PR template
- ✅ Root + per-service `CLAUDE.md`
- ✅ Roadmap issues (#1–#11) on GitHub
- ✅ **Data flow & service boundaries reference** (`docs/data-flow-and-service-boundaries.md`): as-built (not pre-implementation) reference — the boundary/ownership table for all five services, the communication matrix (sync/Dapr service invocation, async/Dapr pub/sub, direct-Redis, external/Stripe, gRPC internals), the full purchase-flow sequence with a mermaid diagram, and the background processes

### Code scaffold
- ✅ Solution (`.slnx`)
- ✅ `EventPlatform.Hosting` — shared service defaults (auth, OpenAPI, JSON, OTel, health)
- ✅ `EventPlatform.Contracts` — base integration event + sample
- ✅ Local Docker dev stack (compose + Dapr components + guide)
- ✅ **One-click local dev:** `./scripts/dev-up.sh` starts Postgres/Redis/Jaeger, waits for health, then starts all five services + Dapr sidecars via a Dapr multi-app run template (`platform/dapr/dapr.yaml`) — replaces five manual `dapr run` terminals with one command (Ctrl+C stops everything); `scripts/dev-down.sh` tears down Docker; `scripts/dev-token.sh` mints the dev auth JWT. Docs (`local-development.md`, `local-e2e-walkthrough.md`) updated to lead with it
- ✅ **Build hardening — green under warnings-as-errors** (deps patched, .NET 10 pruning, global usings, analyzer config)
- ✅ **Service structure flattened** (no `src/`) — layers directly under `services/<name>/`; standard for all services
- 🚧 **Catalog service implementation (issue #6)** ← current focus
  - ✅ Domain (`Event` aggregate, `EventStatus`)
  - ✅ Application slices: `CreateEvent`, `GetEvent` (+ FluentValidation pipeline)
  - ✅ Infrastructure: EF Core + Postgres (`CatalogDbContext`, repository)
  - ✅ API wired: `POST /v1/events`, `GET /v1/events/{id}` via MediatR
  - ✅ Local `dotnet build` green; runs against local Postgres (`EnsureCreated`)
  - ✅ `PublishEvent` slice (draft → published)
  - ✅ **Transactional outbox + Dapr (first use):** `PublishEvent` enqueues `EventPublished` into the `outbox` table in the same transaction; shared `EventPlatform.Messaging` relay publishes it to Dapr pub/sub (at-least-once, CloudEvent id = outbox id)
  - ✅ **Seat map:** `SeatMap`/`Seat` domain, `DefineSeatMap` + `GetSeatMap` slices, `POST`/`GET /v1/events/{id}/seatmap`. Publish now requires a seat map and stamps `SeatCount` on `EventPublished`; `GetSeatMap` is the hand-off Inventory reads ← **verify build locally**
  - ✅ Local dev schema: `Database.EnsureCreatedAsync()` on startup (Development only) creates the schema from the current model — no `dotnet ef` command needed for local dev
  - ⬜ Real EF Core migrations `InitialCreate` (Design ref + design-time factory already in place) — deferred to cloud-deployment work (T8); needed for staging/production, not local dev
- 🚧 **Inventory & Hold — no-oversell core (issue #7)**
  - ✅ **Stage A — provisioning:** service scaffold (Domain `InventoryItem`/`Hold`/`LedgerEntry`, lean Application, EF + Postgres, outbox, migration scaffolding); consumes `EventPublished` via Dapr pub/sub and generates inventory by pulling Catalog's seat map (Dapr service invocation); `GET /v1/events/{id}/inventory` ← **verify build locally**
  - ✅ **Stage B — hold hot path:** Redis Lua atomic check-and-set (`RedisHoldStore`) as the fast gate + Postgres optimistic concurrency (`Version`) as the final authority + outbox; `POST /v1/holds` and `DELETE /v1/holds/{id}` emit `SeatHeld`/`SeatReleased`. Sparse Redis model (missing key = available; no per-seat seeding) ← **verify build locally**
  - ✅ **Stage C — expiry reaper:** `ExpiredHoldReaper` background service reclaims holds past `expires_at` — returns seats to available in Postgres (authority), clears the Redis seat keys, emits `SeatReleased`. Each hold is its own unit of work ← **verify build locally**
  - ✅ **Saga hooks (for #8):** `GET /v1/holds/{id}` (validate: owner/expiry + priced lines), internal `POST /v1/holds/{id}/convert` (idempotent convert-to-sold: seats → `S`, emits `SeatSold`)
  - ✅ Redis↔Postgres drift reconciler: `InventoryReconciler` background service detects a flushed/restarted Redis via a sentinel key and rebuilds the fast gate from Postgres (held seats with remaining TTL + sold + blocked seats), joining held items back to their active hold for the TTL. Only re-applies restrictions, never frees a seat (cannot cause oversell); runs on startup + interval — verified by CI
  - ✅ **Organizer seat blocking:** `InventoryItem.Block()`/`Unblock()` (Available ↔ Blocked, the previously-unused `Blocked` status now wired up); `SeatBlockingService` (all-or-nothing across requested seats, Postgres-first then Redis, same authority order as release/convert); `POST /v1/events/{id}/inventory/block` and `/unblock`; emits `SeatBlocked`/`SeatUnblocked`; reconciler restores blocked seats on a Redis rebuild — verified by CI
  - ⬜ Real EF Core migrations `InitialCreate` (inventory_item, hold, hold_item, inventory_ledger, outbox) — deferred to cloud-deployment work; local dev uses `EnsureCreated` instead (see Catalog note above)
- 🚧 **Ordering — checkout saga (#8)**
  - ✅ Service scaffold: `Order`/`OrderLine` domain, `CheckoutService` saga (validate → create → charge → convert → confirm + compensation), EF + Postgres, outbox, migration scaffolding; `POST /v1/checkout` (Idempotency-Key), `GET /v1/orders/{id}` ← **verify build locally**
  - ✅ Inventory hooks reused: `IHoldClient` (Dapr) → GET hold / convert / release; payment **stubbed** (`StubPaymentClient`)
  - ✅ **Durability — Dapr Workflow:** `CheckoutWorkflow` (deterministic orchestrator) + 8 activities (fetch hold, create order, charge, convert, confirm + compensations release/refund/fail). `/v1/checkout` schedules the workflow and awaits completion; a crash mid-flight resumes where it left off (ADR-0010). Replaced the in-process `CheckoutService` — verified by CI
  - ⬜ Real EF Core migrations `InitialCreate` (orders, order_line, outbox) — deferred to cloud-deployment work; local dev uses `EnsureCreated` instead (see Catalog note above)
  - ✅ Concurrent-duplicate checkout: `CreateOrderActivity` re-checks + `IOrderRepository.TryAddAsync` swallows the `(tenant, idempotency_key)` unique-violation (Postgres 23505); the losing racer re-fetches the winner and the workflow short-circuits to `Duplicate` (409) — no 500, no double charge — verified by CI
- 🚧 **Payments (#9)**
  - ✅ Service scaffold: `Payment` domain, `PaymentService` (idempotent charge/refund on `(order, key)`), EF + Postgres, outbox, migration scaffolding; internal `POST /v1/payments/charge` + `/refund`; emits `PaymentCaptured`/`PaymentFailed`/`PaymentRefunded`
  - ✅ Gateway behind `IPaymentGateway`; dev `SimulatedPaymentGateway`. **Ordering calls Payments** (`DaprPaymentClient`).
  - ✅ Concurrent-duplicate charge: `IPaymentRepository.TrySaveChangesAsync` swallows the `(order, idempotency_key)` unique-violation (Postgres 23505); the losing racer re-fetches the winner (PSP dedupes the charge on the same key, loser's outbox events roll back) — no 500, no double charge — verified by CI
  - ✅ **Stripe gateway (Stripe.net):** `StripePaymentGateway` creates + confirms a PaymentIntent (Stripe idempotency key), refunds via the Refund API. Selected automatically when `Payments:Stripe:SecretKey` is configured (Key Vault / user-secrets / env) — **never committed**. PCI SAQ-A.
  - ✅ **Stripe webhook inbox (async capture / 3DS / refunds):** `POST /v1/payments/webhooks/stripe` verifies the `Stripe-Signature` against `Payments:Stripe:WebhookSecret` (`StripeWebhookGateway`), reconciles the `Payment` idempotently (`PaymentWebhookService`, new idempotent domain transitions), and emits the matching outbox event. At-least-once → exactly-once via a `processed_webhook_event` dedupe ledger committed in the same transaction; neutral `PaymentWebhookNotification` keeps the Stripe SDK out of Application/Domain — verified by CI
  - ⬜ **T-stripe (remaining):** client-side card collection + `payment_method` on the charge (replace `pm_card_visa`, enables real 3DS)
  - ⬜ Real EF Core migrations `InitialCreate` (payment, outbox, **processed_webhook_event**) — deferred to cloud-deployment work; local dev uses `EnsureCreated` instead (see Catalog note above)
- ✅ **Ticketing (#10):** `Ticket` domain, `TicketIssuingService` (idempotent, one ticket per sold seat, CSPRNG scan token), EF + Postgres, outbox, migration scaffolding; consumes `OrderConfirmed` via Dapr pub/sub, emits `TicketIssued`; `GET /v1/orders/{id}/tickets`, `GET /v1/tickets/{id}` — verified by CI
  - ⬜ Real EF Core migrations `InitialCreate` (ticket, outbox) — deferred to cloud-deployment work; local dev uses `EnsureCreated` instead (see Catalog note above)
  - ⬜ T-ticket-token: sign/rotate the scan token before production
- ✅ **Communication (ADR-0016, notifications scope):** sixth service — `DeliveryLogEntry`/`NotificationTemplate` domain, dual-vendor (Azure Communication Services + Twilio) `IEmailSender`/`ISmsSender`/`IWhatsAppSender` ports, config-gated per channel with a dev/logging fallback (same pattern as Payments' Stripe gate); single `POST /v1/notifications/send` endpoint (internal, all three channels, email template-driven via embedded Scriban templates); subscribes to `OrderConfirmed`/`TicketIssued` for redelivery-safety (`ProcessedNotificationEvent` dedup ledger) but real delivery is deferred — no Identity/user-profile service exists yet to resolve a recipient from the bare `UserId` either event carries (`IRecipientResolver`/`UnavailableRecipientResolver`). No outbox (never publishes). First service in the repo with a `tests/` project; CI gained its first `dotnet test` step
  - ⬜ Real EF Core migrations `InitialCreate` (delivery_log, processed_notification_event) — deferred to cloud-deployment work; local dev uses `EnsureCreated` instead (see Catalog note above)
  - ⬜ Real `IRecipientResolver` implementation once Identity (or any user-profile source) exists — unblocks actual Email/SMS/WhatsApp delivery for `OrderConfirmed`/`TicketIssued`
- 🚧 **No-oversell load test (#11)**
  - ✅ k6 harness (`platform/loadtest/`): `no-oversell.js` (N users race for **one** seat; hard gate `holds_succeeded: count<2`), `throughput.js` (sustained load over a big seat map; p95<250ms / p99<500ms / err<1%), `lib/jwt.js` (HS256 dev-token minting), README
  - ✅ Dev-auth path in `EventPlatform.Hosting/AuthenticationExtensions.cs`: symmetric-key JWT validation gated on `Jwt:DevSigningKey` (Development-only; prod stays OIDC) — verified by CI
  - ⬜ Run against the live stack (needs Catalog + Inventory + Dapr + Postgres + Redis up locally) and record the numbers
- ✅ **Event tours, contact/social, enforced booking cutoff, and Reserved-vs-General-Admission tickets (ADR-0019, ADR-0020):** `Venue` removed; `EventGroup` (tour) added with its own `StartsAt`/`EndsAt` and contact/social defaults (per-leg override); `Event.EndsAt` required at creation; `OffSaleAt` renamed to the now-enforced `BookingEndsAt`. `SeatMap` sections can be `Reserved` or `GeneralAdmission` (mixed in one map); Inventory gained a counter-based `GeneralAdmissionAllocation` (fail-closed Redis capacity gate, mirroring but distinct from the fail-open sparse seat model) and `EventInventorySettings` (enforced cutoff, checked first in `PlaceHoldAsync`). `OrderLine`/`Ticket`/`LedgerEntry`/`OrderConfirmed`/`TicketIssued` all widened with nullable seat-vs-GA fields rather than split into separate types. Frontend: tour/event forms, seat-map allocation-type toggle, GA quantity steppers alongside the reserved-seat grid, contact/social display and editing (shared `SocialLinksEditor`) — verified via `tsc -b`/`eslint`/`prettier`/`vite build`, all clean.
  - ⬜ No Inventory endpoint yet exposes a GA allocation's live remaining capacity to the buyer UI (the quantity stepper is capped by total section capacity; the server is still the real enforcer at hold time)
  - ⬜ No UI yet to edit an already-created tour's dates/contact/social after initial creation (the backend command/endpoint exist and are reachable via API)
  - ⬜ `InventoryReconciler` not extended to rebuild GA capacity counters after a Redis flush (fast-path availability only — Postgres remains authoritative, see ADR-0020)
  - ⬜ `BookingEndsAt` not editable post-publish (would need a new command + wiring the zero-consumer `EventUpdated` event into Inventory)
- ✅ **Ticket delivery email, check-in/scan, and per-buyer ticket limits (ADR-0021):** `CheckoutRequest.BuyerEmail` (required) threaded through `Order`/`OrderConfirmed`; new `OrderTicketsIssued`/`IssuedTicketSummary` contract published once per order after all its tickets are minted; Communication's `IntegrationEventNotificationHandler` gains a direct-send handler path (bypasses `NotificationSendService`/`IRecipientResolver`, one `SaveChangesAsync` for the delivery log + processed marker) rendering a new `order-tickets` Scriban template and sending via the dev/logging `IEmailSender`. `Ticket.CheckedInAt` + `ITicketRepository.GetByTokenAsync` + `POST /v1/tickets/scan` (new admin `ScanTicketPage` at `/admin/scan`). `Event.MaxTicketsPerBuyer` propagated to Inventory via `EventPublished`/`EventInventorySettings`; new `IInventoryRepository.GetBuyerCommittedQuantityAsync` sums a buyer's Active+Converted holds; `PlaceHoldOutcome.BuyerLimitExceeded` (409) enforced in `HoldService.PlaceHoldAsync` — verified via `tsc -b`/`eslint`/`prettier`/`vite build`, all clean.
  - ⬜ Archive/purge for consumed tickets — design captured as P3 above, nothing built
  - ⬜ Ticketing still has no `tests/` project (pre-existing gap, matches Ordering/Catalog)
  - ⬜ No QR code image generation — the email and scan flow use the raw opaque token text only

---

## ⏸️ Deferred — CLOUD tasks (ON HOLD — do not lose)

Intentionally paused while we build locally. Revisit before first deploy.

| # | Task | Related |
|---|------|---------|
| C1 | GitHub Actions CI (`dotnet build`/`test`, analyzers, coverage) | ADR-0004 |
| C2 | Terraform: AKS cluster + node pools (system/general/hot-path/spot) | ADR-0002/0005 |
| C3 | Argo CD / GitOps + `deploy/` manifests | ADR-0004 |
| C4 | Dockerfiles + Helm charts per service | Phase 0 |
| C5 | Azure infra: PostgreSQL, Redis, Service Bus/Event Hubs, Key Vault | ADR-0001 |
| C6 | Entra (Azure AD B2C) identity — prod JWT authority | Security |
| C7 | Cloud Dapr components (Service Bus / Azure Cache / Key Vault) — mirror local | ADR-0006 |
| C8 | Azure Front Door + WAF + bot management (edge) | Phase 3 |
| C9 | Observability backend (Azure Monitor / managed Grafana) | Observability |
| C10 | Multi-AZ + DR runbook (RTO < 15m / RPO < 1m) | Phase 3 |

## ⏸️ Deferred — GitHub / ops actions (need repo owner)

| # | Action | Notes |
|---|--------|-------|
| O1 | Make repo **private** | currently public |
| O2 | Branch protection on `main` (PR + review + linked issue + checks) | enforces golden rules |
| O3 | Add `ADD_TO_PROJECT_PAT` secret | for project auto-add workflow |
| O4 | Move auto-add workflow to `main` | issue-triggered workflows run from default branch |
| O5 | Invite teammates to repo + project | |
| O6 | Remove old `services/catalog/src/` (git rm) | leftover from the structure flatten |

---

## ⏸️ Deferred — Product features

| # | Feature | Notes |
|---|---------|-------|
| P1 | **Virtual waiting room / queue system** for high-demand on-sales (Ticketmaster/AXS/Queue-it-style) | **Opt-in per event, not global** — most events never need it; a per-event flag (e.g. `RequiresQueue`) gates whether the waiting-room/admission-token check applies at all. Design sketch: a new, separate **Queue service** — Redis-backed queue per event (sorted set, atomic, horizontally scalable, mirrors the hot-path pattern Inventory already uses for holds); a background admission controller (same shape as Inventory's existing hold-expiry reaper) promotes a configurable number of waiting sessions to "Admitted" every N seconds; admission grants a short-lived, signed token scoped to one event + one session, required by the seat-selection/hold endpoints only when the event has queueing enabled; unused admissions expire the same way a `Hold` already does. Frontend gets a new waiting-room page (position/estimated wait, polls status, auto-redirects once admitted) shown only for queue-enabled events. Deliberately **not** in scope with the EventGroup/tour, General-Admission-vs-Reserved dual allocation, or enforced booking-cutoff work — it's an orthogonal traffic-shaping layer in front of the existing purchase flow, not a domain-model change, so it can be added later with zero rework of that work. |
| P2 | **Buyer flow: browse/select fully anonymous, OTP-gate only at "hold seats," OTP verification *is* login** | Refines ADR-0016's already-deferred Identity service (phone+OTP), captured ahead of building it. Decision: `EventsListPage`/`EventDetailPage` stay anonymous (already true today); `SeatSelectionPage` (currently behind `ProtectedRoute`) should *also* become reachable without a prior login — a buyer picks seats/GA quantities freely. The identity gate moves to the **hold action** itself (clicking "Hold selection"), not a separate upfront login wall and not deferred all the way to checkout: placing a hold claims real, scarce inventory, so it's the natural point to require a lightweight phone+OTP check anyway — this doubles as abuse prevention against anonymous hold-spam/seat-hoarding, which a no-login flow would otherwise leave wide open. A successful OTP verification mints the session/JWT on the spot (that *is* the login, no separate account-creation step); that identity then owns the hold through checkout and ticket issuance. "Complete your profile" (name, email, etc.) becomes an optional post-purchase prompt, never a blocker. Frontend implication: move the `ProtectedRoute` wrapper off `/events/:id/seats` and trigger OTP verification from the "Hold selection" button instead (e.g. an inline modal), not a route-level redirect to `/login`. Backend implication: none beyond what ADR-0016 already scopes for Identity (`POST /v1/otp/request`, `POST /v1/otp/verify` issuing a real JWT) — `HoldService.PlaceHoldAsync` already takes a `userId`, it would just be the just-verified buyer's id instead of a pre-existing session's. Not started; captured per explicit user direction, implementation to follow once prioritized. |
| P3 | **Archive/purge consumed tickets** — design only, nothing built (ADR-0021) | Once a ticket is scanned (checked in), its hot-table row is rarely needed again, but purging it immediately is unsafe (same-day disputes/re-entry). Phased design: **Phase 1** a same-DB `ticket_archive` table plus an organizer-triggered "archive this event's tickets" action (copy then delete from the hot `ticket` table) — the trigger is time-based-by-event, **not** per-ticket-at-scan-time. **Phase 2** an automatic sweep via a `BackgroundService` (`PeriodicTimer` + scoped `IServiceScopeFactory` + try/catch-continue, the exact shape `ExpiredHoldReaper` already establishes), triggered by a retention window past `Event.EndsAt` — needs `EndsAt` denormalized onto `Ticket` at issuance to avoid N cross-service calls to Catalog per sweep. **Phase 3** swaps the archive table for real blob/cold storage once storage cost (not just live-table size) justifies it — the same port-plus-swappable-adapter pattern already used for `IPaymentGateway`/`IEmailSender`, so it's a drop-in adapter later, not a rewrite. Not started. |

---

## Tech debt / to verify

| # | Item | Notes |
|---|------|-------|
| T1 | Confirm CPM package versions | ✅ build green — versions resolve, vulns patched |
| T2 | Build the scaffold locally (`dotnet build`) | ✅ green |
| T3 | MediatR pinned to **12.5.0** | do NOT let Dependabot bump to 13.x (commercial) — ADR-0014 |
| T4 | Dev HTTPS cert | `dotnet dev-certs https --trust` |
| T5 | Architecture-tests project (NetArchTest) | enforce ADR-0008/0009 boundaries in CI |
| T6 | Coverage gate + integration tests (Testcontainers) | Phase 1 exit criteria |
| T7 | Replace Catalog placeholder endpoint | ✅ done — real CreateEvent/GetEvent slices |
| T8 | Catalog EF Core migrations | 🚧 infra ready — EF Design ref (`PrivateAssets=all`), `CatalogDbContextDesignTimeFactory`, host now calls `MigrateAsync` in dev. **Remaining:** run `dotnet ef migrations add InitialCreate` (see [local-development](local-development.md#2-database-migrations-ef-core)) and commit `Migrations/`. Covers `events`, `seat_maps`, `seats`, `outbox` |
| T9 | Transactional outbox building-block | ✅ `EventPlatform.Messaging` — write path (`IEventPublisher`/`OutboxMessage`/`ApplyOutbox`) + `OutboxRelay` (Dapr pub/sub, at-least-once). Consumed side (dedupe on CloudEvent id) lands with Inventory (#7). Needs the `outbox` table in migrations (T8) |
| T10 | Seat-map read path returns all seats inline | fine at Phase-1 dev scale; page/stream `GET /seatmap` (and the Inventory hand-off) before large-venue maps (50k+ seats). `PublishEvent` also loads seats just to count — swap for a `COUNT(*)` |
| T11 | Money as minor units assumes 2-decimal currency | Inventory `ToMinor` does `amount × 100`; refine per ISO 4217 exponent (JPY = 0, etc.) before multi-currency |
| T12 | Order currency is defaulted to `USD` | `CheckoutOptions.DefaultCurrency`; derive from the Catalog event instead (Order → Catalog client) |

---

## How this is maintained

- Update on every meaningful change (part of "done").
- Cloud tasks stay in the **Deferred** tables until we explicitly resume cloud work.
- When board work resumes, mirror open items as GitHub issues.
