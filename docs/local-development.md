# Local Development

**No Azure account or cloud spend is required for local development.** Everything
runs on your machine with Docker. Azure is only used when we *deploy* (AKS). This
is a deliberate benefit of the Dapr + containers design: the same service code
runs locally and in the cloud — only the Dapr component config differs.

## Quick start (one command)

```bash
./scripts/dev-up.sh
```

Starts Postgres, Redis, Jaeger, and all six services (each with a Dapr
sidecar). Ctrl+C stops everything. See
**[local-e2e-walkthrough.md](local-e2e-walkthrough.md)** for the full runbook —
prerequisites, minting a dev auth token, and a copy-paste script that drives a
complete purchase (create event → seat map → publish → hold → checkout → order
→ ticket) — plus how to prove no-oversell, idempotent checkout, and the
self-healing Redis fast gate.

The rest of this page is background on how the pieces underneath that script
fit together.

## Prerequisites

| Tool | Why |
|------|-----|
| **Docker Desktop** | Runs Postgres, Redis, Jaeger (and later, more) |
| **.NET 10 SDK** | Build & run the services — https://dotnet.microsoft.com/download/dotnet/10.0 |
| **Dapr CLI (>= 1.13)** | Runs each service with a sidecar (pub/sub, workflow, service invocation) — https://docs.dapr.io/getting-started/install-dapr-cli/ |

Optional: Visual Studio 2022 (17.14+ for `.slnx`), Rider, or VS Code + C# Dev Kit.

## Backing services

`docker compose up -d` (what `dev-up.sh` calls first) starts:

| Service | Address | Notes |
|---------|---------|-------|
| PostgreSQL | `localhost:5432` | user `eventplatform` / pw `localdev` / db `eventplatform` |
| Redis | `localhost:6380` | 6379 is left free for Dapr's own runtime |
| Jaeger UI | http://localhost:16686 | traces from every service (OTLP on 4317) |
| Azurite | `localhost:10000` (blob only) | Media.Api's local Azure Blob Storage emulator — `ConnectionStrings:media-storage` is `UseDevelopmentStorage=true`, the SDK's built-in shorthand for this |

Stop with `./scripts/dev-down.sh` (add `-v` to also wipe the Postgres volume).

## Database schema — automatic in Development

Nothing to run. Each service has its own Postgres **database** (`catalog`,
`inventory`, `ordering`, `payments`, `ticketing`, `communication`, `identity`
— true database-per-service)
and creates it from its current EF Core model the first time it starts in
Development (`Database.EnsureCreatedAsync()` in `Program.cs`) — no `dotnet ef`
command, no `Migrations/` folder to generate or commit. Each service must have
its own database name for this to work: `EnsureCreatedAsync()` only creates
tables when the target database doesn't exist yet, so two services sharing one
database would silently end up with only the first one's tables.

If you change a domain model and the columns look stale, the fastest local fix
is to drop and recreate the disposable Postgres volume:

```bash
./scripts/dev-down.sh -v && ./scripts/dev-up.sh
```

> **Why not EF Core migrations here?** `EnsureCreated` can't evolve an existing
> schema without data loss, so it's the wrong tool once we need staging/production
> deployments that preserve data. That's real EF Core migrations
> (`dotnet ef migrations add`, `Database.Migrate()`), tracked separately as
> cloud-deployment work (see `docs/progress-tracker.md`) — each service already
> has a design-time factory (e.g. `CatalogDbContextDesignTimeFactory`) ready for
> when that tooling is wired up. For local dev, `EnsureCreated` needs zero
> commands from you.

## How the services run with Dapr

