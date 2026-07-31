# Media service

Image upload + Azure Blob Storage for organizer-supplied event media
(banners today). Deliberately lightweight — see [CLAUDE.md](CLAUDE.md) for
why this service skips the usual per-service layering, and
[ADR-0018](../../docs/adr/0018-media-service-and-blob-storage.md) for the
full design decision.

## What it is

One endpoint: `POST /v1/media/images` — upload an image, get back its
public URL. No download endpoint; the returned URL is fetched directly from
blob storage. Catalog (or any other caller) stores that URL string verbatim
— this service has no awareness of what consumes the URLs it hands out.

## Local run

```bash
docker compose up -d azurite
dotnet run --project services/media/Media.Api
```

Needs Azurite (Azure Storage's local emulator) running — `docker compose up -d`
from the repo root starts it alongside Postgres/Redis/Jaeger. No real Azure
account or storage account needed for local development.

## Tests

```bash
dotnet test services/media/tests/Media.Tests
```

Integration tests spin up a real Azurite container (Testcontainers) and the
app in-process (`WebApplicationFactory`), proving an upload actually produces
a fetchable blob URL end to end.
