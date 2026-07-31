# ADR-0018 — Media service and Azure Blob Storage for event media

- **Status:** Accepted
- **Date:** 2026-07-31

## Context

Catalog's `Event` had no way to hold a description, a venue with any real
attributes (only an opaque `VenueId` GUID), a banner image, or a video —
despite `docs/01-requirements.md` already stating organizers set "venue,
date/time, description, media, and categories," and `docs/04-data-model.md`'s
ER sketch already including a `venue` entity with address/geo and an `event`
with `description`. None of that was ever built. The public event page
(`GET /v1/events/{id}`, already anonymous-readable) was consequently too
thin to function as anything promotional.

Closing this gap needs somewhere to store uploaded image files — this repo
had zero blob/media-storage infrastructure anywhere (no `Azure.Storage.Blobs`
reference, no app-facing storage account; the only storage account in
`infra/` backs Terraform's own remote state).

## Decision

**A new, separate `services/media/` service**, not blob-storage logic bolted
onto Catalog. Media/asset storage is a distinct, reusable bounded context —
not owned by Catalog specifically, and could serve other needs later (e.g.
organizer logos) — and this matches the repo's existing "one service per
bounded context" pattern (root `CLAUDE.md`'s `services/` layout) better than
growing Catalog to also own a storage SDK dependency it has no other reason
to need. Catalog never references `Azure.Storage.Blobs`; it only ever stores
the plain URL string Media's upload endpoint returns, exactly as it stores a
plain video-embed URL.

- **Azure Blob Storage**, one storage account + one public-read container
  (`event-media`), provisioned by a new Terraform leaf module
  (`infra/modules/blob-storage/`). This is the first publicly-readable
  resource anywhere in `infra/` — Postgres, Redis, and Key Vault are all
  private/firewalled. Deliberate for public event-banner images, but a
  real security-posture change worth a second look in review, not
  something that should slip through unnoticed.
- **Server-proxied upload, no download/proxy endpoint.** `Media.Api` exposes
  one endpoint, `POST /v1/media/images`: validates content-type
  (png/jpeg/webp/gif) and an 8MB size cap, streams the file straight to
  blob storage, returns the blob's public URL. Because the container is
  public-read, the browser fetches that URL directly from storage — no
  proxy, no signed/expiring URL. Simpler to build and reason about than a
  SAS-token-issuing endpoint + direct client-to-storage upload (which would
  also need CORS configured on the storage account), and sufficient at
  event-banner-image scale.
- **Azurite for local dev.** `docker-compose.yml` gains an `azurite`
  service (blob port only); `Media.Api`'s Development connection string is
  the SDK's `UseDevelopmentStorage=true` shorthand. Keeps
  `docs/local-development.md`'s "no Azure account needed locally" promise
  intact.
- **Deliberately flat service shape — a conscious, scoped exception to the
  repo's usual per-service Clean Architecture split.** `Media.Api` is one
  project: `Program.cs` and `Endpoints/MediaEndpoints.cs`, no
  `Media.Domain`/`Media.Application`/`Media.Infrastructure`, no MediatR.
  There is exactly one operation with no business invariants beyond
  content-type/size validation — the usual layering would add ceremony
  without buying anything. This reflects explicit direction that this is a
  first pass, not the long-term shape; recorded here so a future reader
  doesn't "fix" it as an oversight. It still gets the shared
  `EventPlatform.Hosting` defaults, a `tests/Media.Tests` project
  (integration tests against a real Azurite container via
  `WebApplicationFactory`), a `CLAUDE.md`, and a README — those cost
  nothing extra given the existing shared infrastructure, so they weren't
  skipped even though the layering was.
- **Video stays an embed-URL field only** (`Event.VideoUrl`, a YouTube/Vimeo
  link), not an uploaded/hosted file. The original ask mentioned "images,
  videos, location" together, but native video hosting (upload, transcoding,
  adaptive-bitrate streaming, storage/egress cost) is a materially bigger,
  separate initiative than image blob storage — flagged explicitly rather
  than silently scoped in.
- **Location**: a real `Venue` aggregate in `Catalog.Domain` (its own table,
  `catalog` schema, FK from `Event.VenueId`) rather than flat address fields
  on `Event` or a separate service — `services/catalog/CLAUDE.md` already
  documented Catalog as owning "events, **venues**, seat maps, ticket types
  and pricing." Venues are tenant-owned, not shared across organizers, in
  this pass.
- **Social-share preview (Open Graph) / SSR remains deferred**, per
  [ADR-0015](0015-frontend-react-vite-antd-and-bff-gateway.md)'s existing
  note anticipating exactly this kind of richer public page and explicitly
  punting SEO/SSR to a future, separate initiative. The buyer event pages
  render this new content client-side only; link-preview bots that don't
  execute JS (Slack/Twitter/WhatsApp-style unfurlers) won't see it yet.

## Consequences

- Organizers can set a description, category, venue, banner image, video
  link, and scheduling metadata on a draft event, and the public
  `/events/:id` and `/` pages render all of it — closing the gap named in
  `docs/01-requirements.md` since the beginning of this project.
- `OnSaleAt`/`OffSaleAt` are **display-only** in this pass — nothing in
  `EventStatus`/`PublishEvent` enforces them yet. A reader could reasonably
  assume adding these fields means they're enforced; they aren't.
- No content moderation or virus scanning on uploads — any authenticated
  organizer can upload arbitrary image bytes to a public-read container.
  Accepted for this pass; revisit before this is anything but a personal/
  low-stakes deployment.
- No venue delete/dedupe/merge tooling, and no cross-tenant shared-venue
  directory — each organizer maintains their own venue list independently,
  even for the same physical building.
- `Media.Api` is the only service holding an Azure Storage SDK dependency;
  every other service stays free of it.

## Alternatives considered

- **Blob upload/download inside Catalog.Api** — rejected; couples a
  storage-technology dependency to a service that has no other reason to
  need one, and blurs Catalog's bounded context.
- **SAS-token-issuing endpoint + direct client-to-storage upload** —
  rejected for now as unnecessary complexity (storage-account CORS,
  token-expiry handling) at the current scale; a plausible future
  optimization if upload volume/size grows.
- **Private container + authenticated download-proxy endpoint** — rejected;
  nothing in scope today needs gated media, and public-read is the normal
  pattern for public event-promotion images. Flagged as a future extension
  point if private media becomes a real requirement.
- **Full Clean Architecture layering for Media** (matching every other
  service) — rejected for this pass given explicit "not a long-term
  solution" direction; revisit if the service grows real business logic
  (moderation workflows, multiple asset types with different rules).
- **Flat address fields directly on `Event` instead of a `Venue` aggregate**
  — rejected; venues are meant to be reused across multiple events by the
  same organizer, which flat per-event fields can't support, and
  `services/catalog/CLAUDE.md` already scoped venues into Catalog's
  bounded context.

## References

- `infra/modules/container-registry/` — the shallow leaf-module style
  `infra/modules/blob-storage/` follows.
- `infra/environments/dev/main.tf` — existing Key Vault secret pattern
  (`redis_connection_string`, `service_connection_strings`) that
  `media_storage_connection_string` reuses.
- `docs/onboarding-new-service.md` — the mechanical steps Media's
  deploy/CD/gateway wiring followed.
- `services/media/CLAUDE.md`, `services/media/README.md`.
