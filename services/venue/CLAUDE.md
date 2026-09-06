# CLAUDE.md — Venue service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns **places**: venues, their gates and facilities, and the versioned seat maps that describe how
each place can be laid out. Bounded context: **Venue** (ADR-0038).

A venue is reusable and an event is not. Before this service, a location was eight fields typed onto
each event and a seating layout rebuilt from scratch every time, so two shows at the same stadium
shared nothing — not the address, not the gates, not the map. Here the venue is defined once and its
seat maps are versioned assets an event points at.

The line to hold: this service owns **physical** facts only. What is on sale, at what price, on
which night is Catalog's business. That is why nothing here has a price on it, and why nothing here
knows whether a seat is free.

## Owns

- **Data store:** PostgreSQL `venue` DB (this service only), schema `venue`.
- **Public API:** REST `/v1/venues`, `/v1/venues/{id}/seat-maps`, `/v1/seat-maps/{id}`.
  Everything is `RequireOrganizer()` **except** `GET /v1/seat-maps/{id}`, which is
  `.AllowAnonymous()` — a buyer has to render the plan to choose a seat, and a ticket sold under an
  older version has to keep resolving. That endpoint still refuses a **draft** to anyone but the
  tenant that owns it. Venue reads are organizer-only on purpose: which buildings an organizer is
  measuring up, and when, says more about an unannounced tour than the tour does.
- **Events published:** `VenueCreated`, `SeatMapPublished`.
- **Events consumed:** —

## The model, and why it is shaped this way

```
Venue ─┬─ VenueGate         physical entry points
       ├─ VenueFacility     what the place offers
       └─ SeatMap ── SeatMapVersion ─┬─ VenueSection ── SeatRow ── Seat   (logical)
                                     ├─ AdmissionArea                     (logical, no seats)
                                     └─ SeatMapElement                    (graphical)
```

- **Section is an entity, not a string on every seat.** As a string it could own nothing: no gate,
  no display order, no code stable across a rename, and no place to hang the shape that draws it.
  Every "section" operation was a string comparison across seat rows, and a rename was a bulk update
  that could half-fail.
- **`Seat` has no price and no availability.** A seat is a fact about a building and changes roughly
  never; a price is a commercial decision that changes weekly. Stamping prices onto tens of
  thousands of seat rows meant a reprice had to rewrite them all, and any that were missed lied.
  Price belongs to the ticket product in Catalog; availability is Inventory's, and differs per
  session while the seat does not.
- **`AdmissionArea` is not a section full of invented seats.** Capacity without seat identity. Fake
  seats cannot be chosen, drawn, or scanned to a place.
- **Graphics live only in `SeatMapElement`.** Moving a block on the plan must not change what a
  ticket refers to, and renumbering a row must not require redrawing anything. Shapes are polygons
  and paths, not a grid of boxes, because a stadium tier, a theatre balcony and a beach club's
  shoreline terrace are the same problem at different curvatures.
- **Coordinates are abstract map space**, not pixels or metres. The client fits the extent to
  whatever viewport it has.

## Versioning rules (the part to get right)

- A map always has **at most one open draft**. Two drafts of the same map would both be right and
  only one could win.
- `StartNewDraft()` **copies the published layout** into the new version — nobody redraws a stadium
  to move one block — with fresh ids, so editing the copy cannot reach back into the version tickets
  were sold against.
- **Publishing freezes the version.** After that the layout is immutable and a structural change
  needs a new version. This is the whole reason versions exist.
- `PublishDraft` captures the previously-published version **before** publishing the draft. Publish
  first and "the published version" momentarily matches two rows, and `Published` (a
  `SingleOrDefault`) throws. There is a test for exactly this.
- **A failed publish leaves the live version live.** Taking a venue's map offline because someone
  tried an edit that did not validate is worse than the edit not landing.
- Superseded versions are **kept, never deleted** — tickets sold against them still have to resolve.

