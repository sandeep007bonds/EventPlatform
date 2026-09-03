# CLAUDE.md — Catalog service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns **what is being sold**: events, their performances, tours, ticket types, promo codes, tax and
fees, and the policy documents a buyer agrees to. Bounded context: **Catalog** (ADR-0008).

Two lines to hold:

- **Catalog does not own places.** Venues, gates and seat maps belong to the Venue service
  (ADR-0038). A performance *names* a Venue seat-map version; it never copies one.
- **Catalog does not own availability.** Whether a seat is free is Inventory's answer, and it
  differs per performance while the seat does not.

## The model

```
Event                         what is being sold, and how it is marketed
 ├─ title, slug, description, media, contact, social, category
 ├─ currency, tax, booking fee, MaxTicketsPerBuyer, RequiresQueue, OnSaleAt
 ├─ TicketType[]  PromoCode[]  PolicyDocument[]
 └─ EventSession[]             one performance
      ├─ Name?, StartsAt, EndsAt, DoorsOpenAt?, BookingEndsAt?, Status, SalesPaused
      ├─ VenueId + SeatMapId + SeatMapVersionId   → the Venue service, pinned by version
      ├─ VenueSnapshot (name, city, country, tz)  → a display cache, never decided from
      └─ SessionAllocation[] { Code, TicketTypeId }
```

**`EventSession` is the grain everything downstream keys on** (ADR-0039). Inventory provisions per
performance, orders and tickets name one, a scan is validated against one. A three-night run is one
event with three performances — not three events.

**`SessionAllocation` is where price meets place.** A Venue seat carries no price, so something has
to say "Lower Tier is Gold", and it has to say it per performance: Friday's Lower Tier can be Gold
while Saturday's matinee sells the same seats as Premium. It binds a Venue **section or
admission-area code** to a `TicketTypeId` — one row per block, about twenty for a stadium, not one
per seat.

**Naming:** the type is `EventSession`; every route, parameter and DTO field says `eventSessionId`,
never bare `sessionId` — Queue already owns that word for waiting-room sessions. UI copy says
"performance".

**A tour leg is not a performance.** A leg is a different city, venue and seat map, separately
advertised — that is `EventGroup`. Sessions are several nights of the *same* event.

### What sits at which level, and why

| | Level | Why |
|---|---|---|
| `StartsAt` / `EndsAt` / `DoorsOpenAt` | Session | A performance has a time; an event has a run. |
| `BookingEndsAt` | Session | "Book until 2h before **this** show" is a different instant every night. |
| `SalesPaused` | Session | Pull one night without pulling the run. The event-wide switch fans out. |
| Venue + seat map | Session | Two nights can use different configurations of the hall. |
| `OnSaleAt` | Event | A run goes on sale once, at one advertised moment. |
| `RequiresQueue` | Event | One waiting room gates the on-sale, so Queue stays event-keyed. |
| `MaxTicketsPerBuyer` | Event | A per-night limit would let one buyer take the cap three times over. |

## Owns

- **Data store:** PostgreSQL `catalog` DB (this service only).
- **Public API:**
  - `/v1/events` — `GET` list, `POST` create (with its first performance), `GET /{id}`,
    `GET /by-slug/{slug}`, `POST /{id}/publish`, `POST /{id}/pause-sales`, `POST /{id}/resume-sales`,
    `PUT /{id}/selling-rules`, `PUT /{id}/presentation`, `PUT /{id}/slug`.
  - `/v1/events/{eventId}/sessions` — `GET` list (anonymous), `POST` add, `PUT /{eventSessionId}`,
    `DELETE /{eventSessionId}`, `PUT /{eventSessionId}/seat-map`,
    `PUT /{eventSessionId}/allocations`, `POST /{eventSessionId}/publish`,
    `POST /{eventSessionId}/cancel`, `POST /{eventSessionId}/pause-sales`, `.../resume-sales`.
  - `/v1/event-groups`, `/v1/events/{eventId}/ticket-types`, `/v1/events/{eventId}/promo-codes`,
    `/v1/policies`, `/v1/events/{eventId}/policies` — unchanged.
- **Reads are anonymous, writes are `RequireOrganizer()`.** Every write handler still checks the
  event belongs to the caller's tenant and answers a mismatch with an **opaque 404**, so an id probe
  cannot confirm what exists.
- **Events published:** `EventPublished` (event-level facts only — Queue is its one consumer),
  `EventSessionPublished` (one per performance — Inventory and Ticketing provision from it),
  `EventSessionCancelled`, `EventSalesPaused`/`EventSalesResumed` (both now per performance),
  `EventUpdated`.
- **Events consumed:** —

## Publishing

`PublishEvent` is **all-or-nothing across performances**. Each one is checked by
`SessionPublishCheck` (shared with `PublishEventSession`, so the two cannot drift):

1. It names a seat map, and that map still exists in Venue.
2. The pinned version is **published**, and is still the map's published one.
3. **Every block in the version has an allocation.** An unallocated section is not spare capacity —
   it is capacity Inventory never hears about, so the map renders with a hole nobody can distinguish
   from a sold-out block.
4. Every allocated ticket type is this event's and still active.

A failure lists **every** problem, not the first: an organizer fixing a three-night run needs all
three at once. Publishing partially would take an event live with one advertised night silently
unbuyable, which is worse than not publishing.

A performance added to an event that is already selling publishes on its own
(`POST .../sessions/{id}/publish`) — that is the late-show path.

## Service-specific rules

- **`IVenueClient` is the only outbound call**, over Dapr to app-id `venue`, and only on two cold
  paths: attaching a seat map, and validating a publish. Nothing a buyer does reaches it. The
  venue's *name* is fetched best-effort and degrades to a placeholder — failing an attach because a
  display string could not be read would block real work over a cosmetic field.
- **`Event.FirstSessionStartsAt`/`LastSessionEndsAt` are denormalised**, maintained by the aggregate
  whenever a session is added, moved or removed. The storefront lists by date and filters to
  upcoming; computing this in memory would turn one indexed scan into loading every session of every
  event. Never set them from outside the aggregate.
- **`SessionCommandResult` is shared by all nine session commands.** They answer the same three
  questions — found it, was it allowed, what does it look like now — and nine near-identical outcome
  enums would only be nine chances to return the wrong status code.
- **The event's cached date range and the tour rules go together.** A leg is compared to its
  siblings on its whole run (first performance to last), because that is what the tour advertises.
- **Deleted in ADR-0039:** `SeatMap`, `Seat`, `GeneralAdmissionSection`, `EntryGate`,
  `EventLocation`, `TicketTypeResolver` and every seat-map slice. Do not reintroduce them — a seat
  map that lives here cannot be shared between events, which is the whole reason Venue exists.

## Migrations

No migration is checked in — the model changed shape completely in ADR-0039 and the old ones
described tables that no longer exist. Generate a fresh one against an empty database:

```bash
./scripts/db-add-migration.sh InitialCreate catalog
```

## Local run

```bash
dotnet run --project services/catalog/Catalog.Api
# browse the API docs at /scalar/v1 (non-production)
```

## Do not

- Model a venue, a gate or a seat here. That is the Venue service.
- Put availability here. That is Inventory, per performance.
- Read another service's database.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