`scripts/dev-up.sh` runs `dapr run -f platform/dapr/dapr.yaml` — a
[Dapr multi-app run](https://docs.dapr.io/developing-applications/local-development/multi-app-dapr-run/)
template that starts all six services with their sidecars in one process
tree. It's equivalent to running, for each service, its own:

```bash
dapr run --app-id catalog --app-port 5080 --app-protocol http \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/catalog/Catalog.Api
```

`--app-port`/`--app-protocol http` are what let Dapr deliver pub/sub events and
route service-to-service (service invocation) calls by app-id. The local Dapr
components in `platform/dapr/components/` point at the Docker Redis
(`localhost:6380`) for **both** pub/sub and the Dapr Workflow state store. In
Azure, the **same-named** components point at Service Bus / Azure Cache — so
nothing in the service code changes.

> **Without a sidecar:** you can still `dotnet run` an API directly, but the
> outbox relay will log a publish error every couple of seconds (messages just
> stay pending in the `outbox` table until a sidecar is present), and Dapr
> service invocation / pub/sub / workflow calls fail. Always use `dev-up.sh`
> (or `dapr run`) for anything beyond compiling a single service.

### How the outbox flows

1. A command handler calls `IEventPublisher.Enqueue(...)`; the event is written to
   the `outbox` table **in the same transaction** as the state change (no dual-write).
2. `OutboxRelay` (a background service) polls the table and publishes pending rows
   to Dapr pub/sub (`pubsub` component), stamping the outbox id as the CloudEvent
   id so consumers can dedupe. Delivery is **at-least-once**.

## Secrets (local)

The local Dapr secret store reads `platform/dapr/secrets.local.json`
(git-ignored). `dev-up.sh` creates it from the example automatically if it's
missing; to do it by hand:

```bash
cp platform/dapr/secrets.local.example.json platform/dapr/secrets.local.json
```

Put only **local dummy** values there. Real secrets live in Key Vault (cloud), never in the repo.

### Stripe key (Payments)

The Payments service uses the real Stripe gateway when `Payments:Stripe:SecretKey`
is configured; otherwise it uses the dev simulator (`dev-up.sh` works fine
either way). **Never commit the key.** For local dev, use user-secrets (stored
outside the repo), set *before* running `dev-up.sh`:

```bash
dotnet user-secrets set "Payments:Stripe:SecretKey" "sk_test_..." \
  --project services/payments/Payments.Api
```

In the cloud the key comes from **Key Vault** (surfaced as the same config key). Use
a **test** key locally, and roll any key that has ever been shared.

### Twilio credentials (Communication SMS/WhatsApp)

Communication's SMS/WhatsApp senders use Twilio when `Communication:Sms:Provider`
(or `Communication:WhatsApp:Provider`) is set to `Twilio`; otherwise they use the
dev/logging sender (`dev-up.sh` works fine either way — nothing sends for real).
**Never commit these values.** For local dev, use user-secrets, set *before*
running `dev-up.sh`:

```bash
dotnet user-secrets set "Communication:Sms:Provider" "Twilio" \
  --project services/communication/Communication.Api
dotnet user-secrets set "Communication:Twilio:AccountSid" "AC..." \
  --project services/communication/Communication.Api
dotnet user-secrets set "Communication:Twilio:AuthToken" "..." \
  --project services/communication/Communication.Api
dotnet user-secrets set "Communication:Twilio:SmsFromNumber" "+1..." \
  --project services/communication/Communication.Api
```

In the cloud these come from **Key Vault** (surfaced as the same config keys).
Use a **trial/test** account locally, and roll any credential that has ever
been shared or pasted somewhere outside a secrets manager.

## What is NOT needed locally

- ❌ An Azure subscription
- ❌ AKS / Kubernetes (services run as plain processes locally)
- ❌ Azure Service Bus / Event Hubs (Dapr uses local Redis)
- ❌ A real identity provider (Development accepts a locally-signed dev JWT — see
  the walkthrough's token-minting step; production uses real OIDC)

## Ports summary

| Port | Service |
|------|---------|
| 5432 | PostgreSQL |
| 6380 | Redis (app) |
| 16686 | Jaeger UI |
| 4317 / 4318 | OTLP ingest (traces) |
| 5080 | Catalog.Api |
| 5081 | Inventory.Api |
| 5082 | Ordering.Api |
| 5083 | Payments.Api |
| 5084 | Ticketing.Api |
| 5085 | Communication.Api |
| 5086 | Media.Api |
| 5087 | Identity.Api |
| 10000 | Azurite (blob) |

`dev-up.sh` runs every service over plain HTTP on these ports (no dev TLS cert
needed) — see [local-e2e-walkthrough.md](local-e2e-walkthrough.md) for why.
