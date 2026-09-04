# v9 alignment — all 216 tickets

The reference architecture pack ("v9") specifies a ticketing platform as **216 backlog tickets**
across 20 families. This repository is a partial implementation of that architecture. This document
is the ledger: every one of the 216 tickets, what it asks for, whether we satisfy it, and where.

**Why this document exists.** The question that motivated it was "if we close the delta instead of
rebuilding, will we miss something?" A rebuild does not answer that question — only an itemised
audit does. This is that audit, and it is the completeness guarantee.

**Keep it current in the same commit as the work.** A ticket's row moves to ✅ in the commit that
closes it, not in a later sweep. A stale ledger is worse than none, because it is trusted.

## How to read this

| Mark | Meaning |
| --- | --- |
| ✅ | Satisfied. The named file/service does what the ticket asks. |
| ◐ | Partial. Something real exists; the gap is stated. |
| ✗ | Missing. Nothing in the tree does this. |
| ⊘ | Deliberately divergent. We do this differently on purpose; the ADR is named. |

**On ticket titles.** v9 names the individual tickets only for the PLAT family
([Sprint-Plan §Sprint 0]). Every other family is given as a range (`INV-001 to INV-013`) with its
scope defined by the design documents rather than per-ticket titles. The scope column below for
those families is therefore **derived** — from `04-Database/Detailed-Service-Table-Dictionary`,
`05-API/API-Contract-Catalog`, `06-Events/Core-Event-Catalog`, the `02-Architecture` design notes,
and the acceptance criteria that v9 does spell out for its representative tickets. Where a ticket
number's exact scope is a judgement call, the family total and the union of scope is what matters:
no capability from the v9 documents is dropped by a numbering disagreement.

**Counts.** 216 tickets: PLAT 20 · AUTH 7 · TEN 7 · AUD 8 · VEN 8 · MAP 17 · EVENT 16 · TOUR 5 ·
PRICE 11 · PROMO 13 · INV 13 · QUEUE 11 · ORDER 13 · PAY 7 · REF 5 · TKT 10 · COM 12 · GATE 10 ·
SEARCH 10 · REPORT 13.

**Status roll-up.**

| | ✅ | ◐ | ✗ | ⊘ |
| --- | --- | --- | --- | --- |
| At first audit | 71 | 52 | 90 | 3 |
| After the Venue service (ADR-0038) | 85 | 48 | 80 | 3 |
| After Catalog moved to performances (ADR-0039) | 86 | 51 | 76 | 3 |
| Now | 90 | 50 | 73 | 3 |

Read the first row honestly: **71 of 216** when this ledger was written, and lower than the
"~110 already satisfied" estimate given before going row by row — that estimate counted families
that *exist* rather than tickets that *pass*, which is exactly the error an itemised audit is for.
The rows below it are the Venue service, then event sessions (ADR-0039) in two landings: Catalog
first, which moved more tickets to ◐ than to ✅ because half of each one still hung off an event
id, and then Inventory/Ordering/Ticketing, which closed them. One ticket moved **backwards** in
that last row: MAP-006 (automatic seat numbering) was ✅ on the strength of Catalog generating seat
numbers, and Catalog's seat map is gone. Venue stores what it is given. Marking it ◐ is the honest
reading, and this ledger is worth nothing if it only ever moves one way.

---

## PLAT — Foundation (20)

The only family v9 titles individually.

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| PLAT-001 | Repository structure | ✅ | Monorepo: `services/`, `building-blocks/`, `gateways/`, `frontend/`, `platform/`, `infra/`, `deploy/`, `templates/`, `docs/`. No secrets committed. |
| PLAT-002 | .NET solution baseline | ✅ | `EventPlatform.sln`, `Directory.Build.props`, `Directory.Packages.props` (Central Package Management). |
| PLAT-003 | Coding standards | ✅ | `.editorconfig` + StyleCop + analyzers, warnings-as-errors. |
| PLAT-004 | AI coding guidelines | ✅ | Root `CLAUDE.md` + 14 per-area files + `docs/engineering-guidelines.md`. Exceeds v9's single `AI-CODING-GUIDELINES.md`: we add a build-error log and a mechanical pre-commit checker (`scripts/check-csharp-style.py`, `.githooks/pre-commit`). |
| PLAT-005 | CI/CD | ✅ | `.github/workflows/ci.yml`, `cd.yml`; Argo CD GitOps (`platform/argocd`, `deploy/`). |
| PLAT-006 | Docker | ✅ | Dockerfile per service; `docker-compose.yml` for local. |
| PLAT-007 | Kubernetes baseline | ✅ | `deploy/base` + `deploy/overlays` (Kustomize), `platform/dapr`. |
| PLAT-008 | External configuration | ◐ | Config is externalised and bound to options types, but **there is no startup validation** — no `ValidateOnStart`/`ValidateDataAnnotations` anywhere in the tree. v9 makes that mandatory. |
| PLAT-009 | Secrets management | ✅ | Azure Key Vault (`infra/modules/key-vault`), workload identity from AKS. Golden rule 7. |
| PLAT-010 | OpenTelemetry | ✅ | `EventPlatform.Hosting/ObservabilityExtensions.cs`; collector per ADR-0031. |
| PLAT-011 | Structured logging | ✅ | Same, via the OTel logging provider. |
| PLAT-012 | Health/readiness | ✅ | `EventPlatform.Hosting/HealthCheckExtensions.cs`; every service ships both (golden rule 8). |
| PLAT-013 | API/OpenAPI standards | ✅ | `OpenApiExtensions.cs` + Scalar at `/scalar/v1`; `docs/08-api-design.md`. |
| PLAT-014 | Error model | ✅ | ProblemDetails in `HostingExtensions.cs` and at the gateway. |
| PLAT-015 | Correlation IDs | ◐ | W3C `traceparent` propagates through OTel, and Communication carries a `CorrelationId` on delivery log entries. What is missing is a **platform-wide correlation id building block** — one that surfaces the id in ProblemDetails, in the audit record, and on every published event. Closed by the envelope work (see EVENTS §6 of the plan). |
| PLAT-016 | Idempotency framework | ◐ | Real idempotency exists on the money paths (Ordering checkout, Payments intents, Ticketing issuance), but each rolls its own. v9 wants a shared framework with a `ProcessedCommand` table. Ours is per-service and unverified across services. |
| PLAT-017 | Outbox framework | ✅ | `building-blocks/EventPlatform.Messaging` — `OutboxMessage`, `OutboxEventPublisher`, `OutboxRelay`, `IOutboxDbContext`. Business state and outbox commit in one local transaction. |
| PLAT-018 | Kafka integration | ⊘ | We use **Dapr pub/sub** (Redis Streams locally, Azure Service Bus in Azure). Same guarantees at the seam that matters — at-least-once with consumer idempotency — and Dapr keeps the broker swappable, so this is reversible rather than lost. ADR-0004/0010. |
| PLAT-019 | Redis integration | ✅ | Inventory's fast availability gate (never authoritative — matches v9's own rule). |
| PLAT-020 | MySQL baseline | ⊘ | **PostgreSQL**, database-per-service, EF Core migrations. A stack choice carried alongside v9's design, not a consequence of it. ADR-0003 (ours). |

