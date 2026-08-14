# ADR-0029 — EF Core migrations, PostgreSQL-targeted, with the provider seam collected in one place

**Status:** Accepted · **Date:** 2026-08-14

## Context

Every service created its schema with `Database.EnsureCreatedAsync()` on startup, gated to
Development. That was always explicitly temporary: `EnsureCreated` cannot evolve an existing
schema, so any model change meant dropping the local database — and it gives a deployed
environment nothing at all. Nothing could be shared or staged until this was replaced.

Two questions had to be answered together, because the second constrains the first:

1. What produces and applies the schema?
2. Do we need to support more than one database engine?

On (2) the requirement was stated as a hedge — PostgreSQL today, possibly MySQL or SQL Server
later — with no customer or contract requiring it.

### What was actually coupled to PostgreSQL

Measured, not assumed:

| Coupling | Where | Severity |
|---|---|---|
| `UseNpgsql(...)` | 20 call sites | Trivial — one line per service |
| `catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })` | `OrderRepository.TryAddAsync`, `PaymentRepository.TrySaveChangesAsync` | **Serious** |
| `HasFilter("\"IsActive\" = true")` partial index | `SigningKeyConfiguration` | Moderate |

The middle row is the one that matters. Those two catches are what turn a concurrent duplicate
checkout or charge into a `409` instead of a `500`. On any other engine the `catch` clause simply
never matches: the exception escapes, and the protection silently degrades. It would compile, pass
every existing test, and fail only under a production race — the same shape of bug as the
orchestrator stall in ADR-0028, where correct-looking code was wrong at runtime with nothing to
catch it.

The partial index fails more quietly still: MySQL has no filtered indexes, so the "only one active
signing key" constraint would not exist at all.

## Decision

### Target PostgreSQL. Do not build multi-engine support now.

The expensive part of multi-engine support is not migrations — it is **semantics**. This platform's
core guarantees (a seat never sold twice, a buyer never double-charged) rest on unique-index race
behaviour, optimistic concurrency and transaction isolation, and those differ subtly between
PostgreSQL, MySQL and SQL Server. Supporting three engines means proving no-oversell on three
engines, or shipping a guarantee verified on one and asserted for the rest.

It also costs capability. We already use partial indexes, and at scale we would want `jsonb`,
`INSERT … ON CONFLICT`, and `SELECT … FOR UPDATE SKIP LOCKED` for the outbox relays. Designing for
the lowest common denominator forfeits all of it in exchange for portability nobody has asked for.

ADR-0001 also locks Azure single-cloud, where Azure Database for PostgreSQL is a first-class
managed service. There is no infrastructure pressure toward another engine.

### Collect the provider seam instead of abstracting the provider

New `building-blocks/EventPlatform.Persistence` holds the knowledge of *which* engine we run on:

- `DbExceptions.IsUniqueViolation(this DbUpdateException)` — the two repositories now express
  intent, and the provider detail lives in one file.
- `MigrationRunner` — the `--migrate` entry point described below.

`UseNpgsql` stays inline in each service's `DependencyInjection`. Wrapping it behind a
provider-selection abstraction with exactly one provider would be the premature abstraction this
ADR exists to avoid — it is one grep-able line per service, and a port would have to revisit each
service's registration anyway.

### EF Core migrations, not a SQL project and not a separate migration tool

- **A SQL project (`.sqlproj` / SSDT / DACPAC) was rejected outright.** That toolchain is SQL Server
  only. Choosing it would have locked us to the one engine we do not use, permanently — the exact
  opposite of the stated goal.
- **FluentMigrator, DbUp, Liquibase and Flyway were rejected** on maintainability. The schema is
  already defined once, in EF's `IEntityTypeConfiguration` classes. Any of these tools would mean
  maintaining it twice — the EF model *and* hand-written DDL — and the two will drift. EF's
  `migrations add` diffs the model and writes the migration, so the model stays the single source of
  truth.
- If a second engine is ever genuinely required, EF's documented multi-provider migrations pattern
  keeps the model shared and adds a per-provider migrations assembly. That is a port, not a rewrite.

