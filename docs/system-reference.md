# EventPlatform — System Reference

The single **as-built** description of what this platform does and how it works.
Grounded in the code on `claude/enterprise-ticket-platform-w3opb0`, not in the
pre-implementation design docs (those are in [docs/design/](design/) and are
deliberately frozen).

This supersedes the former `data-flow-and-service-boundaries.md`.

> **How to read this alongside everything else.** The [ADRs](adr/) say *why*
> each choice was made and are the authority on intent. Per-service
> `CLAUDE.md` files carry the detail a developer needs while editing that
> service. This document is the map between them. See the
> [documentation map](#documentation-map) at the end.

---

## 1. What it is

A multi-tenant SaaS ticketing platform for high-demand live events — the kind
where a popular on-sale means thousands of people racing for the same seat in
the same second, and where selling one seat twice is unacceptable.

Two products share one backend:

- a **public buyer experience** — browse events, pick seats, queue if it's busy,
  pay, receive tickets
- an **organizer back office** — create events and tours, define seating, set
  pricing and limits, publish, monitor sales, scan people in at the door

**Multi-tenant:** every organizer is a tenant. Their events, inventory and
orders are theirs; a buyer, by contrast, is *not* tenant-scoped — one person
buys from many organizers over time (ADR-0022).

---

## 2. The map

```
                    ┌──────────────────────────────────┐
   Browser  ───────▶│  Gateway (YARP)  :5090           │
   (React SPA)      │  route allowlist · CORS · auth   │
                    │  pass-through                    │
                    └───────────────┬──────────────────┘
                                    │  /api/<service>/v1/...
        ┌───────────┬───────────┬───┴───────┬───────────┬───────────┐
        ▼           ▼           ▼           ▼           ▼           ▼
   ┌────────┐  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌────────┐ ┌────────┐
   │catalog │  │inventory│ │ordering │ │ticketing│ │identity│ │ queue  │
   │ :5080  │  │  :5081  │ │  :5082  │ │  :5084  │ │ :5087  │ │ :5088  │
   └────────┘  └─────────┘ └─────────┘ └─────────┘ └────────┘ └────────┘
                                │                                    
                     not gateway-routed (internal only)              
        ┌───────────────────────┼────────────────────────┐           
        ▼                       ▼                        ▼           
   ┌─────────┐          ┌──────────────┐          ┌──────────┐       
   │payments │          │communication │          │  media   │       
   │  :5083  │          │    :5085     │          │  :5086   │       
   └─────────┘          └──────────────┘          └──────────┘       

   Every service: own Postgres database · Dapr sidecar · OpenTelemetry
   Async between services: Dapr pub/sub, fed by a transactional outbox
   Sync between services:  Dapr service invocation (by app-id)
```

**Nine services**, each owning exactly one database and never reading another's
tables — only its HTTP API (sync) or its published events (async).

| Service | One-line job |
|---|---|
| **catalog** | Events, tours, seat maps, pricing, entry gates. The source of truth for *what is being sold*. |
| **inventory** | Seats and general-admission capacity: holds, no-oversell, block/unblock. The source of truth for *what is still available*. |
| **ordering** | Checkout. Runs the saga that turns a hold into a paid, confirmed order. |
| **payments** | Money. Stripe integration, idempotency, refunds, reconciliation. |
| **ticketing** | Issues tickets, generates QR codes, scans people in at the gate. |
| **identity** | Who you are. Buyer phone+OTP, organizer email+password, RS256 token issuer. |
| **queue** | Virtual waiting room for high-demand on-sales. Opt-in per event. |
| **venue** | Places: venues, gates, facilities, and versioned seat maps. The source of truth for *where it happens and which seats exist* — never for price or availability (ADR-0038). |
| **communication** | Every outbound email/SMS/WhatsApp. One audit trail, one vendor integration. |
| **media** | Image upload to blob storage. Deliberately tiny and flat. |

Plus **gateway** (`:5090`, YARP — the only thing a browser talks to) and
**frontend** (React + Vite + Ant Design SPA, buyer and admin in one app).

---

## 3. Capabilities

### For organizers

| Capability | Where |
|---|---|
| Register an organization (creates the tenant + first account) | identity |
| Create an event: title, dates, inline location, currency | catalog |
| Group events into a **tour** — multiple cities/dates under one heading, each leg independently sellable | catalog |
| Rich detail: description, category, banner image, video embed, age restriction, contact + social links | catalog + media |
| Define a seat map: **reserved** sections (individually addressable seats) and/or **general-admission** sections (capacity only) — the two can mix in one event | catalog |
| Edit seat-map sections after definition (add / replace / delete by name) | catalog |
| Define **entry gates** and restrict a section to one | catalog |
| Set an on-sale time, a booking cutoff, and a max-tickets-per-buyer limit | catalog → inventory |
| Publish an event (provisions inventory across services) | catalog |
| **Pause and resume sales** manually, without unpublishing | catalog → inventory |
| Turn on a **virtual waiting room** and tune its admission rate | catalog → queue |
| Block / unblock individual seats (holds, comps, damage) | inventory |
| See live per-seat status, including who has been checked in | inventory + ticketing |
| View orders for the tenant | ordering |
| **Scan tickets at the gate**, by camera or hardware scanner, gate-aware | ticketing |

### For buyers

| Capability | Where |
|---|---|
| Browse published events anonymously — no login to look | catalog |
| See full event detail, seat map and live availability, anonymously | catalog + inventory |
| Join a waiting room when the event requires one | queue |
| Pick reserved seats and/or GA quantities, mixed in one selection | inventory |
| **Log in only at the point of holding seats** — phone + OTP, no upfront wall | identity |
| Hold seats for a short window while deciding | inventory |
| Pay by card (3-D Secure), UPI, or anything else enabled on the Stripe account | payments |
| Receive tickets by email, one combined message per order | ticketing → communication |
| View orders and tickets, with scannable QR codes | ordering + ticketing |
| Cancel a confirmed order — tickets voided, seats released, money refunded | ordering |

### Guarantees the system makes

| Guarantee | How |
|---|---|
| **A seat is never sold twice** | Redis atomic Lua gate on the hot path, Postgres with optimistic concurrency as the authority |
| **A buyer is never double-charged** | Idempotency key on checkout (unique index + pre-check) and on the Stripe call |
| **Money and inventory never diverge silently** | Transactional outbox — a state change and its event commit together or not at all |
| **An event is never delivered twice** | Dedup ledgers (`ProcessedWebhookEvent`, `ProcessedNotificationEvent`) keyed on event id |
| **A captured payment always reaches an order, or is refunded** | Three routes to the outcome plus a reconciliation sweep (§5) |
| **Abandoned holds return to sale** | `ExpiredHoldReaper` background service |
| **One tenant never sees another's data** | Tenant derived from the resource for buyer paths, from the token claim for organizer paths (ADR-0022) |

---

## 4. User journeys

### Buyer: browse → ticket

```
1. Browse           GET /api/catalog/v1/events                    anonymous
2. Event detail     GET .../events/{id}  + /seatmap  + inventory  anonymous
3. Queue?           event.requiresQueue → /events/{id}/queue      anonymous
                    POST .../queue/join → position, then poll
                    until admitted → HMAC admission token
4. Pick seats       GET .../inventory/seats  (live per-seat status)
5. Hold selection   ← IDENTITY GATE: OTP modal appears here, not before
                    POST /api/inventory/v1/holds  { seats, gaQuantities,
                                                    queueAdmissionToken? }
                    → 201 holdId, expires in ~2 min
6. Checkout         POST /api/ordering/v1/checkout { holdId, buyerEmail }
                    + Idempotency-Key header
                    → 200 { orderId, clientSecret }
                    Hold is extended to ~15 min for authentication
7. Pay              Stripe Payment Element in the browser.
                    3-D Secure / UPI app-switch happen entirely
                    between the buyer and Stripe — card data never
                    touches our servers (PCI SAQ-A).
8. Confirm          Browser tells us the moment it resolves;
                    webhook and saga poll are backstops (§5)
                    → order Confirmed, seats Sold, tickets minted
9. Tickets          Email arrives (one per order, all tickets listed);
                    /orders/{id} shows QR codes
```

The identity gate at step 5 is deliberate: everything up to *committing* to
seats is anonymous, so a buyer is never asked to log in before they know they
want something.

### Organizer: idea → doors open

```
1. Register/login   POST /api/identity/v1/organizers/register
                    → creates Tenant + OrganizerAccount, returns a token
                      carrying role=organizer and tenant_id
2. Create           /admin/events/new — one page, one or many legs.
                    Adding a second leg turns it into a tour.
3. Enrich           Upload a banner (media), add description, category,
                    contact/social, age restriction, video
4. Seat map         Reserved sections (rows × seats) and/or GA sections
                    (capacity). Optionally bind sections to entry gates.
5. Rules            On-sale time, booking cutoff, max tickets per buyer,
                    waiting room on/off
6. Publish          POST .../events/{id}/publish
                    → EventPublished fans out:
                      inventory  provisions seats + GA allocations + settings
                      ticketing  warms its scan cache
                      queue      provisions queue settings
7. Monitor          Seat panel (live status), tenant order list
                    Pause/resume sales at any time
8. Doors            /admin/scan — pick event + gate, scan by camera or
                    hardware wedge scanner
```

### Gate staff: scanning

```
POST /api/ticketing/v1/tickets/scan  { token, eventId, gateId? }

  404  unknown token — or a token for a different event
       (same response deliberately: a wrong-event scan must not
        reveal that the token is valid somewhere else)
  409  outside the check-in window · wrong gate · already used · void
  200  checked in — CheckedInAt stamped, seat turns a distinct colour
       in the organizer's seat panel
```

Every check is a **local** read. Ticketing warms an event's window and gate
rules into its own database when the event publishes, so a turnstile at peak
does zero cross-service calls (ADR-0025).

---

## 5. How data flows

### The two backbones

**Synchronous** — Dapr service invocation by app-id. Used when the caller needs
an answer now: the checkout saga calling Inventory and Payments, Identity
calling Communication to send an OTP.

**Asynchronous** — Dapr pub/sub, fed by a **transactional outbox**. A service
writes its state change and its outgoing event in the *same database
transaction*; a relay publishes from the outbox on a timer. The event cannot be
lost if the write succeeded, and cannot be sent if it didn't.

### Integration events

| Event | Published by | Consumed by |
|---|---|---|
| `EventPublished` | catalog | inventory (provision), ticketing (warm scan cache), queue (provision) |
| `EventSalesPaused` / `EventSalesResumed` | catalog | inventory |
| `EventUpdated` | catalog | *(no consumer yet)* |
| `SeatHeld` · `SeatReleased` · `SeatSold` · `SeatBlocked` · `SeatUnblocked` | inventory | *(no consumer yet — audit/analytics hooks)* |
| `PaymentCaptured` · `PaymentFailed` | payments | ordering (resumes the waiting saga) |
| `PaymentRefunded` | payments | *(no consumer yet)* |
| `OrderConfirmed` | ordering | ticketing (issue tickets), communication |
| `TicketIssued` | ticketing | communication |
| `OrderTicketsIssued` | ticketing | communication (sends the combined email) |

### The checkout saga

The most intricate flow in the system. It runs as a **Dapr Workflow** — a
durable orchestrator that survives process restarts by replaying its history.

```
POST /v1/checkout
  │
  ├─ orderId minted here, and used as the workflow's instance id
  │  (so anything can later resume this exact saga with no lookup table)
  │
  ▼
1  FetchHold            validate owner, active, not expired
2  FetchEventCurrency   price in the event's own currency  ← UPI needs INR
3  CreateOrder          status AwaitingPayment
4  CreateIntent         Stripe PaymentIntent — created, NOT confirmed
   RecordPaymentIntent  client secret onto the order row
   ExtendHold           ~2 min → ~15 min, to cover authentication
   │
   │   ← API returns { orderId, clientSecret } here; the saga keeps waiting
   │
   ▼
   WAIT for the payment outcome, whichever arrives first:
     ① browser  POST /v1/orders/{id}/payment/sync   (instant, the common case)
     ② webhook  Stripe → payments → PaymentCaptured → ordering subscriber
     ③ poll     saga asks Payments every 20s, which re-reads Stripe
   │
   ▼
5  Convert             hold → sold
6  ConfirmOrder        → OrderConfirmed → tickets minted, email sent
```

**Why three routes.** The browser knows first — `confirmPayment` resolves with
the confirmed intent in hand — so it tells us immediately. The webhook is
authoritative and survives the browser closing, but cannot reach `localhost`.
The poll needs neither. All three land on the same reconciliation, and every
transition is a `TryMark*`, so whoever arrives second is a harmless no-op
(ADR-0028).

**Compensation.** Payment failed or timed out → fail the order, release the
hold. Convert failed after capture → fail, **refund**, release. Intent creation
threw → fail, release (no money moved, nothing to refund).

**The last backstop.** A buyer who authenticates and then vanishes, on a machine
no webhook can reach, is watched by nothing. `StalePaymentReconciler` sweeps
payments left `Initiated` past 20 minutes: captured ones emit `PaymentCaptured`
(which Ordering refunds if the saga already gave up), the rest are cancelled at
Stripe and failed so the seats come back.

### The no-oversell path

```
POST /v1/holds
  ├─ settings checks   sales paused? on-sale started? past booking cutoff?
  │                    buyer already at their per-event limit?
  │                    queue required and no valid admission token?
  ├─ Redis Lua         atomic check-and-set across every requested seat and
  │                    GA counter — all or nothing, no partial holds
  └─ Postgres          Hold + HoldItems written, outbox event enqueued
                       (optimistic concurrency; a lost race releases Redis)
```

Redis is the **fast gate**, Postgres the **authority**. The gate exists because
at on-sale peak thousands of requests contend for the same rows; Lua makes the
decision atomic without a database round trip per attempt. `ExpiredHoldReaper`
reconciles anything the TTL let lapse.

---

## 6. Service reference

| Service | Owns | Key API | Publishes | Consumes |
|---|---|---|---|---|
| **catalog** | `Event` (+ social links), `EventGroup` (+ social links), `SeatMap`, `Seat`, `GeneralAdmissionSection`, `EntryGate` | `POST/GET /v1/events` · `GET /v1/events/{id}` · `POST/PUT/DELETE .../seatmap[/sections]` · `PUT .../details` · `POST .../publish` · `POST .../pause-sales`,`.../resume-sales` · `/v1/event-groups` · `.../entry-gates` | `EventPublished`, `EventSalesPaused`, `EventSalesResumed`, `EventUpdated` | — |
| **inventory** | `InventoryItem`, `GeneralAdmissionAllocation`, `EventInventorySettings`, `Hold`, `HoldItem`, `HoldGeneralAdmissionItem`, `LedgerEntry` (+ Redis) | `POST /v1/holds` · `GET/DELETE /v1/holds/{id}` · `GET .../inventory[/seats|/general-admission]` · `POST .../inventory/block`,`/unblock` · *internal:* `/holds/{id}/convert`,`/release`,`/extend`,`/cancel` | `SeatHeld`, `SeatReleased`, `SeatSold`, `SeatBlocked`, `SeatUnblocked` | `EventPublished`, `EventSalesPaused`, `EventSalesResumed` |
| **ordering** | `Order`, `OrderLine` (+ workflow state) | `POST /v1/checkout` · `GET /v1/orders[?mine|?forTenant]` · `GET /v1/orders/{id}` · `POST /v1/orders/{id}/payment/sync` · `POST /v1/orders/{id}/cancel` | `OrderConfirmed` | `PaymentCaptured`, `PaymentFailed` |
| **payments** | `Payment`, `ProcessedWebhookEvent` | *internal:* `POST /v1/payments/intents` · `POST /v1/payments/{orderId}/sync` · `POST /v1/payments/refund` · *public:* `POST /v1/payments/webhooks/stripe` | `PaymentCaptured`, `PaymentFailed`, `PaymentRefunded` | — |
| **ticketing** | `Ticket`, scan cache (`EventScanContext`, gate assignments) | `GET /v1/orders/{id}/tickets` · `GET /v1/tickets/{id}[/qrcode]` · `GET /v1/events/{id}/tickets` · `POST /v1/tickets/scan` · *internal:* `.../tickets/void` | `TicketIssued`, `OrderTicketsIssued` | `OrderConfirmed`, `EventPublished` |
| **identity** | `PhoneVerification`, `BuyerAccount`, `Tenant`, `OrganizerAccount`, `SigningKey` | `POST /v1/otp/request`,`/verify` · `POST /v1/organizers/register`,`/login` · `/.well-known/openid-configuration`, `/jwks.json` | — | — |
| **queue** | `QueueSettings` (+ Redis waiting room) | `POST .../queue/join` · `GET .../queue/status` · `GET/PUT .../queue/settings` | — | `EventPublished` |
| **venue** | `Venue`, `VenueGate`, `VenueFacility`, `SeatMap`, `SeatMapVersion`, `VenueSection`, `SeatRow`, `Seat`, `AdmissionArea`, `SeatMapElement` | `POST/GET/PUT /v1/venues[/{id}]` · `POST .../activate`,`.../archive`,`.../gates`,`.../facilities` · `POST/GET .../seat-maps` · `GET /v1/seat-maps/{id}[?version=]` · `POST .../versions` · `PUT .../draft/layout` · `POST .../publish` | `VenueCreated`, `SeatMapPublished` | — |
| **communication** | `DeliveryLogEntry`, `ProcessedNotificationEvent` | *internal:* `POST /v1/notifications/send` | — *(the only service with no outbox)* | `OrderConfirmed`, `TicketIssued`, `OrderTicketsIssued` |
| **media** | *no database* — Azure Blob Storage | `POST /v1/media/images` | — | — |

**Not gateway-routed** (internal or provider-only, unreachable from a browser):
all of Payments except the Stripe webhook, Communication entirely, Inventory's
`convert`/`release`/`extend`/`cancel`, Ticketing's `void`, Identity's discovery
and JWKS documents.

---

## 7. Cross-cutting mechanisms

**Authentication.** Identity issues RS256 JWTs and publishes an OIDC discovery
document plus JWKS. Every other service validates through ASP.NET Core's
standard `Authority`-based path — no custom key handling anywhere. A **buyer**
token carries `sub` and `role: buyer` and deliberately **no** `tenant_id`; an
**organizer** token carries `tenant_id` because organizers genuinely are
tenant-scoped.

**Tenancy.** Organizer actions take the tenant from the caller's token claim.
Buyer actions derive it from the *resource* being acted on — the event's
inventory settings, the hold being checked out — because a buyer's token has no
tenant at all (ADR-0022).

**Idempotency.** Checkout is deduped on `(user_id, idempotency_key)` with a
unique index plus a pre-check; concurrent duplicates lose at the index and
resolve to the winner rather than erroring. Payments dedupes on
`(order_id, idempotency_key)` and passes the same key to Stripe.

**Outbox.** Any service that publishes writes events into an outbox table inside
the same transaction as the state change. `OutboxRelay` polls and publishes.
Communication is the sole exception — it never publishes.

**Observability.** OpenTelemetry across every service; traces span the whole
checkout path, including the Dapr sidecars, so a pub/sub hop is a span rather
than a gap. Services export OTLP to whatever `OTEL_EXPORTER_OTLP_ENDPOINT`
names and know nothing else about the backend: Jaeger locally at `:16686`, an
OpenTelemetry Collector in AKS forwarding to Application Insights (ADR-0031).
Container logs and node/pod metrics reach the same Log Analytics workspace via
Container Insights, and both share one daily ingestion cap — see
`infra/README.md` before load testing.

(`docs/07-observability.md` is the pre-implementation design sketch and names
tools — Kafka, Loki, Tempo — that the build never adopted. It is kept as
written, like the rest of the `0x-` series; this file is the as-built record.)

---

## 8. Where things run

**Locally** — `scripts/dev-up.sh` brings up Postgres, Redis, Jaeger and Azurite
in Docker, applies every schema (`scripts/db-migrate.sh`), then starts all nine
services plus the gateway — eight under Dapr; Media and the gateway run without a
sidecar, neither needing pub/sub or service invocation.

| Port | Service | | Port | Service |
|---|---|---|---|---|
| 5080 | catalog | | 5085 | communication |
| 5081 | inventory | | 5086 | media |
| 5082 | ordering | | 5087 | identity |
| 5083 | payments | | 5088 | queue |
| 5084 | ticketing | | 5090 | **gateway** |

Frontend on `:5173` (Vite). Full walkthrough:
[local-e2e-walkthrough.md](local-e2e-walkthrough.md).

**In Azure** — Terraform in [infra/](../infra/) provisions AKS, Postgres,
Redis, Key Vault, ACR and blob storage. The dev topology deliberately diverges
from the production ADRs to stay cheap (one Postgres server with many
databases, Redis instead of Service Bus, single node pool) — recorded in
ADR-0017 so it is a decision, not a shortcut. GitHub Actions builds and pushes;
Argo CD reconciles [deploy/](../deploy/).

---

## 9. Known gaps

Honest list; these are decisions or debts, not surprises.

| Gap | Status |
|---|---|
| Test **depth** is uneven across services | Closed as an existence gap — all nine services have `tests/`. Inventory's no-oversell path is now exercised against a real Redis (Testcontainers); Catalog and Ticketing are thinner. Still no end-to-end saga test against a real Dapr sidecar |
| No end-to-end verification of the deployed topology | Terraform, manifests and the SPA image are validated only by `terraform validate` + `kustomize build` in CI, which prove they are well-formed, not that they work. **Argo CD sync phase/wave ordering is invisible to both** — the first real deploy found the migrate Jobs deadlocked as `PreSync` hooks waiting on a SecretProviderClass the main sync had not created (ADR-0029, Consequences). Nothing short of a real sync against a real cluster catches that class of defect |
| Ticket QR payload is the raw opaque token, not signed or expiring | Tracked; fine while tokens are 128-bit random and single-use |
| Queue admission tokens are not one-time-use | Holds are independently capacity- and limit-checked, so the queue only paces access |
| Bundle purchase across multiple tour legs | Deferred — needs multi-event orders with their own pricing |
| Ticket archive/purge for finished events | Designed (P3 in the tracker), not built |
| `EventUpdated`, `PaymentRefunded`, and the `Seat*` events have no consumers | Published for future use |
| No end-to-end saga test against a real Dapr sidecar | Covered by unit tests + an orchestrator purity scan instead |

---

## Documentation map

| If you want… | Read |
|---|---|
| Why a decision was made | [ADRs](adr/) — 28 records, the authority on intent |
| To work inside one service | that service's `CLAUDE.md` |
| To run it locally | [local-development.md](local-development.md), [local-e2e-walkthrough.md](local-e2e-walkthrough.md) |
| What's built vs deferred | [progress-tracker.md](progress-tracker.md) |
| The original product thinking | [00-vision-and-scope.md](00-vision-and-scope.md) … [09-delivery-roadmap.md](09-delivery-roadmap.md) |
| Per-feature narrative flows | [feature-flows/](feature-flows/) |
| Pre-implementation design | [design/](design/) — HLD, DFD, LLD (**frozen**, kept for provenance) |
| To add a service | [onboarding-new-service.md](onboarding-new-service.md) |
| The coding rules | [engineering-guidelines.md](engineering-guidelines.md), root `CLAUDE.md` |