## AUTH — Identity & Access (7)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| AUTH-001 | User model, tenant-scoped | ✅ | `Identity.Domain/BuyerAccount.cs`, `OrganizerAccount.cs`, `Tenant.cs`. |
| AUTH-002 | Authentication (OIDC-compatible tokens) | ✅ | JWT issue/validate; `SigningKey.cs`, `AuthenticationExtensions.cs`; ADR-0032. |
| AUTH-003 | Buyer authentication | ✅ | Phone OTP (`PhoneVerification.cs`); ADR-0016. |
| AUTH-004 | Organizer authentication | ✅ | Email + password register/login; ADR-0023. |
| AUTH-005 | **Roles map to stable permissions** | ✗ | We have a `role` claim with exactly two values and policies over it (`AuthorizationPolicies.cs`, ADR-0035). v9 requires a `Permission` table with stable codes, roles as collections of permissions, and the eight-role matrix in `08-Security/RBAC-Permission-Matrix`. **Zero occurrences of a permission concept in Identity.** |
| AUTH-006 | Role assignment API (`PUT /users/{id}/roles`, `GET /permissions`) | ✗ | No endpoints. |
| AUTH-007 | Privileged permission changes audited | ✗ | Depends on AUTH-005 and the Audit service. |

## TEN — Tenant (7)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| TEN-001 | Tenant entity + lifecycle status | ◐ | `Identity.Domain/Tenant.cs` exists but is a thin record inside Identity — no `Status`, no lifecycle. v9 gives Tenant its own service. |
| TEN-002 | Tenant CRUD API | ✗ | No `/v1/tenants` endpoints. |
| TEN-003 | Tenant slug (public identifier) | ✗ | Slugs exist on `Event`, not on `Tenant`. |
| TEN-004 | Tenant settings (`TenantSetting`, validated JSON) | ✗ | — |
| TEN-005 | Tenant branding (logo, primary colour) | ✗ | — |
| TEN-006 | **Tenant isolation, enforced and tested** | ✅ | Tenant context is established server-side (`TenantContextMiddleware`, `ITenantContext`) and never read from a request payload; cross-tenant reads return the opaque 404 pattern; `Event.IsVisibleTo` is unit-tested as a security boundary. Deny-by-default fallback policy per ADR-0035. |
| TEN-007 | `TenantCreated` event | ✗ | Not published. |

## AUD — Audit (8)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| AUD-001 | Audit record captures actor/tenant/action/entity/time/correlation | ◐ | `building-blocks/EventPlatform.Auditing` + `EventPlatform.Persistence/AuditFieldsInterceptor.cs` stamp **who and when onto the row being changed** (ADR-0036). That is audit *columns*, not audit *records*: there is no `AuditRecord` table, no action name, no correlation id. |
| AUD-002 | Before/after values on material changes | ✗ | The interceptor writes `CreatedBy`/`UpdatedBy`/timestamps only. |
| AUD-003 | Audit is append-only — normal APIs cannot update/delete | ✗ | No audit store to protect. |
| AUD-004 | Audit read API (`GET /v1/audit`, `/{id}`) | ✗ | — |
| AUD-005 | Redaction policy for sensitive values | ✗ | — |
| AUD-006 | Audit as a consumer of domain events | ✗ | — |
| AUD-007 | Correlation/causation on every audit record | ✗ | Blocked on PLAT-015 and the event envelope. |
| AUD-008 | Audit retention | ✗ | — |

*Sequenced last in the plan deliberately: an Audit service that consumes events is only worth
building once the events carry the envelope (`causationId`, `eventVersion`, actor) it needs to
record. Building it first would record a weaker fact permanently.*

