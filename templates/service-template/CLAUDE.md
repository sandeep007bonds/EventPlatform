# CLAUDE.md — <ServiceName> service

Service-specific guidance. Inherits everything in the [root CLAUDE.md](../../CLAUDE.md)
and the [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

<!-- One paragraph: this service's bounded context and what it owns. -->

## Owns

- **Data store:** <!-- e.g., PostgreSQL `xyz` DB (this service ONLY) -->
- **Public API:** <!-- e.g., REST /v1/... -->
- **Events published:** <!-- e.g., `XCreated`, `XUpdated` -->
- **Events consumed:** <!-- e.g., `OrderConfirmed` -->

## Structure

- `X.Api` — Minimal API host, Dapr, DI, middleware
- `X.Application` — vertical slices under `Features/` (Command/Handler/Validator/Endpoint)
- `X.Domain` — entities, value objects, invariants, domain events
- `X.Infrastructure` — EF Core, outbox, Dapr adapters, external clients
- `X.Workflow` — Dapr workflow + activities (only if this service orchestrates a saga)
- `tests/` — unit + integration (Testcontainers)

## Local run

```bash
dotnet run --project src/X.Api
# or with Dapr sidecar:
dapr run --app-id x --resources-path ../../platform/dapr/components -- dotnet run --project src/X.Api
```

## Service-specific rules

<!-- Anything unusual, e.g., "Inventory hot path: no MediatR, hand-tuned data access." -->

## Do not

- Read another service's database.
- Add a package version to the `.csproj` (use `Directory.Packages.props`).
- Deploy by hand — change `deploy/` and let Argo CD reconcile.
