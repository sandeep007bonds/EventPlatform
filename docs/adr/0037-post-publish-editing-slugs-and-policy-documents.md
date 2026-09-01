# ADR-0037 — Editing is split by consequence, not by status; events get slugs; policies are versioned

**Status:** Accepted · **Date:** 2026-09-01

## Context

Three problems, one decision, because they all live in the same place: what an `Event` carries and
who may change it.

**1. Almost nothing was editable after publish, and some things never were.**
`Event.UpdateDetails` refused every edit once an event left `Draft`, on the reasoning that changing
details after publish would need buyers re-notified. That is right for dates, venue and money and
wrong for the other fourteen fields — an organizer could not fix a typo in a description or swap a
banner image while tickets were on sale.

Worse, three things were **not in `UpdateDetails` at all**, so they were immutable from creation
even in `Draft`: `Title`, `StartsAt`, and the entire venue block. A misspelled event title was
permanent.

**2. No slug.** Every public URL was a GUID, so an event page could not be shared or indexed
sensibly, and a printed link was unusable.

**3. No terms, privacy or refund policy anywhere.** For a platform taking money that is not a
content gap, it is a compliance one — and a refund dispute needs to know *which version* the buyer
agreed to, which nothing recorded.

## Decision

### Split editing by consequence

`Event.UpdateDetails` becomes two operations with different rules:

| `UpdateSchedule` — Draft-only | `UpdatePresentation` — any status |
|---|---|
| `StartsAt` *(new — was impossible)*, `EndsAt`, `DoorsOpenAt`, `OnSaleAt`, `BookingEndsAt` | `Title` *(new)*, `Description`, `Category`, `AgeRestriction` |
| venue name, address, city, region, postcode, country, lat/long *(new)* | `BannerImageUrl`, `VideoUrl` |
| `TaxRatePercent`, `TaxLabel`, `BookingFeePerTicketMinor`, `TimeZoneId` | `ContactPhone`, `ContactMobile`, `ContactEmail`, `WebsiteUrl`, social links |
| `MaxTicketsPerBuyer`, `RequiresQueue` | |

Money, dates, venue and ticket rules stay locked after publish, because changing any of them alters
what a ticket holder bought. Everything else is presentation and moves freely.

`Title` is deliberately in the editable column. Renaming a live event is mildly jarring; never
being able to correct your own typo is worse, and the audit shadow fields (ADR-0036) now record who
changed it and when.

Two endpoints — `PUT /v1/events/{id}/details` (Draft-only) and `PUT /v1/events/{id}/presentation`
(any status) — rather than one with a mode flag, so the 409 is visible in the route table.

**Explicitly not in scope:** postponing or relocating a *published* event. That is a real
requirement and it is not an edit — it needs buyer notification and probably a refund right. It
gets its own flow.

### Slug

`Event.Slug` — lowercase `[a-z0-9-]`, globally unique, derived from the title at creation with a
numeric suffix on collision, editable while `Draft`, **locked after publish** because the URL has
already been advertised. Reserved words (`admin`, `api`, `login`, `checkout`, `events`, …) refused.
`/events/{slug}` resolves alongside the existing id route rather than replacing it, so links issued
before this change keep working.

Uniqueness is platform-wide, not per tenant: the slug is the whole of a public URL, and two tenants
cannot both own `/events/coldplay-mumbai`. The check in `CreateEventHandler` is read-then-write and
therefore racy; the unique index is the actual guard, and the loser of a race gets a constraint
violation rather than a duplicate URL.

### Policy documents

A `PolicyDocument` aggregate in **Catalog**, not Identity. Catalog already owns the commercial and
presentational content an event carries; putting it in Identity would mean Catalog reading another
service's data to render a public page.

`TenantId` · `EventId?` · `Kind` (`Terms` | `Privacy` | `Refund`) · `BodyHtml` · `Version` ·
`UpdatedAt`. A row with `EventId = null` is the organizer's default; one with an `EventId`
overrides it for that event — the same defaults-and-override shape `EventGroup` already uses for
tour contact details (ADR-0020).

Two things make this enterprise rather than a text field:

- **Versioned.** `Version` increments on every revision. Ordering captures the versions in force at
  checkout, so a refund dispute months later can answer "what did they agree to" instead of
  guessing from whatever the current text says. Saving an unchanged body is a no-op, so an
  organizer opening the editor and pressing Save does not invalidate the version orders point at.
- **Sanitised on write, not on render.** Stored HTML is an XSS vector aimed at every future reader.
  `HtmlSanitizer` (Ganss.Xss, MIT — licence checked per golden rule 4), narrowed to structure,
  emphasis and `http`/`https`/`mailto` links; no images, no iframes, no inline styles, so a policy
  page cannot become a tracking beacon that fires on every buyer who opens it. Doing it on write
  means the database is the thing you can inspect to prove a `<script>` did not survive.

**No new service for this.** One aggregate, three fields, low write volume; a service per aggregate
is the anti-pattern, and Ordering already holds a Catalog client for pricing, so capturing versions
at checkout costs nothing extra. It becomes a real `Content` context later *if* it grows to own
galleries, per-event email templates and multi-language.

## Consequences

- `Event.Create` takes a slug; `CreateEventHandler` derives one, so `IEventRepository` gains
  `GetBySlugAsync` and `ListSlugsForStemAsync`.
- The `events.Slug` column is `NOT NULL UNIQUE`, so its migration must backfill existing rows
  before the index is created — see the note in `services/catalog/CLAUDE.md`.
- The organizer console's event page becomes tabbed, because the sections now have genuinely
  different lifecycles and each saves independently.
- The policy editor ships as a textarea with a live preview, not a WYSIWYG: `frontend/CLAUDE.md`
  forbids a UI library beyond Ant Design without discussion, and Ant has no rich-text editor.
  Storing HTML means that upgrade is a component swap, not a data migration.

## Alternatives considered

**One endpoint with a `mode` flag.** Rejected — the authorization and the 409 stop being visible at
the route, and every future reader has to open the handler to learn which fields lock when.

**Slug per tenant rather than platform-wide.** Would allow two organizers to hold the same slug,
which means the URL needs a tenant segment to disambiguate. That is a different (and worse) public
URL scheme, chosen for the convenience of a uniqueness check.

**Policy documents as plain text rather than HTML.** Safer by construction, and unusable: a terms
document without headings, lists or links is not a terms document. Sanitising on write gets most of
the safety without the cost.