## VEN — Venue (8)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| VEN-001 | `Venue` aggregate, tenant-owned, reusable across events | ✅ | `services/venue` — `Venues.Domain/Venue.cs`; ADR-0038. Catalog's `EventLocation` and its whole seat-map aggregate are **deleted**: a performance names a Venue seat-map version instead (ADR-0039). |
| VEN-002 | `VenueAddress` | ✅ | Owned value on `Venue`; columns on `venues`. |
| VEN-003 | Venue CRUD API (`POST/GET/PATCH /v1/venues`) | ✅ | `VenueEndpoints.cs` — create, list, get, update, activate, archive. Organizer-only, including the reads. `/admin/venues` in the SPA drives all of it. |
| VEN-004 | `VenueGate` — physical gate configuration | ✅ | `VenueGate.cs` — per **venue**, code unique within it, rename and deactivate rather than delete. Managed on the venue's Gates tab, and assignable per block in the seat-map editor. Event-time gate *authorization* stays with the scanning side. |
| VEN-005 | `VenueZone` | ✅ | Modelled as `AdmissionArea` — unreserved capacity with no seat identity. |
| VEN-006 | `VenueFacility` | ✅ | `VenueFacility.cs`; free text on purpose — the set differs by venue kind. |
| VEN-007 | Venue types | ✅ | `Venue.VenueType`, free text for the same reason. |
| VEN-008 | `VenueCreated` event → Search, Audit, Reporting | ✅ | `EventPlatform.Contracts/Venues/VenueCreated.cs`, published through the outbox. No consumers yet — those services do not exist. |

## MAP — Seat map (17)

v9's logical model is `Venue → SeatMap → SeatMapVersion → Section → Row → Seat`, with graphical
layout held separately. **We now have exactly that** (ADR-0038). What remains open in this family is
the *editor*, not the model: Catalog's legacy `Event → SeatMap → Seat` (section as a string on each
seat) still serves existing events until they move onto venue-owned maps.

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| MAP-001 | Graphical editor: create/edit elements | ◐ | `SeatMapEditorModal` under `/admin/venues` describes blocks as rows × seats and publishes them. No canvas — and it sends no shapes at all, which Venue accepts (it only rejects a *partly* drawn map), so the designer can add geometry later without invalidating anything made now. |
| MAP-002 | Logical identity separate from coordinates | ✅ | `SeatMapElement.cs` holds every coordinate; `Seat`/`SeatRow`/`VenueSection` hold none. Moving a block cannot change what a ticket names. ADR-0038. |
| MAP-003 | Zoom / pan | ✗ | — |
| MAP-004 | Drag / drop placement | ✗ | — |
| MAP-005 | Section / row / seat creation as first-class entities | ✅ | `VenueSection` → `SeatRow` → `Seat`, each an entity with its own identity, order and constraints. Section carries a stable `Code` that survives a rename. |
| MAP-006 | Automatic seat numbering | ◐ | Venue stores a seat number per seat as a **string** — real venues number seats `12A`, and an integer column makes that unrepresentable. Generating a row's numbering automatically is the editor's job and belongs with MAP-001. |
| MAP-007 | Bulk operations | ◐ | Whole-layout replacement (`PUT /draft/layout`) is the bulk primitive. Not bulk edit of an arbitrary selection in an editor. |
| MAP-008 | Stage / screen / facility elements | ✅ | `SeatMapElementKind`: `Stage`, `Entrance`, `Facility`, `Obstruction`, `Label`, plus section/area shapes. |
| MAP-009 | GA zones | ✅ | `AdmissionArea.cs` — capacity with no seat identity, alongside reserved sections in one version. Inventory provisions one `GeneralAdmissionAllocation` per area per performance. |
| MAP-010 | VIP / VVIP zones | ◐ | Expressible as a ticket type + section name; not a modelled zone kind. |
| MAP-011 | Accessible seats | ✅ | `SeatAttributes` flags — `Accessible`, `Companion`, `RestrictedView`, `Aisle`. Flags because they genuinely combine. |
| MAP-012 | Blocked / non-sellable areas | ✅ | `Seat.IsSellable` for permanently dead space (a camera position, no view at all) — a property of the building. Per-show hold-back stays Inventory's reversible blocking. |
| MAP-013 | Gates on the map | ✅ | `VenueSection.GateId` / `AdmissionArea.GateId`, validated against the venue at save **and** publish. Ticketing warms them per performance from the pinned version, so a scan enforces the gates that were in force when the tickets sold. |
| MAP-014 | Pricing-zone visualization | ◐ | Price is resolved per seat from the joined `TicketType`; nothing renders it as a zone. |
| MAP-015 | Import / export | ✗ | — |
| MAP-016 | Draft / version | ✅ | `SeatMapVersion` — one open draft at a time, a new draft pre-filled from the published layout with fresh ids, published versions immutable, superseded ones kept because tickets still resolve against them. The editor shows which version is open and refuses to edit a published one. |
| MAP-017 | Publish + validation (duplicate ids, hierarchy, capacity) | ✅ | `Validate()` returns **every** problem: duplicate codes, duplicate row labels, duplicate seat numbers, empty sections/rows/areas, missing geometry, a half-drawn map — and the editor renders the whole list rather than the first. Compatibility with the linked performance is checked from the Catalog side by `SessionPublishCheck`, which refuses a publish where any block is unallocated. |

*Progressive loading (venue overview → section geometry → selected-section seats) is v9's answer to
large-stadium rendering, and is still not implemented: `GET /v1/seat-maps/{id}` returns the whole
version. The model no longer stands in the way — `SeatMapElement` is exactly the overview layer — so
it is a read-endpoint change when stadium scale demands it, not a remodelling.*