### Migrations are applied by an explicit step; services never migrate themselves

A service that migrates on boot races itself the moment it has more than one replica, and a failed
migration takes the application down instead of failing a job someone can inspect.

The same container image does both roles, selected by argument: run it normally and it serves
traffic and never touches the schema; run it with `--migrate` and it applies migrations, logs what
it applied, and exits. In Kubernetes that is an Argo CD PreSync job, so the schema lands before any
new pod rolls.

That job is `deploy/base/<service>/migrate-job.yaml`, one per database-owning service, annotated
`argocd.argoproj.io/hook: PreSync`. It runs the service's own image with `args: ["--migrate"]` and
carries the same environment its Deployment does, because the host is fully built before the
migration branch is reached. Two details are load-bearing and must survive future edits: the pod
template carries **no** `dapr.io/*` annotations — a Dapr sidecar never exits on its own, so the pod
would stay Running and the Job would never complete — and the container image keeps the same
`<service>-placeholder` name as the Deployment, so CI's `kustomize edit set image` rewrites both to
the same tag and a migration can never be applied by a different build than the one about to serve.

Local development uses **the same entry point** — `scripts/dev-up.sh` runs `scripts/db-migrate.sh`
before starting anything. There is deliberately no dev-only schema path any more: the mechanism
that runs in production is the one exercised every day, so it cannot rot unnoticed the way
`EnsureCreated` did.

## Consequences

- `EnsureCreatedAsync` is gone from all eight services. **Existing local databases must be dropped
  once** (`./scripts/dev-down.sh -v`) — a database created by `EnsureCreated` has no migration
  history, so EF will try to create tables that already exist.
- Schema changes now require a deliberate `./scripts/db-add-migration.sh <Name> <service>` and a
  reviewed diff. That is the point: a migration is DDL against real data and deserves review.
- Nothing else in the solution may reference `Npgsql` types directly. A future port means changing
  `EventPlatform.Persistence`, the per-service `UseNpgsql` lines, and the one partial-index filter —
  plus re-proving the concurrency guarantees on the new engine, which is the real cost.
- Migrations are generated by tooling, never hand-written. A hand-written migration defeats the
  model-as-source-of-truth property this decision was chosen for.
- On the **first ever** sync into an empty namespace, the PreSync jobs run before any Deployment
  pod has mounted the `eventplatform-keyvault` SecretProviderClass — and that mount is what creates
  the `eventplatform-secrets` Secret their `secretKeyRef`s read. The jobs mount the class themselves,
  so the secret materialises during their own volume mount and the kubelet's container-config retry
  picks it up; a first-sync job may sit briefly in `CreateContainerConfigError` before starting.
  Expected, self-resolving, and worth recognising rather than debugging from scratch.
- A migration that fails leaves its Job and pod logs in place (`hook-delete-policy:
  BeforeHookCreation` deletes the previous run only when the next one starts), and Argo CD reports
  the sync as failed without rolling any new pods. That is the behaviour migrating-on-startup could
  not give: there, the same failure is a crash-looping replica taking traffic down with it.

## What a port to MySQL or SQL Server would actually cost

Recorded so the decision can be revisited with numbers rather than instinct:

1. **Provider swap** — new EF provider package, the `UseNpgsql` lines, a per-provider migrations
   assembly. Days.
2. **`IsUniqueViolation`** — one method, per-provider error codes. Hours.
3. **Partial index** — no MySQL equivalent; needs a different mechanism to keep "one active signing
   key" true. Days, plus a correctness argument.
4. **Re-proving concurrency** — the no-oversell and no-double-charge guarantees re-tested against
   the new engine's isolation and unique-index behaviour, and a CI matrix that keeps them proven.
   **This is the majority of the work and the reason not to claim multi-engine support until it is
   actually needed.**

## References

- ADR-0001 (Azure single-cloud), ADR-0008 (database-per-service), ADR-0028 (a correct-looking
  change that was wrong only at runtime — the argument for keeping provider assumptions in one
  reviewable place)
- `building-blocks/EventPlatform.Persistence`, `scripts/db-add-migration.sh`, `scripts/db-migrate.sh`
