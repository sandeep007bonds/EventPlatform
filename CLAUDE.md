# CLAUDE.md — EventPlatform (root)

Guidance for Claude Code (and humans) working anywhere in this repository.
Per-service `CLAUDE.md` files add service-specific rules on top of these.

## What this is

A multi-tenant SaaS ticketing platform for high-demand live events. See
[README](README.md), [architecture](docs/02-architecture.md), the [ADRs](docs/adr/),
and the [detailed design](docs/design/). Read the [engineering guidelines](docs/engineering-guidelines.md)
before writing code — they are enforced by the build, not optional.

## Locked stack (see ADRs)

Azure single-cloud SaaS · **AKS** · **.NET 10 (LTS)** · **Dapr** · **Terraform** ·
**GitHub Actions + Argo CD (GitOps)** · **Monorepo** · DDD bounded contexts,
database-per-service · Clean Architecture + Vertical Slices + CQRS ·
event-driven + orchestrated checkout saga + transactional outbox ·
hybrid multi-tenancy · payments saga + idempotency + PCI SAQ-A · Phase 1 = seated.

## Golden rules (must follow)

1. **One class per file.** File-scoped namespaces, nullable enabled.
2. **XML doc comments on all public/protected members.** Private members:
   self-documenting names; comment only the non-obvious *why*.
3. **StyleCop + analyzers pass with zero warnings** — warnings are errors.
4. **Central Package Management** — never add a version to a `.csproj`; pin it in
   `Directory.Packages.props`. Check a package's licence before adding it.
5. **Respect the layers.** Domain never depends on Infrastructure. Never read
   another service's database — talk via API or events only.
6. **Idempotency + outbox** on anything touching money or inventory.
7. **No secrets in code.** Key Vault only.
8. **Every service** ships: tests (unit + integration), health/readiness
   endpoints, OpenTelemetry, a README, and a `CLAUDE.md`.

## Workflow rules

- **Create a tracking issue for every unit of work** before starting it. No PR
  without a linked issue.
- **Branch per issue**, name it with the issue number: `feat/<n>-short-slug`,
  `fix/<n>-...`, `docs/<n>-...`. Never commit to `main`.
- **Conventional Commits** (`feat:`, `fix:`, `docs:`, `test:`, `refactor:`,
  `chore:`, `ci:`).
- **PRs require** CI green + review + linked issue. Keep them small.
- **GitOps only** — do not `kubectl apply` / `helm install` to a cluster by
  hand; change the `deploy/` manifests and let Argo CD reconcile.
- **ADR for significant decisions** — add a numbered record in `docs/adr/`.

## Build & test (once services exist)

```bash
dotnet build            # must be warning-free (warnings = errors)
dotnet test             # unit + integration (Testcontainers)
dotnet format --verify-no-changes   # style check
```

## Repo layout

```
services/         # one independently-deployed service per bounded context
building-blocks/  # shared libs; contracts/ is the ONLY cross-service dependency
gateways/         # YARP gateway + BFFs
platform/         # Dapr components, KEDA scalers, K8s/Helm (portable)
infra/            # Terraform (Azure)
deploy/           # GitOps manifests reconciled by Argo CD
templates/        # dotnet new service template (carries the golden rules)
docs/             # architecture, ADRs, design, guidelines
```

## Per-service structure (Clean + Vertical Slices)

`X.Api` (host) · `X.Application` (Features/ slices) · `X.Domain` (invariants) ·
`X.Infrastructure` (adapters) · `X.Workflow` (Dapr workflow, where needed) ·
`tests/`. The **Inventory** service is intentionally leaner on the hot path.
See [LLD §2](docs/design/lld-phase1-seated.md).