## EVENT — Event & Tour (16)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| EVENT-001 | `Event` aggregate, tenant-owned | ✅ | `Catalog.Domain/Event.cs`. |
| EVENT-002 | Event slug, unique, locked after publish | ✅ | `EventSlug.cs`, `Event.ChangeSlug`, unique index; ADR-0037. Exceeds v9, which specifies `Event(TenantId, Slug UNIQUE)` — ours is **globally** unique, because a slug is the whole of a public URL. |
| EVENT-003 | Event page: description, media, category, age rating | ✅ | `Event.UpdatePresentation` + `EventPresentationForm.tsx`; editable at any status. |
| EVENT-004 | Event media | ◐ | Banner + video URL, uploaded through the Media service. No `EventMedia` collection, no ordering/captions. |
| EVENT-005 | **`EventSession`** | ✅ | `Catalog.Domain/EventSession.cs` — a performance with its own times, venue, pinned Venue seat-map version and `SessionAllocation` map; ADR-0039. Inventory, Ordering and Ticketing are all keyed on `EventSessionId`; a seat held for one night stays free on the next, and `RedisNoOversellTests` pins it. |
| EVENT-006 | Event categories | ◐ | Free-text `Category` string, not an entity. |
| EVENT-007 | `EventArtist` | ✗ | — |
| EVENT-008 | `EventTerm` (terms per event) | ✅ | `PolicyDocument.cs` — Terms/Privacy/Refund, versioned, tenant default with per-event override, HTML sanitised on write; ADR-0037. |
| EVENT-009 | Event lifecycle states | ◐ | Event: `Draft → Published`. Performance: `Draft → Published → Cancelled`, plus a per-session `SalesPaused`. Still missing v9's review/approval step and its terminal states (`SOLD_OUT`, `COMPLETED`, `POSTPONED`, `ARCHIVED`). |
| EVENT-010 | Publish: validate prerequisites, emit `EventPublished`, audit | ◐ | Publish validates draft + seat map and emits `EventPublished` through the outbox. Not audited as a record; no search projection to notify. |
| EVENT-011 | Submit for review / approve | ✗ | — |
| EVENT-012 | `EventVersion` — snapshot, changed-by, reason, approval state | ✗ | No version history. Post-publish edits are stamped by the audit-fields interceptor and otherwise unrecorded. |
| EVENT-013 | **Published-event change policy A–D** | ◐ | We implement a two-class split — presentation editable at any status, schedule Draft-only (ADR-0037). v9's four classes add: Class B (customer-impacting: elevated permission, reason, communication evaluation) and Class C (commercial/inventory: direct mutation *prohibited*, a controlled workflow with approval and compensating actions). Ours refuses Class C rather than working it. |
| EVENT-014 | Reschedule workflow | ✗ | No reschedule at all. Class D. |
| EVENT-015 | Cancel workflow + compensating refunds | ◐ | A performance can be cancelled and announced (`EventSessionCancelled`). No compensating refunds: working out who bought what and refunding them is a saga with approval in it, not a side effect of a status change. |
| EVENT-016 | Postpone | ◐ | A draft performance can be rescheduled and a published one cancelled (`EventSessionCancelled`). Postponing a *selling* performance — move the date, keep the tickets, tell the buyers — is still the Class D workflow, not an edit. |

## TOUR (5)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| TOUR-001 | `Tour` aggregate | ✅ | `EventGroup.cs` — thin, optional parent; ADR-0019. |
| TOUR-002 | `TourEvent` linkage | ✅ | `Event.EventGroupId`; each leg independently sellable. |
| TOUR-003 | Tour-wide defaults overridden per leg | ✅ | Date range + contact/social defaults; ADR-0020. |
| TOUR-004 | Tour API (`POST /v1/tours`, `/tours/{id}/events`) | ✅ | `/v1/event-groups`; anonymous `GET` by id. |
| TOUR-005 | Leg validation against the tour's range and siblings | ✅ | Overlap + range checks on create and update, 409; ADR-0024. Sequential creation is required for the sibling check to be correct — see `frontend/CLAUDE.md`. |

## PRICE — Catalog / Pricing (11)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| PRICE-001 | `TicketProduct` — what a section is sold as | ✅ | `Catalog.Domain/TicketType.cs`; `Seat`/`GeneralAdmissionSection` carry `TicketTypeId`. |
| PRICE-002 | Ticket product CRUD API | ✅ | `/v1/events/{id}/ticket-types` — create/list/update/deactivate. Not Draft-only, deliberately. |
| PRICE-003 | `PriceTier` | ◐ | Survives as a string on the seat map for the migration window; nothing reads it (price projects from the joined type). Not a modelled tier. |
| PRICE-004 | `TicketPrice` with `ValidFrom`/`ValidTo` | ✗ | One price per type, one row. |
| PRICE-005 | **Price phases** (early-bird → regular → door), unambiguous, never mutating confirmed orders | ✗ | The reason repricing is refused after publish today: Inventory holds a copy of the price from provisioning time, so a change here would move the displayed number and not the charged one. Phases fix the cause. |
| PRICE-006 | `TicketRule` — per-product limits, sales window | ◐ | `TicketType` carries `SalesStartsAt`/`SalesEndsAt`/`MaxPerBuyer`, but **Inventory does not enforce them** — it only knows the event-level `MaxTicketsPerBuyer` and window. So the fields exist and do nothing. |
| PRICE-007 | `OrderFeeRule` | ◐ | One flat `BookingFeePerTicketMinor` on the event; not a rule set. ADR-0034 documents the rounding. |
| PRICE-008 | Tax | ✅ | `TaxRatePercent`/`TaxLabel`, charged on the post-discount amount by Ordering; fee taxed separately; ADR-0034. |
| PRICE-009 | Prices snapshotted into the order | ✅ | `OrderLine`/`OrderPricing`; `POST /v1/checkout/quote` is the server-side authority and the frontend never adds anything up (ADR-0034). |
| PRICE-010 | `TicketTypeId` carried through Inventory and Ordering | ✅ | Provisioning joins the Venue seat map to the performance's `SessionAllocation` map by block code, so `InventoryItem.TicketTypeId` and `GeneralAdmissionAllocation.TicketTypeId` are set at source and carried through the hold snapshot to `OrderLine.TicketTypeId`. Promo-code tier scoping moved onto it too, replacing a free-text tier name that matched only by case-insensitive luck and broke on rename. |
| PRICE-011 | `TicketProductChanged` event → Inventory, Search, Reporting | ✗ | — |

