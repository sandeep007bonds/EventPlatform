# CLAUDE.md — Media service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md),
**except** the per-service Clean Architecture layering — see "Structure" below.

## Responsibility

Owns image upload and blob storage for organizer-supplied media (event
banners today; potentially other tenant-supplied assets later). Bounded
context: **Media** — deliberately separate from Catalog so blob-storage
concerns don't leak into it; Catalog only ever stores the URL string this
service's upload endpoint returns. See [ADR-0018](../../docs/adr/0018-media-service-and-blob-storage.md).

## Owns

- **Data store:** none — Azure Blob Storage (Azurite locally), not
  PostgreSQL. No transactional writes to coordinate, so this service does
  not use `EventPlatform.Messaging`'s outbox and has no reference to
  `EventPlatform.Contracts`.
- **Public API:** `POST /v1/media/images` (auth required — 401 without a
  tenant) — multipart image upload, returns the blob's public URL.
  **No download/GET endpoint.** The container is public-read
  (`container_access_type = "blob"` in Terraform); the browser fetches an
  uploaded image's URL directly from storage, never through this service.
- **Events published/consumed:** none.

## Structure

**Deliberately flat — this is a conscious, scoped exception, not an
oversight.** One project, `Media.Api`: `Program.cs`, `Endpoints/MediaEndpoints.cs`.
No `Media.Domain`/`Media.Application`/`Media.Infrastructure` split, no
MediatR/CQRS — there's exactly one operation (upload) with no business
invariants beyond content-type/size validation, so the usual per-service
layering would add ceremony without buying anything. `tests/Media.Tests`
still exists (root golden rule #8) — integration tests against a real
Azurite container via `WebApplicationFactory<Program>`.

**Do not** add Domain/Application/Infrastructure layers to this service
without a real reason to. If it grows real business logic (moderation
workflows, multiple asset types with different rules, etc.), that's the
signal to reconsider — not a default to design toward now.

## Local run

```bash
docker compose up -d azurite   # one-time, or already running via full dev-up.sh
dotnet run --project services/media/Media.Api
# browse the API docs at /scalar/v1 (non-production)
```

## Do not

- Add Domain/Application/Infrastructure layers without a real reason (see
  "Structure" above).
- Add a download/proxy endpoint for "private" media without first deciding
  how gated access should work (SAS tokens? auth-checked proxy?) — nothing
  here needs it yet.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
