# ADR-0038 — A Venue service, and seat maps that are versioned assets rather than event fields

**Status:** Accepted · **Date:** 2026-09-02

## Context

A venue was not modelled. `Catalog.Domain.EventLocation` is eight strings and two coordinates set
inline on each `Event` at creation, and `Catalog.Domain.SeatMap` hangs off an event id one-to-one.
The consequences compound:

1. **Nothing is reusable.** Two shows at the same stadium share no address, no gates and no seating
   layout. The organizer retypes the address and rebuilds the map — rows × seats-per-row, per
   section — every single time. For a venue that hosts forty events a year this is forty
   opportunities to disagree with itself.
2. **Section is a string copied onto every seat.** As a string it can own nothing: no gate, no
   display order, no code that survives a rename, and nowhere to hang a shape. Every section
   operation is a string comparison across seat rows, and a rename is a bulk update that can
   half-fail.
3. **Rows are not modelled at all**, only a label repeated per seat. Nothing prevents two rows in
   one section both calling themselves `F`, and row ordering cannot be stored.
4. **Price lives on the seat.** `Seat.PriceTier`/`PriceAmount` were superseded by `TicketTypeId`
   (ADR-0034 and the ticket-type work) precisely because a reprice would otherwise have to rewrite
   tens of thousands of seat rows, and any it missed would lie. The columns survive but nothing
   reads them.
5. **There is no geometry.** A seat has no coordinates, so a seat map cannot be drawn — it can only
   be listed. Every venue is implicitly a grid of rectangles, which excludes the irregular ones
   (beach clubs, terraces, in-the-round) rather than merely rendering them badly.
6. **A map is frozen by publish and can never change again.** A venue that reconfigures has no way
   to express it, because there is no version to change *to*.

The reference architecture ("v9", see [docs/v9-alignment.md](../v9-alignment.md)) makes this its
Phase 2 blocker: 8 VEN tickets and 17 MAP tickets, all of which need a venue to exist first.

## Decision

### 1. Venue is its own service

`services/venue`, its own PostgreSQL database, database-per-service as everywhere else. It owns
`Venue`, `VenueGate`, `VenueFacility`, `SeatMap`, `SeatMapVersion`, `VenueSection`, `SeatRow`,
`Seat`, `AdmissionArea` and `SeatMapElement`.

It is a service rather than a Catalog aggregate because the data has a different owner, a different
lifecycle and a different access pattern: a venue is edited by a venue manager and read by every
event; an event is edited by a promoter and read by buyers. v9 separates them for the same reason
and we agree here — this is one of the four splits we accepted from its 17-service topology.

**The line: this service owns physical facts only.** Where a place is, how you get in, what it
offers, which seats exist and where they are drawn. What is on sale, at what price, on which night
stays in Catalog. Keeping that line sharp is what lets one venue serve a hundred events without any
of them being able to change it for the others.

### 2. Logical identity is separate from graphical layout

```
SeatMapVersion ─┬─ VenueSection ── SeatRow ── Seat   logical: what a ticket can name
                ├─ AdmissionArea                     logical: capacity with no seat identity
                └─ SeatMapElement                    graphical: coordinates, shape, style
```

Moving a block on the plan must not change what a ticket refers to, and renumbering a row must not
require redrawing anything. `Seat` therefore has no coordinates and `SeatMapElement` has no
identity a ticket can name.

Elements are polygons and paths, not a grid of boxes. A stadium tier, a theatre balcony and a beach
club's shoreline terrace are the same problem at different curvatures; a rectangle-only model does
not render the third badly, it excludes it.

Coordinates are in abstract map space rather than pixels or metres, so one stored plan renders on a
phone and on a wall display without the numbers meaning anything different.

### 3. Section and row become entities; admission areas are not sections

`VenueSection` carries a stable `Code` (safe for another service to store, survives a rename), a
display order and an optional gate. `SeatRow` carries a label unique within its section and an
order. `Seat` carries a **string** number — real venues number seats `12A` and `B2`, and an integer
column quietly makes those unrepresentable — plus accessibility attributes as flags and an
`IsSellable` flag for permanently dead space.

Unreserved capacity is an `AdmissionArea`, not a section full of invented seats. Fake seats cannot
be chosen, cannot be drawn, and cannot be scanned to a place.

