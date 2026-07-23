# Catalog service

Owns events, venues, seat maps, ticket types and pricing; publishes seated events
and their generated inventory. See the [LLD](../../docs/design/lld-phase1-seated.md)
and issue #6.

## Status

**Skeleton.** `src/Catalog.Api` boots with the shared `EventPlatform.Hosting`
defaults (auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks) and exposes a
placeholder `GET /v1/events/{id}`. The real slices, domain, persistence and events
land during Phase 1.

## Endpoints

| Method | Path | Notes |
|--------|------|-------|
| GET | `/v1/events/{id}` | Placeholder (anonymous) |
| GET | `/health/live` | Liveness (from ServiceDefaults) |
| GET | `/health/ready` | Readiness (from ServiceDefaults) |
| GET | `/openapi/v1.json` | OpenAPI document |
| GET | `/scalar/v1` | API reference UI (non-production) |

## Run

```bash
dotnet run --project src/Catalog.Api
```
