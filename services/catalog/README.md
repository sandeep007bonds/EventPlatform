# Catalog service

Owns events, venues, seat maps, ticket types and pricing; publishes seated events
and their generated inventory. See the [LLD](../../docs/design/lld-phase1-seated.md)
and issue #6.

## Structure

```
Catalog.Api/             # Minimal API host + endpoints (uses EventPlatform.Hosting defaults)
Catalog.Application/     # Vertical slices (Features/), abstractions, pipeline behaviors
Catalog.Domain/          # Event aggregate + invariants
Catalog.Infrastructure/  # EF Core + PostgreSQL persistence
```

## Endpoints

| Method | Path | Notes |
|--------|------|-------|
| POST | `/v1/events` | Create a draft event (tenant from JWT) |
| GET | `/v1/events/{id}` | Fetch an event |
| GET | `/health/live` · `/health/ready` | Health (from ServiceDefaults) |
| GET | `/openapi/v1.json` · `/scalar/v1` | OpenAPI doc + Scalar UI (non-prod) |

## Run

```bash
# from repo root: docker compose up -d   (Postgres/Redis/Jaeger)
dotnet run --project services/catalog/Catalog.Api
```

Dev uses EF Core `EnsureCreated` to build the schema on startup (migrations: tracker T8).