## PROMO — Promotion (13)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| PROMO-001 | Promo validation: window, usage, per-customer, restrictions | ✅ | Catalog answers *what the rules are*; Ordering counts redemptions, because only Ordering can read orders. ADR-0034. |
| PROMO-002 | `Promotion` aggregate | ◐ | We have `PromoCode` + `PromoCodeTier` and no separate `Promotion` parent — a code *is* the promotion. Fine until bulk codes (PROMO-005). |
| PROMO-003 | Percentage and fixed-amount discount | ✅ | `DiscountType.cs`. |
| PROMO-004 | Validity window | ✅ | Optional start/end. |
| PROMO-005 | Bulk code generation (`POST /promotions/{id}/codes/bulk`) | ✗ | One code per create. |
| PROMO-006 | **Concurrent redemption cannot exceed usage limits** | ◐ | Total and per-buyer caps are modelled and checked at checkout. The check is not proven atomic under concurrent redemption; v9 makes that an explicit acceptance criterion. Needs a test that races it. |
| PROMO-007 | Tier/product scoping | ✅ | Optional tier scoping; **an empty tier list means every tier**, so a tier added later is covered rather than excluded. |
| PROMO-008 | Public vs private codes | ✅ | `.../promo-codes/public` deliberately never publishes redemption caps. |
| PROMO-009 | Deactivate | ✅ | `.../{id}/deactivate`. No edit-after-create, deliberately — an advertised code must not silently change value. |
| PROMO-010 | Discount recorded in the order snapshot | ✅ | `OrderPricing`; the code sent to checkout is the one the *server* accepted. |
| PROMO-011 | `PromotionUsage` table | ◐ | Redemptions are counted from orders rather than recorded as usage rows. Correct answers, no usage history, no cheap per-promotion analytics. |
| PROMO-012 | Stacking policy | ✗ | One code per order. |
| PROMO-013 | `PromotionCreated` event | ✗ | — |

## INV — Inventory (13)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| INV-001 | Reserved and GA represented independently, tenant-enforced | ✅ | `InventoryItem.cs` (per-seat) and `GeneralAdmissionAllocation.cs` (capacity pool). |
| INV-002 | **Reserved hold**: same seat cannot be held twice; configurable expiry; release returns it | ✅ | `Hold.cs`/`HoldItem.cs`; Redis fast gate in front of the Postgres ledger, Postgres authoritative. |
| INV-003 | **GA hold**: atomic capacity check, no oversell, idempotent release | ✅ | `GeneralAdmissionAllocation` + `LedgerEntry`. |
| INV-004 | Hold expiry | ✅ | `HoldStatus` + expiry sweep. |
| INV-005 | Confirm hold → sold | ✅ | `SeatSold`; `SOLD` is terminal for the allocation. |
| INV-006 | Block / unblock | ✅ | `SeatBlocked`/`SeatUnblocked` + `SeatBlockPanel`. |
| INV-007 | Availability read (`GET /sessions/{id}/availability`) | ✅ | `GET /v1/sessions/{eventSessionId}/inventory[/seats|/general-admission]`, anonymous, per performance. Named `/inventory` rather than `/availability`; the shape is v9's. |
| INV-008 | Sales window + pause enforcement | ✅ | `EventInventorySettings` from `EventPublished`; rejects holds before `OnSaleAt`, after `BookingEndsAt`, or while paused (ADR-0026/0027). |
| INV-009 | Per-buyer limit enforced cumulatively across holds | ✅ | `MaxTicketsPerBuyer` propagated via `EventPublished`; ADR-0021. Event-level only — per-type is PRICE-006. |
| INV-010 | **Reconciliation**: find orphaned holds and inconsistent counters without overwriting uncertain financial state | ✅ | The drift reconciler between Redis and the Postgres ledger. |
| INV-011 | Redis accelerates, never authoritative | ✅ | Matches v9's own rule exactly. |
| INV-012 | `TicketTypeId` on inventory | ✅ | See PRICE-010. |
| INV-013 | Hold-back / release-in-waves | ✗ | INV-012 no longer blocks it — the ticket type is on every row now. What is missing is the hold-back state itself and the release action. |

