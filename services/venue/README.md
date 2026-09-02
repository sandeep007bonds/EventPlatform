# Venue service

Owns places: venues, their gates and facilities, and the versioned seat maps
that describe how each place can be laid out.

A venue is reusable and an event is not. That one sentence is why this service
exists. Before it, a location was eight fields typed onto each event and a
seating layout rebuilt from scratch every time — so two shows at the same
stadium shared nothing: not the address, not the gates, not the map.

## The line it holds

This service owns **physical** facts only.

| Question | Answered by |
|---|---|
| Where is this place, how do you get in, what does it offer? | **Venue** |
| Which seats exist, in which rows, in which sections, and where are they drawn? | **Venue** |
| What is this seat sold as, and for how much? | Catalog (ticket products) |
| Is this seat free for Friday's show? | Inventory |
| May this ticket enter through this gate tonight? | Ticketing |

So a `Seat` here has **no price and no availability**. A seat is a fact about a
building and changes roughly never; a price changes weekly and per event, and
availability differs per performance. Stamping either onto tens of thousands of
seat rows means a change has to rewrite them all, and any row that is missed
lies.

## The model

```
Venue ─┬─ VenueGate         physical entry points
       ├─ VenueFacility     what the place offers
       └─ SeatMap ── SeatMapVersion ─┬─ VenueSection ── SeatRow ── Seat   logical
                                     ├─ AdmissionArea                     logical, no seats
                                     └─ SeatMapElement                    graphical
```

**Logical identity and graphical layout are separate.** Moving a block on the
plan must not change what a ticket refers to, and renumbering a row must not
require redrawing anything. Everything with coordinates is a `SeatMapElement`;
everything a ticket can name is not.

Shapes are polygons and paths, not a grid of boxes, because the venues are not
all rectangles. A stadium tier, a theatre balcony and a beach club's shoreline
terrace are the same problem at different curvatures, and a model that only
draws rectangles quietly excludes the third.

Coordinates are in abstract map space — not pixels, not metres. The client fits
the extent to whatever viewport it has, so a plan drawn once renders on a phone
and on a wall display.

## Versions

A published seat-map version is **immutable**, because seats are sold against
it. A venue reconfigures — a block removed for a stage extension, a row
renumbered, standing replacing seating — and if the map were edited in place,
every ticket already sold would silently start referring to a different place,
or to nowhere.

So: edit a draft, publish it, and the previous live version becomes
`Superseded` rather than being deleted. Tickets sold under it still resolve.

- At most **one open draft** at a time.
- A new draft **starts from the published layout**, with fresh ids.
- A **failed publish leaves the live version live**.

## Publish validation

`Validate()` returns every problem at once, not the first — a stadium plan can
fail thirty ways, and an editor that reveals them one refresh apart is
unusable.

Duplicate section/area code · duplicate row label · duplicate seat number ·
section with no rows · row with no seats · area with no capacity · a layout
that sells nothing · polygon with no points · box with no bounds · a map that
draws some sections but not others.

That last one is deliberate: a map with **no** plan is fine (a small hall needs
none), but a **half-drawn** one leaves a buyer unable to tell a hole from a
sold-out block.

Gate references are checked against the venue by the application layer — twice.
At save, because the person who typed the wrong gate is still looking at the
screen; and again at publish, because a gate can be deactivated in between.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/venues` | Create a venue |
| GET | `/v1/venues` | List the tenant's venues (`?includeArchived=true`) |
| GET | `/v1/venues/{venueId}` | One venue with its gates and facilities |
| PUT | `/v1/venues/{venueId}` | Correct name, type, address, time zone (any status) |
| POST | `/v1/venues/{venueId}/activate` | Make it selectable for new events |
| POST | `/v1/venues/{venueId}/archive` | Retire it (one-way) |
| POST | `/v1/venues/{venueId}/gates` | Add a physical entry point |
| POST | `/v1/venues/{venueId}/facilities` | Record something the venue offers |
| POST | `/v1/venues/{venueId}/seat-maps` | Add a seating configuration (opens draft v1) |
| GET | `/v1/venues/{venueId}/seat-maps` | List configurations, without layouts |
| GET | `/v1/seat-maps/{seatMapId}` | One version in full (`?version=N`, default: published) |
| POST | `/v1/seat-maps/{seatMapId}/versions` | Open a new draft from the published layout |
| PUT | `/v1/seat-maps/{seatMapId}/draft/layout` | Replace the draft's whole layout |
| POST | `/v1/seat-maps/{seatMapId}/publish` | Validate and freeze the draft |

Everything is organizer-only except `GET /v1/seat-maps/{seatMapId}`, which is
anonymous so a buyer can render the plan — and which still refuses a **draft**
to anyone but the tenant that owns it.

## Events published

| Event | Carries | For |
|---|---|---|
| `VenueCreated` | id, name, city, country | Search, Audit, Reporting |
| `SeatMapPublished` | map id, version id, version number, capacity | Event, Search, Audit |

`SeatMapPublished` carries the capacity but **not the layout**. A stadium plan
is megabytes and a message bus is the wrong place to move it; a consumer that
needs the seats reads them back by id.

## Local run

```bash
dotnet run --project services/venue/Venues.Api
# API docs at http://localhost:5089/scalar/v1 (non-production)
```

The initial EF migration is not checked in yet — see
[CLAUDE.md](CLAUDE.md#migrations) for the one command that generates it.
