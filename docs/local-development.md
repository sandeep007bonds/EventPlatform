# Local Development

**No Azure account or cloud spend is required for local development.** Everything
runs on your machine with Docker. Azure is only used when we *deploy* (AKS). This
is a deliberate benefit of the Dapr + containers design: the same service code
runs locally and in the cloud — only the Dapr component config differs.

> **Want the full purchase flow across all five services?** See the copy-paste
> runbook in [local-e2e-walkthrough.md](local-e2e-walkthrough.md): create event →
> seat map → publish → provision inventory → hold → checkout (pay) → order
> confirmed → ticket issued, plus how to mint a dev JWT and prove no-oversell.

## Prerequisites

| Tool | Why |
|------|-----|
| **Docker Desktop** | Runs Postgres, Redis, Jaeger (and later, more) |
| **.NET 10 SDK** | Build & run the services — https://dotnet.microsoft.com/download/dotnet/10.0 |
| **Dapr CLI** | Needed once services use Dapr (pub/sub, workflow) — https://docs.dapr.io/getting-started/install-dapr-cli/ |

Optional: Visual Studio 2022 (17.14+ for `.slnx`), Rider, or VS Code + C# Dev Kit.

## 1. Start the backing services

From the repo root:

```bash
docker compose up -d
```

This starts:

| Service | Address | Notes |
|---------|---------|-------|
| PostgreSQL | `localhost:5432` | user `eventplatform` / pw `localdev` / db `eventplatform` |
| Redis | `localhost:6380` | 6379 is left free for Dapr's own runtime |
| Jaeger UI | http://localhost:16686 | traces from every service (OTLP on 4317) |

Stop with `docker compose down` (add `-v` to also wipe the Postgres volume).

## 2. Database migrations (EF Core)

The schema is owned by **EF Core migrations** (not `EnsureCreated`). In dev the
Catalog host applies any pending migrations on startup, so normally you don't run
anything by hand. You only touch the tooling when the model changes.

One-time tool install:

```bash
dotnet tool install --global dotnet-ef
```

Generate the initial migration (run once, then commit the generated
`Migrations/` folder — it is part of the source):

```bash
dotnet ef migrations add InitialCreate \
  --project services/catalog/Catalog.Infrastructure \
  --startup-project services/catalog/Catalog.Api
```

After that, each model change is a new migration (`dotnet ef migrations add <Name>`
with the same `--project`/`--startup-project`). To apply migrations to a running
DB without launching the API, use `dotnet ef database update` (same arguments);
override the target with the `CATALOG_DB` connection-string env var.

> A design-time factory (`CatalogDbContextDesignTimeFactory`) lets the tooling
> build the context without starting the API host (Dapr, the outbox relay, …).

## 3. Run a service *with* Dapr

Catalog now publishes `EventPublished` through the transactional outbox, and the
outbox relay publishes to Dapr pub/sub — so run it with a Dapr sidecar that loads
our local components:

```bash
dapr init            # one-time; installs the Dapr runtime (uses Docker)

dapr run \
  --app-id catalog \
  --resources-path platform/dapr/components \
  --config platform/dapr/config.yaml \
  -- dotnet run --project services/catalog/Catalog.Api
```

Then open:
- **API docs (Scalar):** https://localhost:7080/scalar/v1
- **Sample endpoint:** https://localhost:7080/v1/events/00000000-0000-0000-0000-000000000000
- **Health:** https://localhost:7080/health/live and `/health/ready`
- **Traces:** http://localhost:16686 (pick the `catalog` service)

> First HTTPS run may prompt for the dev certificate — trust it once with
> `dotnet dev-certs https --trust`.

> **Without a sidecar:** you can still `dotnet run` the API directly, but the
> outbox relay will log a publish error every couple of seconds (messages just
> stay pending in the `outbox` table until a sidecar is present). Use the
> `dapr run` command above for the full path.

The local Dapr components in `platform/dapr/components/` point at the Docker Redis
(`localhost:6380`). In Azure, the **same-named** components point at Service Bus /
Azure Cache — so nothing in the service code changes.

### How the outbox flows

1. A command handler calls `IEventPublisher.Enqueue(...)`; the event is written to
   the `outbox` table **in the same transaction** as the state change (no dual-write).
2. `OutboxRelay` (a background service) polls the table and publishes pending rows
   to Dapr pub/sub (`pubsub` component), stamping the outbox id as the CloudEvent
   id so consumers can dedupe. Delivery is **at-least-once**.

## 4. Secrets (local)

The local Dapr secret store reads `platform/dapr/secrets.local.json` (git-ignored).
Create it from the example:

```bash
cp platform/dapr/secrets.local.example.json platform/dapr/secrets.local.json
```

Put only **local dummy** values there. Real secrets live in Key Vault (cloud), never in the repo.

### Stripe key (Payments)

The Payments service uses the real Stripe gateway when `Payments:Stripe:SecretKey`
is configured; otherwise it uses the dev simulator. **Never commit the key.** For
local dev, use user-secrets (stored outside the repo):

```bash
dotnet user-secrets set "Payments:Stripe:SecretKey" "sk_test_..." \
  --project services/payments/Payments.Api
```

In the cloud the key comes from **Key Vault** (surfaced as the same config key). Use
a **test** key locally, and roll any key that has ever been shared.

## What is NOT needed locally

- ❌ An Azure subscription
- ❌ AKS / Kubernetes (services run as plain processes locally)
- ❌ Azure Service Bus / Event Hubs (Dapr uses local Redis)
- ❌ Entra / a real identity provider (dev endpoints are anonymous; add Keycloak in Docker later if you want real tokens)

## Ports summary

| Port | Service |
|------|---------|
| 5432 | PostgreSQL |
| 6380 | Redis (app) |
| 16686 | Jaeger UI |
| 4317 / 4318 | OTLP ingest (traces) |
| 5080 / 7080 | Catalog.Api (http / https) |
| 5081 / 7081 | Inventory.Api (http / https) |
| 5082 / 7082 | Ordering.Api (http / https) |
| 5083 / 7083 | Payments.Api (http / https) |
| 5084 / 7084 | Ticketing.Api (http / https) |

## End-to-end: Catalog → Inventory

To see inventory provisioned from a published event, run **both** services with
their own Dapr sidecars (each in its own terminal), then create → seat-map →
publish an event in Catalog:

```bash
# terminal 1
dapr run --app-id catalog --resources-path platform/dapr/components \
  --config platform/dapr/config.yaml -- dotnet run --project services/catalog/Catalog.Api

# terminal 2
dapr run --app-id inventory --resources-path platform/dapr/components \
  --config platform/dapr/config.yaml -- dotnet run --project services/inventory/Inventory.Api
```

On publish, Catalog's outbox relay emits `EventPublished`; Inventory receives it
over pub/sub, pulls the seat map from Catalog (Dapr service invocation), and
generates one inventory item per seat. Verify with
`GET https://localhost:7081/v1/events/{eventId}/inventory`.