## QUEUE (11)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| QUEUE-001 | Waiting room per event | ✅ | `Queue.Domain/QueueSettings.cs`; provisioned from `EventPublished` when `RequiresQueue`; ADR-0026. |
| QUEUE-002 | Join / position / status | ✅ | Join + status endpoints. |
| QUEUE-003 | **Admission respects configured rate and concurrency; expired tokens cannot enter checkout** | ✅ | Admission tokens with expiry; Inventory refuses a hold without a valid token on a queued event. |
| QUEUE-004 | Anonymous key hashed, raw tokens not stored | ◐ | Tokens are opaque; v9 additionally requires hashing the anonymous key at rest. Not verified. |
| QUEUE-005 | Pause immediately stops new admission | ◐ | Sales pause stops holds; a queue-level pause/resume endpoint pair is not exposed separately. |
| QUEUE-006 | Resume | ◐ | Same. |
| QUEUE-007 | Queue session lifecycle (`CREATED→OPEN→ACTIVE→PAUSED→DRAINING→CLOSED`) | ✗ | No modelled session state machine. |
| QUEUE-008 | Token lifecycle (`WAITING→ADMITTED→EXPIRED/CANCELLED`) | ◐ | Implicit in expiry, not a modelled machine. |
| QUEUE-009 | Queue metrics emitted | ◐ | OTel traces/metrics from the service; no queue-specific metric contract. |
| QUEUE-010 | `QueueTokenAdmitted` event → Order, Reporting | ✗ | Admission is checked synchronously; nothing is published. |
| QUEUE-011 | `QueueAudit` | ✗ | — |

## ORDER — Order / Checkout (13)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| ORDER-001 | **Create order**: unique number, snapshotted prices, tenant validated, idempotent, no cross-service DB access | ✅ | `Order.cs`, `OrderLine.cs`, `OrderPricing.cs`; Dapr workflow saga in `Ordering.Workflow`. |
| ORDER-002 | Cart | ⊘ | We hold **inventory** rather than a cart: the buyer's selection *is* a hold with a countdown, and checkout converts it. A cart that does not hold inventory would let two buyers fill carts with the same seat, which is the bug the hold exists to prevent. Recorded as divergence, not a gap. |
| ORDER-003 | **Checkout**: guest purchase, valid hold, server-side totals, snapshot, idempotent retry | ◐ | Everything except **guest checkout** — the buyer must complete phone OTP before the hold is placed (`OtpLoginFlow`). v9 (ADR-009) makes account-less purchase a Day-1 requirement. |
| ORDER-004 | `Customer` / `CustomerAddress` | ✗ | Buyer identity lives in Identity; Ordering has no customer model, so a guest has nowhere to land. Blocks ORDER-003. |
| ORDER-005 | Order status machine | ◐ | Ours: `Pending → Paid → Confirmed → Cancelled/Failed`. v9 adds `INVENTORY_HELD`, `PAYMENT_PENDING`, `TICKETS_ISSUED`, `COMPLETED`, `EXPIRED`, `REFUND_PENDING`, `REFUNDED`, `PARTIALLY_REFUNDED`. The refund states are the material absence. |
| ORDER-006 | `OrderCharge` — fees and tax as rows | ◐ | Totals are computed and stored (`OrderPricing`), not itemised as charge rows. `PriceRow` renders them; the breakdown is derived. |
| ORDER-007 | `OrderPromotion` | ◐ | Applied code + discount stored on the order; no join row. |
| ORDER-008 | `CheckoutAttempt` | ✗ | Attempts are not recorded, so a failed checkout leaves no trace to explain. |
| ORDER-009 | `ProcessedCommand` (idempotency ledger) | ◐ | Idempotency keys are honoured on the workflow activities; no shared table. See PLAT-016. |
| ORDER-010 | **Cancellation**: valid transitions, inventory release, communication, audit | ◐ | Cancel exists and releases inventory. No communication trigger, no audit record. |
| ORDER-011 | Order read API | ✅ | `GET /v1/orders/{id}`, plus `mine=true`/`forTenant=true` lists (the endpoint 400s without one — deliberate). |
| ORDER-012 | `OrderCreated` / `OrderCancelled` events | ◐ | `OrderConfirmed` is published. `OrderCreated` and `OrderCancelled` are not. |
| ORDER-013 | Order tied to a session, not an event | ✅ | `Order.EventSessionId`, threaded from the hold snapshot through `CreateOrderInput`/`ConfirmInput` and out on `OrderConfirmed`. `CatalogEventId` stays alongside it because promo codes and the per-buyer cap are decided for the whole run. |

## PAY — Payment (7)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| PAY-001 | **Payment**: provider by configuration, external secrets, no duplicate success, validated transitions, `PaymentSucceeded` via outbox | ✅ | `Payments.Domain/Payment.cs`, `PaymentStatus.cs`; Stripe adapter; intent created before the form mounts (ADR-0028); PCI SAQ-A — card data only ever reaches Stripe's iframe. |
| PAY-002 | **Capture**: amount/currency matches the authorized order; duplicates cannot create a second logical payment | ✅ | Server re-derives the amount from the order snapshot; never trusts the client. |
| PAY-003 | `PaymentTransaction` history | ◐ | Status transitions on one row; no append-only transaction table. |
| PAY-004 | **Webhook**: signature verified, provider event id deduplicated, unknown callbacks rejected safely | ✅ | Stripe signature verification + event-id dedupe. |
| PAY-005 | `PaymentWebhookEvent` table | ✅ | Dedupe store for provider event ids. |
| PAY-006 | `PaymentProviderAccount` — multiple providers / accounts | ◐ | One provider, configuration-selected, with a no-Stripe dev fallback. The adapter seam exists; a second provider has never been wired. |
| PAY-007 | `PaymentReconciliation` | ✗ | — |

