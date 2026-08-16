# Local Development

**No Azure account or cloud spend is required for local development.** Everything
runs on your machine with Docker. Azure is only used when we *deploy* (AKS). This
is a deliberate benefit of the Dapr + containers design: the same service code
runs locally and in the cloud — only the Dapr component config differs.

## Quick start (one command)

```bash
./scripts/dev-up.sh
```

Starts Postgres, Redis, Jaeger, and all nine services — eight with a Dapr
sidecar, plus Media.Api and the gateway, which run without one. Ctrl+C stops
everything. See
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

## Database schema — EF Core migrations

Each service has its own Postgres **database** (`catalog`, `inventory`,
`ordering`, `payments`, `ticketing`, `communication`, `identity`, `queue` — true
database-per-service) and its own migration history.

`dev-up.sh` applies migrations for you before starting anything, so day to day
there is nothing to run. When you change a model, generate a migration:

```bash
# one service (usual case — only the service you changed)
./scripts/db-add-migration.sh AddSeatGateColumn catalog

# or all eight
./scripts/db-add-migration.sh InitialCreate
```

Review the generated `Migrations/` files, then commit them alongside the model
change. To apply without a full `dev-up`:

```bash
./scripts/db-migrate.sh            # all eight
./scripts/db-migrate.sh catalog    # just one
```

To check that no model has moved on without a migration to match:

```bash
./scripts/db-check-drift.sh          # all eight
./scripts/db-check-drift.sh catalog  # just one
```

CI runs this on every push. It is the guard against the one thing the
model-as-source-of-truth approach cannot catch on its own — a model change
merged without its migration, which surfaces later as a deploy that fails, or
succeeds against a schema that no longer matches the code. A service with no
migrations committed yet is skipped rather than failed: "never migrated" and
"migrated, then drifted" are different states, and only the second is a bug.

All three need the EF tool once: `dotnet tool install --global dotnet-ef`.

> **Running `dotnet ef` by hand?** Point `--startup-project` at the
> **Infrastructure** project, not the Api:
>
> ```bash
> dotnet ef migrations add AddThing \
>   --project services/catalog/Catalog.Infrastructure \
>   --startup-project services/catalog/Catalog.Infrastructure
> ```
>
> Aiming it at the Api gives *"Your startup project 'Catalog.Api' doesn't
> reference Microsoft.EntityFrameworkCore.Design"* — and the fix is not to add
> that package to the Api projects. Each Infrastructure project has an
> `IDesignTimeDbContextFactory` that builds its context standalone, reading
> `<SERVICE>_DB` (e.g. `CATALOG_DB`) or falling back to the local dev connection
> string. That is exactly why those factories exist: the tools never start the
> host, so no Dapr sidecar or outbox relay spins up just to diff a model. The
> scripts above already do this.

> **Services never migrate themselves.** Applying the schema is an explicit
> step — the same image run with `--migrate` applies migrations and exits, which
> is exactly what a deployed environment runs as an Argo CD PreSync job. One
> mechanism, exercised locally every day rather than only at deploy time
> (ADR-0029). A service that migrated on startup would race itself across
> replicas and take the app down on a bad migration.

> **Coming from `EnsureCreated`?** Drop your local volumes once —
> `./scripts/dev-down.sh -v && ./scripts/dev-up.sh`. A database created by
> `EnsureCreated` has no migration history, so EF would try to create tables that
> already exist.

## How the services run with Dapr

`scripts/dev-up.sh` runs `dapr run -f platform/dapr/dapr.yaml` — a
[Dapr multi-app run](https://docs.dapr.io/developing-applications/local-development/multi-app-dapr-run/)
template that starts eight of the nine services with their sidecars in one
process tree. Media.Api is deliberately not in it — it has no database and no
pub/sub, so it never runs with a sidecar (see `services/media/CLAUDE.md`);
`dev-up.sh` starts it, and the gateway, as plain processes alongside. It's equivalent to running, for each service, its own:

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

To exercise the real card-collection UI (instead of the dev fallback that skips
rendering a card field entirely), also set the matching **publishable** key —
safe to expose client-side — in `frontend/.env.development.local`:

```
VITE_STRIPE_PUBLISHABLE_KEY=pk_test_...
```

Leaving it unset is fine: checkout falls back to a canned test payment method
and the buyer flow works exactly as before, with no Stripe setup required.

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
| 5088 | Queue.Api |
| 10000 | Azurite (blob) |

`dev-up.sh` runs every service over plain HTTP on these ports (no dev TLS cert
needed) — see [local-e2e-walkthrough.md](local-e2e-walkthrough.md) for why.
