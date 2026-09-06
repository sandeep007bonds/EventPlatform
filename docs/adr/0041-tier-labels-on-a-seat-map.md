# ADR-0041 — A seat map may say how a block is usually sold, but never what it costs

**Status:** Accepted · **Date:** 2026-09-05

## Context

ADR-0038 took price out of Venue and it was right to. A seat is a fact about a building and changes
roughly never; a price is a commercial decision that changes weekly. Stamping prices onto tens of
thousands of seat rows meant a reprice had to rewrite them all, and any that were missed lied.

What that left behind is repetition nobody had measured. `TicketType` is per **event**;
`SessionAllocation` — which block sells as which type — is per **performance**. Neither carries
forward from anything. A promoter running fifty shows at one stadium creates the same ticket types
fifty times and re-maps the same twenty blocks a hundred and fifty times, entering the identical
answer every time, by hand, with a 409 waiting for whichever block they miss.

The information being re-entered is not new each time. Every one of those events says "Lower Tier is
the expensive one, the pit is general admission" — because that is how the building is habitually
carved up. Only the *amounts* differ.

## Decision

**A seat-map block may carry an optional `TierLabel` — a name, never an amount.**

`VenueSection` and `AdmissionArea` gain a nullable `TierLabel` (`Lower Tier`, `VIP`, `GA`), settable
in the seat-map editor and carried through `ToLayout`/`ReplaceLayout` so it survives a re-version.
An empty string is stored as `null`: "no usual tier" and "a tier named nothing" must stay
distinguishable.

**Nothing on the server reads it to decide anything.** It is not an input to publish validation, to
pricing, to inventory provisioning, or to any authorization check. `SessionPublishCheck` still
demands that every block resolve to a real active `TicketType`, which is the actual guarantee, and
that is unchanged.

Its only consumers are in the SPA, and all three are conveniences over data already loaded:

- creating an event's ticket types from the distinct labels on its venue's map — with **no price**,
  which the organizer then sets
- pre-selecting each block's ticket type by matching label to type name
- defaulting a performance's allocation from another performance already mapped against the same map

Every one of those is editable afterwards. A label is a suggestion; the allocation is the decision.

### Why this does not reopen ADR-0038

The line ADR-0038 drew is between **facts about a building** and **decisions about a sale**. A
section's `Name` is already on the wrong side of no line — Venue stores "Lower Tier" today. A tier
label is the same kind of claim at a coarser grain: it says which blocks are commercially alike,
which is a property of the architecture, not of any event.

What stays out remains out: no amount, no currency, no discount, no sales window, no per-buyer cap,
no notion of what is or is not on sale tonight. If a future change wants `TierPrice` on a block, that
is ADR-0038's rejected design and needs its own ADR arguing against the reprice-rewrites-everything
problem, not an extension of this one.

## Consequences

- **One Venue migration**, two nullable `varchar(100)` columns. Existing maps get `null`, which means
  exactly what it should: this venue has no usual answer.
- **Events are unaffected unless they want to be.** No existing flow changes; an organizer who
  ignores labels sees the behaviour they see today.
- **The label can drift from reality.** A venue re-tiered without re-labelling will pre-fill the
  wrong suggestion. That is tolerable precisely because nothing downstream trusts it — the organizer
  sees the pre-filled mapping before saving, and the server validates the result regardless.
- **A tempting next step is now visible and should be resisted**: letting the label carry a *default*
  price "just for convenience". That is a price in Venue with extra steps, and the reprice problem
  returns with it.

## Alternatives considered

**Tenant-global ticket types with per-event price overrides.** Define "Gold/Silver/Bronze" once for
the organizer. Rejected: a `TicketType` carries its own sales window and per-buyer limit, which are
genuinely per-event, so this would share only a name while complicating the model — and the
expensive part is the *mapping*, not the naming.

**A "copy allocations from another event" action.** Solves the repetition without touching Venue, but
requires the organizer to remember which previous event to copy, and breaks the moment the venue
publishes a new map version with different block codes. The label travels with the map, so it stays
correct across events that have nothing to do with each other.

**Do nothing and let the editor default sensibly.** Copying the previous performance's allocation
handles a multi-night run and is worth doing anyway (it is), but it does nothing for the first
performance of the fiftieth event at the same stadium, which is the case that actually hurts.