## REF — Refund (5)

v9 gives Refund its own service. We keep refunds in Payments — same aggregate, same provider, same
ledger, and a distributed saga where a local transaction works. **Recorded as a boundary
divergence; the capabilities below are still owed.**

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| REF-001 | `Refund` aggregate; full and partial | ◐ | `PaymentRefunded` + `RefundActivity` in the ordering workflow refund a payment. There is no `Refund` aggregate, so there is no refundable-balance arithmetic and partial refunds are not expressible. |
| REF-002 | **Approval**: permission and policy enforced, cannot exceed refundable balance, audited | ✗ | No approval step, no balance. |
| REF-003 | Refund request API (`POST /v1/refunds`, `GET /orders/{id}/refunds`) | ✗ | Refund is an internal saga activity, not a requestable action. |
| REF-004 | **Processing**: provider retry cannot duplicate, result reconciled, `RefundSucceeded/Failed` emitted | ◐ | Retry is idempotent through the activity's key. No reconciliation; `PaymentRefunded` is published but no success/failure pair. |
| REF-005 | Multiple refund transactions against a remaining balance | ✗ | — |
| — | Boundary | ⊘ | Refund stays inside Payments. To be recorded as an ADR when REF-001 lands. |

## TKT — Ticket (10)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| TKT-001 | **Issue ticket**: unique identity, correct linkage, reserved *or* GA reference, idempotent, reliably published | ✅ | `Ticketing.Domain/Ticket.cs`; issued from `OrderConfirmed`; `TicketIssued`/`OrderTicketsIssued` through the outbox. |
| TKT-002 | Unique ticket number + QR reference | ✅ | Scan token per ticket. |
| TKT-003 | Reserved vs GA admission reference | ✅ | `SeatEntryGate.cs` / `GaAllocationGate.cs`. |
| TKT-004 | Ticket read API | ✅ | Per order and per ticket. |
| TKT-005 | Ticket delivery | ◐ | Communication sends the confirmation; no `TicketDelivery` record or redelivery. |
| TKT-006 | `TicketStatusHistory` | ✗ | `TicketStatus` is a current value with no history. |
| TKT-007 | **Void**: authorized, unusable afterwards, projections updated, audited | ✗ | No void. A ticket cannot be invalidated today — the gap that makes EVENT-015 (cancel) impossible. |
| TKT-008 | `TicketHolder` — name the ticket, per-attendee | ✗ | Every ticket in an order belongs to the buyer. |
| TKT-009 | `TicketTransfer` | ✗ | v9 reserves the model for future use; we have neither. |
| TKT-010 | Ticket lifecycle (`CREATED→ISSUED→DELIVERED→VALIDATED→USED`, + `CANCELLED/VOIDED/EXPIRED`) | ◐ | Ours: `Issued → CheckedIn`. Missing delivery, void, cancel, expiry. |

## COM — Communication (12)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| COM-001 | `CommunicationTemplate` | ✅ | `NotificationTemplate.cs`, `TemplateKeys.cs`. |
| COM-002 | Channels (email, SMS) | ✅ | `NotificationChannel.cs`. |
| COM-003 | **Delivery**: provider by configuration, bounded retry, duplicate prevented where an idempotency key exists, status observable | ◐ | Provider is configuration-selected and delivery status is logged (`DeliveryLogEntry`, `DeliveryStatus`). Retry is not a bounded documented policy and duplicate prevention is not keyed. |
| COM-004 | Send API (`POST /v1/communications/send`) | ✅ | `SendNotificationCommand`. |
| COM-005 | Communication read API | ◐ | Delivery log is queryable internally; no `GET /v1/communications/{id}`. |
| COM-006 | Template list API | ✗ | — |
| COM-007 | `CommunicationRecipient` | ◐ | One recipient per send. |
| COM-008 | `DeliveryAttempt` history | ◐ | One log entry per send, not per attempt. |
| COM-009 | `ProviderAccount` | ✗ | — |
| COM-010 | Order confirmation flow | ✅ | Triggered from `OrderConfirmed`. |
| COM-011 | Event-change / cancellation flows | ✗ | Blocked on EVENT-014/015. |
| COM-012 | `CommunicationDelivered` event | ✗ | — |

## GATE — Access / Gate (10)

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| GATE-001 | **Scan**: validates session, ticket status and gate rule; duplicate detected; configurable authorization; auditable | ◐ | The scan now validates against the **performance** — body `{ token, eventSessionId, gateId? }`, checked against that night's window — plus ticket status and gate eligibility, resolved from a locally warmed cache of the pinned Venue version (ADR-0025). A duplicate is detected, and `ScanTicketPage` makes the scanner pick event → performance → gate, defaulting to the performance happening now or next. Still missing: the scan is not written to an audit store. |
| GATE-002 | Accept / reject with reason codes | ◐ | Accept/reject with a message; no stable `ReasonCode` vocabulary. |
| GATE-003 | `Scan` record with operator, device, correlation | ✗ | Check-in is a status change on the ticket. No scan record — so "who scanned this, on which device" is unanswerable, and GATE reporting has no source. |
| GATE-004 | `AccessRule` — data-driven per session/type/area/gate with time windows | ✗ | Gate eligibility is a fixed reference on the seat, not a rule with `AllowedFrom`/`AllowedTo` or a capacity limit. |
| GATE-005 | `Gate` / `GateSession` | ✗ | — |
| GATE-006 | Gate status API (`GET /sessions/{id}/gate-status`) | ✗ | — |
| GATE-007 | `GateCapacitySnapshot` | ✗ | — |
| GATE-008 | Scan idempotency | ✅ | A second scan of the same token is detected, not double-counted. |
| GATE-009 | `TicketScanned` event → Reporting, Audit | ✗ | — |
| GATE-010 | Offline scanning + conflict resolution | ✗ | v9 makes this conditional ("if enabled for a deployment"). Not enabled; the sync/conflict behaviour it demands is therefore not owed until it is. |