## Publish validation

`SeatMapVersion.Validate()` returns **every** problem as a list, not the first as an exception: a
stadium plan can fail thirty ways at once, and an editor that reveals them one refresh apart is
unusable. The API returns them as a 409 body.

Rules: duplicate section/area code · duplicate row label in a section · duplicate seat number in a
row · section with no rows · row with no seats · admission area with no capacity · a layout that
sells nothing · a polygon or path with no points · a rectangle or ellipse with no bounds · **a map
that draws some sections but not others** (a map with no plan at all is fine — a small hall needs
none; a half-drawn one leaves the buyer unable to tell a hole from a sold-out block).

**Gate references are checked in the application layer, not the aggregate.** A gate belongs to the
venue, and an aggregate that reached across to validate one would be reading another aggregate's
state to decide its own. They are checked twice — at save, because the person who typed the wrong
gate is still looking at the screen, and again at publish, because a gate can be deactivated in
between.

## Service-specific rules

- **The projects are `Venues.*`, the folder and service name are `venue`.** A class called `Venue`
  inside a namespace called `Venue.Domain` is a name-resolution trap — the identifier binds to the
  type and `Venue.Domain.X` stops compiling. Plural project names cost nothing and remove the whole
  class of problem. The Dapr app-id, the database, the schema and the gateway prefix all stay
  singular `venue`.
- **Layouts are replaced wholesale, never patched.** A graphical editor already holds the entire
  plan and knows nothing about which of a hundred nudges the server has seen; a patch protocol would
  mean inventing an operation vocabulary, ordering it, and reconciling conflicts, for a draft only
  one person edits at a time. `PUT /draft/layout` cannot half-apply. For a large stadium the body is
  genuinely large, and that is the honest cost.
- **A draft may be incomplete.** Saving half a stadium and coming back is normal, so save-time
  validation rejects only what cannot be stored at all (a dangling element reference, an unknown
  gate). Everything else waits for publish.
- **Repositories load versions in a second query, not a filtered `Include`.** A filtered include
  forces every `ThenInclude` branch to repeat the identical filter, and EF throws if two of them
  drift. Both loaders are therefore **tracked** — fixup only happens for tracked entities, so
  `AsNoTracking` would silently return a seat map with no versions on it.
- **`GetWithVersionAsync` with no version number returns the published version if there is one,
  otherwise the open draft.** Published-only reads as the safe default and is not: a map has no
  published version until someone publishes it, so every newly created map answered "not found" to
  its own owner and the editor could never open one. The draft is not leaked by loading it —
  `GetSeatMapHandler` refuses a draft to anyone but the owning tenant, and that check was
  unreachable until this was fixed.
- `GetTrackedByIdAsync` deliberately **skips superseded versions**. They are immutable and nothing
  can touch them, and skipping them is safe for numbering too: a superseded version is by definition
  older than the published one, so the highest number is always still in view.

## Migrations

```bash
./scripts/db-add-migration.sh <Name> venue
```

On an empty database there is nothing else to do — `./scripts/db-migrate.sh venue` (or `dev-up.sh`)
applies it. The script points `--startup-project` at `Venues.Infrastructure`, not the API host: the
design-time factory builds the context standalone, so the tooling never starts Dapr or the outbox
relay just to diff a model.

## Local run

```bash
dotnet run --project services/venue/Venues.Api
# browse the API docs at /scalar/v1 (non-production) — http://localhost:5089
```

## Do not

- Put a price, a discount, or anything about what is on sale into this service. A block's
  optional `TierLabel` is the one adjacent thing allowed, and it is a **name only** — it says
  which blocks are commercially alike, never what they cost, and nothing on the server reads it
  to decide anything (ADR-0041). `TierPrice` is not a follow-up; it is ADR-0038's rejected design.
- Put availability here — Inventory owns whether a seat is free, per session.
- Edit a published version. Start a new one.
- Read another service's database.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