### 4. A seat has no price and no availability

Price belongs to the ticket product in Catalog, one row per product. Availability belongs to
Inventory and differs per session while the seat does not. This finishes the migration ADR-0034
started: the `PriceTier`/`PriceAmount` columns on Catalog's `Seat` have no counterpart here and are
not carried forward.

### 5. Seat maps are versioned, and a published version is immutable

A `SeatMap` is a named configuration ("end stage", "in the round") with a series of numbered
versions. Exactly one may be a draft; publishing freezes it and supersedes the previous live one,
which is **kept, not deleted**, because tickets sold against it still have to resolve.

A structural change starts a new draft **pre-filled with the published layout** — nobody redraws a
stadium to move one block — with fresh ids, so editing the copy cannot reach back into the version
tickets were sold under.

### 6. Layouts are replaced wholesale, not patched

`PUT /v1/seat-maps/{id}/draft/layout` takes the entire layout. A graphical editor already holds the
whole plan and knows nothing about which of a hundred nudges the server has seen; a patch protocol
would mean inventing an operation vocabulary, ordering it, and reconciling conflicts — for a draft
only one person edits at a time. Wholesale replacement cannot half-apply.

A draft is allowed to be **incomplete**: saving half a stadium and coming back is normal work, so
save-time validation rejects only what cannot be stored at all (an element naming a section the
layout does not contain; a gate that is not this venue's). Everything else waits for publish.

### 7. Publish validation returns every problem, as a list

A stadium plan can fail thirty ways at once, and an editor that reveals them one refresh apart is
unusable. `SeatMapVersion.Validate()` returns `SeatMapValidationError[]`; the API returns them as a
409 body. The aggregate's own `Publish` still throws if called with an invalid layout — defence in
depth against a caller that forgot to validate, not the primary path.

One rule is worth calling out: **a map that draws some sections but not others is rejected**, while
a map that draws none is fine. A small hall needs no plan; a half-drawn plan leaves the buyer unable
to tell a hole from a sold-out block.

**Gate references are validated in the application layer, not the aggregate.** A gate belongs to the
venue; a seat-map aggregate that reached across to check one would be reading another aggregate's
state to decide its own. They are checked twice — at save, because the person who typed the wrong
gate is still on the screen, and again at publish, because a gate can be deactivated in between.

## Consequences

**Catalog's `SeatMap` and `EventLocation` are now the legacy path.** They keep working and nothing
about existing events changes in this ADR. Migrating events onto venue-owned maps is the next
decision, and it is entangled with `EventSession` (v9's EVENT-005) — inventory, tickets and reports
should hang off a session, not an event, and moving them is one change, not two. That work gets its
own ADR.

**The projects are named `Venues.*` while the service, database, schema, Dapr app-id and gateway
prefix are all `venue`.** A class called `Venue` inside a namespace called `Venue.Domain` is a
name-resolution trap: the identifier binds to the type and `Venue.Domain.X` stops compiling. Plural
project names cost nothing and remove the whole class of problem.

**Repositories load versions in a second query rather than a filtered `Include`.** A filtered
include forces every `ThenInclude` branch to repeat the identical filter and EF throws if two of
them drift apart. Both loaders are therefore tracked, since relationship fixup only happens for
tracked entities.

**Progressive loading is not implemented yet.** v9 wants venue overview → section geometry →
selected-section seats for large stadiums; today `GET /v1/seat-maps/{id}` returns the whole version.
That is fine at hall scale and will not be at stadium scale. The model no longer stands in the way —
`SeatMapElement` is exactly the overview layer — so it is a read-endpoint change when it is needed,
not a remodelling.

**No migration is checked in.** The .NET SDK is not available in the environment this was authored
in, so `dotnet ef migrations add InitialCreate` has to be run before first use. The command is in
the service's CLAUDE.md.

## Closes

VEN-001 · VEN-002 · VEN-003 · VEN-004 · VEN-005 (as `AdmissionArea`) · VEN-006 · VEN-007 · VEN-008 ·
MAP-002 · MAP-005 · MAP-006 · MAP-008 · MAP-009 · MAP-011 · MAP-012 (as `IsSellable`) · MAP-013 ·
MAP-016 · MAP-017. The editor itself (MAP-001, 003, 004, 007, 010, 014, 015) is UI work on top of
this model.