## SEARCH (10)

**No search service exists.** The buyer event list is a paged query against Catalog. Every ticket
below is missing; they are listed individually so the family is not collapsed into one line.

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| SEARCH-001 | Search service + index store | ✗ | — |
| SEARCH-002 | Event document schema | ✗ | — |
| SEARCH-003 | Venue document schema | ✗ | — |
| SEARCH-004 | **Projection**: only eligible published events indexed; tenant/public visibility enforced; lag observable; rebuildable from events | ✗ | The visibility rule itself exists and is tested (`Event.IsVisibleTo`); nothing projects it. |
| SEARCH-005 | Event search API (`GET /v1/search/events`) | ◐ | `GET /v1/events` with filters serves the storefront today — honest for the current catalogue size, not a search index. |
| SEARCH-006 | Venue search API | ✗ | `VenueCreated` is published and nothing consumes it. |
| SEARCH-007 | Faceting / filters | ◐ | Category and date filters on the Catalog list. |
| SEARCH-008 | Availability projection | ✗ | — |
| SEARCH-009 | Index version + rebuild | ✗ | — |
| SEARCH-010 | Projection lag metric | ✗ | — |

## REPORT — Reporting (13)

**No reporting service and no analytical store exist.** Admin grids export the rows they were
given (`DataGrid` + `csv.ts`), which is the honest client-side behaviour and not a report.

| # | Scope | | Where / gap |
| --- | --- | --- | --- |
| REPORT-001 | **Sales report** from analytical data, mandatory tenant filter, event/session/date filters, freshness shown | ✗ | — |
| REPORT-002 | Separate analytical store | ✗ | Reports would read the transactional databases, which the service boundary forbids. |
| REPORT-003 | Dimensional model (`DimTenant/Event/Session/Venue/TicketProduct/CustomerSegment/Date/Gate`) | ✗ | `DimVenue`, `DimSession` and `DimTicketProduct` all have a real grain to hang off now (VEN-001, EVENT-005, PRICE-010), and orders/tickets/scans key on it. Nothing projects them yet — but a projection built now would be built on the right grain. |
| REPORT-004 | `FactOrder` / `FactOrderItem` | ✗ | — |
| REPORT-005 | Revenue report, reconciling to transactional sources | ✗ | — |
| REPORT-006 | Inventory report | ✗ | — |
| REPORT-007 | Promotions report | ✗ | Needs PROMO-011 for usage history. |
| REPORT-008 | Refunds report | ✗ | Needs REF-001. |
| REPORT-009 | Gate-entry report | ✗ | Needs GATE-003 — there is no scan record to aggregate. |
| REPORT-010 | Queue report | ✗ | Needs QUEUE-010. |
| REPORT-011 | Tenant report | ✗ | — |
| REPORT-012 | Export with row and permission limits | ◐ | Client-side CSV per grid page, with RFC 4180 quoting and formula-injection guarding. Not permission- or row-limited server-side. |
| REPORT-013 | Data freshness / last-processed timestamp | ✗ | — |

---

## What the audit says

Three gaps were structural, and everything else queued behind them. **Two are now closed.**

1. ~~**Venue (VEN-001)**~~ — done. `services/venue` owns venues, gates, facilities and versioned
   seat maps with logical identity separated from graphical layout (ADR-0038). There is no legacy
   path left beside it: Catalog's `EventLocation`, `SeatMap`, `Seat`, `GeneralAdmissionSection` and
   `EntryGate` were deleted rather than deprecated, because a seat map that lives in Catalog cannot
   be shared between events, which is the whole reason Venue exists.
2. ~~**`EventSession` (EVENT-005)**~~ — done. Inventory, orders, tickets and scans key on
   `EventSessionId`; a three-night run is one event with three performances, and the same seat is
   three separately sellable rows. Three things stay event-scoped on purpose and say so in code:
   the per-buyer cap (a cap counted per night is not a cap), the queue admission token (one waiting
   room gates one on-sale), and promo codes. REPORT is no longer blocked on the grain — a
   projection built now would be built on the right one.
3. **The event envelope (PLAT-015 / AUD-007)** — no `causationId`, no `eventVersion`, no DLQ story.
   Audit consumes events, so Audit is worth building only after the envelope carries what it needs
   to record. **This is now the only structural gap left.**

One of the two cheap unblockers is done: **`TicketTypeId` through Inventory (PRICE-010 /
INV-012)** landed with the re-key, because provisioning had to carry the type anyway. That leaves
**price phases (PRICE-005)**, which removes the "no repricing after publish" restriction by fixing
its cause instead of refusing the operation, and makes per-type hold-back (INV-013) the next
reachable thing.

Two are honest divergences worth defending rather than closing: **hold-instead-of-cart
(ORDER-002)** and **refund-inside-payment (REF)**.

One is a Day-1 v9 requirement we simply do not meet and should: **guest checkout (ORDER-003 /
ORDER-004)**. A buyer must complete an OTP before they can hold a seat.
