# ADR-0042 — A performance arranges the venue it hires; the venue does not know it happened

**Status:** Accepted · **Date:** 2026-09-08

## Context

ADR-0038 gave places their own service and ADR-0039 made the performance the selling grain. Between
them they left one thing unsayable, and walking a real onboarding flow found it: **a promoter hiring
a hall does not sell the hall as the hall is.**

Four things happen routinely, and the model expressed exactly one of them:

1. Only part of the venue goes on sale — lower bowl only, upper tier closed for a small show.
2. A block is advertised under a different name — the north stand sold as "Golden Circle".
3. Less is sold than a block holds — 200 tickets in a pit that fits 400.
4. Seating is added that the venue's layout does not have — floor seating for this show only.

`SessionAllocation` was `(blockCode → ticketTypeId)`, and `SessionPublishCheck` demanded that
**every** block in the pinned version be allocated. So "half the venue" was unpublishable, a block
was always called whatever the venue called it, and the only per-performance decision available was
the price.

Putting any of this in Venue was never an option. A venue is reusable and an event is not — that
separation is the whole reason the service exists — so "the upper tier is closed" cannot be a fact
about a building that fifty other events also hire.

## Decision

**`SessionAllocation` becomes the overlay a performance arranges its hired venue with.** It stays
per performance, stays keyed by the block's stable `Code`, and gains three fields:

| Field | |
|---|---|
| `IsExcluded` | not on sale this performance |
| `DisplayName` | what buyers see it called; `null` uses the venue's name |
| `CapacityOverride` | how many to sell from an **admission area**; `null` sells all of it |

`TicketTypeId` becomes **nullable**, and the aggregate enforces priced-**xor**-excluded: a block is
sold as something, or it is closed, never both and never neither. A closed block has no price to
name, and asking an organizer to price a block they just shut is a question with no right answer.

**Publish changes from "every block allocated" to "every block allocated *or excluded*, and at least
one allocated."** A block nobody decided about is still refused — that is capacity Inventory never
hears about, and a hole in the map the buyer cannot tell from a sold-out section. Excluding it says
the same thing deliberately, and that difference is the entire point of the field.

This covers needs 1–3. **Need 4 is not addressed here** and needs its own decision, because
event-owned *geometry* is a second origin for seat identity and touches Inventory, Ticketing and the
buyer's map.

### Capacity is two problems, not one

For an admission area, capacity is a number, so "sell 200 of 400" is a complete instruction and
`CapacityOverride` is honest. For a reserved section it is not: seats have identity, so "sell 200 of
400" never says *which* 200, and nothing downstream could pick them. Holding reserved seats back is
**seat blocking**, which Inventory already does per performance
(`POST /v1/sessions/{eventSessionId}/inventory/block`). The handler refuses a cap on a section
rather than clamping or reinterpreting it — for the same reason an `AdmissionArea` is not a section
full of invented seats.

### The capacity number that was quietly wrong

`SessionPublishReadiness` reported `version.Capacity` — the **whole version's** — and that flows
straight out on `EventSessionPublished.Capacity`. The moment a block can be excluded or capped, that
is the building's number and not the night's. It is now summed over the version's blocks, taking
`capacityOverride ?? physical` for each block still on sale, and skipping the excluded ones. Driving
the loop from the *version* rather than from the allocation rows also means an allocation left
behind by an older layout contributes nothing, instead of adding capacity no part of the building
backs.

### Renaming survives time for free

Because the overlay lives on the performance and freezes when the performance publishes — exactly as
the pinned seat-map version does — a ticket sold into "Golden Circle" still reads that when next
year's show calls the same block something else. The `Code` never changes with the display name;
allocations, inventory, tickets and scanning all bind to the code, so a rename stays a display
decision rather than becoming a data migration.

### Why this does not reopen ADR-0038

ADR-0038 draws the line between **facts about a building** and **decisions about a sale**. Every
field added here is on the sale side and none of them reaches Venue: the venue still says the pit
holds 400 and the block is called the North Stand, and it says so identically to the next fifty
events. What changed is only that the performance may now say what it does with that.

## Consequences

- **One Catalog migration**: `TicketTypeId` becomes nullable, plus `IsExcluded` (not null, default
  false), `DisplayName` (`varchar(100)`) and `CapacityOverride` (`int`). Existing rows are unaffected
  — every one of them is a priced, unexcluded, unrenamed, uncapped block, which is what the defaults
  say.
- **Inventory needed almost nothing.** It already skipped a seat or area whose code has no
  allocation, so an excluded block — simply absent from the payload — provisions nothing. The one
  real change is `allocation.CapacityOverride ?? area.Capacity`. Its *comment* had to change though:
  "reaching this means the two services disagree" is now the ordinary excluded case, and leaving it
  would send the next reader hunting a bug that is not there.
- **Ticket and scan display still show the venue's word.** Those read the block name from Venue and
  know nothing of the overlay. Carrying `DisplayName` onto the ticket is a follow-on, deliberately
  not done here — the buyer's seat map already fetches both the layout and the session, so it
  overlays the name with no new plumbing, and that is where the name matters most.
- **A fully-excluded performance is refused**, in the aggregate and in the publish check. It is a
  thing an organizer can type and never a thing they can sell, and the SPA now counts blocks *on
  sale* rather than allocation rows so it does not offer a Publish button the server will reject.

## Alternatives considered

**Let an unallocated block simply mean "not on sale".** Free to implement, and wrong: it makes an
omission and a decision indistinguishable, so a block the organizer forgot and a block they closed
look identical — to the server, to the next organizer, and in the audit trail. The whole value of
the publish check is that it catches the first.

**Model exclusion in Venue, as a per-event layout variant.** Would let two events share "the
half-house configuration". Rejected: it puts an event's commercial decision in the building's
library, and it multiplies versions — a venue with four sellable subsets and three real layouts
becomes twelve versions, most of which describe no building that exists.

**Overlay per event rather than per performance.** Simpler to enter once for a three-night run. But
the whole reason `SessionAllocation` is per performance is that Friday and Saturday genuinely differ,
and a promoter who closes the upper tier for the matinee only is the ordinary case, not the exotic
one. The repetition is answered where it belongs — the editor offers a sibling performance's whole
arrangement, exclusions and renames included, as a suggestion.

**`CapacityOverride` on reserved sections too, meaning "sell any 200".** Rejected above: it names no
seats, and seat blocking already answers it precisely.
