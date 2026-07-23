# Local Development

**No Azure account or cloud spend is required for local development.** Everything
runs on your machine with Docker. Azure is only used when we *deploy* (AKS). This
is a deliberate benefit of the Dapr + containers design: the same service code
runs locally and in the cloud — only the Dapr component config differs.

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

## 2. Run a service (standalone — no Dapr yet)

The Catalog service doesn't call Dapr yet, so you can just run it:

```bash
dotnet run --project services/catalog/Catalog.Api
```

Then open:
- **API docs (Scalar):** https://localhost:7080/scalar/v1
- **Sample endpoint:** https://localhost:7080/v1/events/00000000-0000-0000-0000-000000000000
- **Health:** https://localhost:7080/health/live and `/health/ready`
- **Traces:** http://localhost:16686 (pick the `catalog` service)

> First HTTPS run may prompt for the dev certificate — trust it once with
> `dotnet dev-certs https --trust`.

## 3. Run a service *with* Dapr (once services use it)

When a service starts using pub/sub or workflow (Order, Inventory), run it with a
Dapr sidecar that loads our local components:

```bash
dapr init            # one-time; installs the Dapr runtime (uses Docker)

dapr run \
  --app-id catalog \
  --resources-path platform/dapr/components \
  --config platform/dapr/config.yaml \
  -- dotnet run --project services/catalog/Catalog.Api
```

The local Dapr components in `platform/dapr/components/` point at the Docker Redis
(`localhost:6380`). In Azure, the **same-named** components point at Service Bus /
Azure Cache — so nothing in the service code changes.

## 4. Secrets (local)

The local Dapr secret store reads `platform/dapr/secrets.local.json` (git-ignored).
Create it from the example:

```bash
cp platform/dapr/secrets.local.example.json platform/dapr/secrets.local.json
```

Put only **local dummy** values there. Real secrets live in Key Vault (cloud), never in the repo.

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
