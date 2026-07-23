# Engineering Guidelines (Golden Rules)

These are the non-negotiable conventions for EventPlatform. The guiding
principle: **a rule only counts if tooling enforces it.** Each rule below is
tagged by how it's enforced — `[build]` breaks the build, `[CI]` blocks the PR,
`[review]` is checked by a human, `[convention]` is documented and expected.

Related: [ADRs](adr/), [HLD](design/hld.md), root `CLAUDE.md`.

---

## 1. Code style & structure

| Rule | Enforcement |
|------|-------------|
| **StyleCop** analyzers on every project | `[build]` `StyleCop.Analyzers` + `.editorconfig` |
| **Warnings are errors** — style violations fail the build | `[build]` `TreatWarningsAsErrors=true` |
| **Single class per file** (small nested records/enums allowed) | `[build]` SA1402 |
| **File-scoped namespaces**, `using` inside namespace, ordered | `[build]` `.editorconfig` |
| **Nullable reference types enabled** everywhere | `[build]` `Nullable=enable` |
| **Global usings** via `ImplicitUsings` + a curated `GlobalUsings.cs` | `[convention]` |
| **XML doc comments required on all `public`/`protected` members** | `[build]` `GenerateDocumentationFile=true` → CS1591 as error |

> **Doc-comment scope:** public/protected API **must** be documented (it's the
> contract other services and teammates consume). Private members should be
> self-documenting by name; add a comment only when the *why* isn't obvious.
> This keeps docs meaningful instead of noise. (Tighten to private-too later if
> desired by enabling the relevant rule.)

## 2. Dependencies

| Rule | Enforcement |
|------|-------------|
| **Central Package Management** — all versions pinned in `Directory.Packages.props` | `[build]` `ManagePackageVersionsCentrally=true` |
| No floating versions; one version per package repo-wide | `[build]` CPM |
| **Dependabot** for updates; **SBOM** on container images | `[CI]` |
| Check licensing before adding a package (e.g., MediatR / AutoMapper / MassTransit are now commercially licensed) | `[review]` |

## 3. Architecture guardrails (automate the ADRs)

| Rule | Enforcement |
|------|-------------|
| **Domain must not depend on Infrastructure** (Clean Architecture) | `[build]` architecture tests (`NetArchTest`) |
| **No service references another service's database**; sharing is contracts-only | `[build]` arch tests + `[review]` |
| **Every event-emitting service uses the transactional outbox** | `[review]` (ADR-0010) |
| **Idempotency keys** on all money/inventory-mutating endpoints | `[review]` (LLD) |
| **API versioning** (`/v1`), OpenAPI spec generated per service | `[CI]` |
| **Clean Architecture + Vertical Slices + CQRS** layout (Inventory hot path leaner) | `[convention]` (ADR-0009) |

## 4. Testing

| Rule | Enforcement |
|------|-------------|
| Every service has **unit + integration tests** (Testcontainers for DB/Redis/Dapr) | `[CI]` |
| **Minimum coverage gate** (start at 80%, tune per service) | `[CI]` |
| Correctness-critical paths (hold, saga) have **concurrency/load tests** | `[CI]` (Phase 1 exit criteria) |

## 5. Security & observability (non-negotiable for this domain)

| Rule | Enforcement |
|------|-------------|
| **No secrets in code** — Key Vault only | `[CI]` secret scanning |
| **OpenTelemetry + structured logging + correlation IDs** in every service | `[convention]` + template |
| **Health & readiness endpoints** in every service | `[convention]` (K8s/Argo depend on them) |
| **PCI SAQ-A**: no card data on our servers | `[review]` (ADR-0012) |
| Tenant context from validated JWT only; never trust `tenant_id` from the body | `[review]` (ADR-0011) |

## 6. Delivery & process

| Rule | Enforcement |
|------|-------------|
| **A tracking issue for every unit of work** — no PR without a linked issue | `[CI]` PR template + branch protection |
| **Branch names carry the issue number** (e.g., `feat/7-inventory-hold`) | `[convention]` |
| **Conventional Commits** (`feat:`, `fix:`, `docs:`, …) | `[convention]` |
| **PR requires**: CI green + 1 review + linked issue | `[CI]` branch protection on `main` |
| **GitOps only** — no manual `kubectl`/`helm` to clusters (Argo CD reconciles) | `[convention]` (ADR-0004) |
| **ADR for any significant decision** | `[review]` |

## 7. Documentation

| Rule | Enforcement |
|------|-------------|
| **Root `CLAUDE.md`** (global rules) + **per-service `CLAUDE.md`** | `[convention]` |
| **README per service**: purpose, run-locally, endpoints, events pub/sub | `[review]` |
| Public API documented (XML docs → generated OpenAPI) | `[build]` |

---

## How the rules are wired

| Rule area | Where it lives |
|-----------|----------------|
| Style, nullable, warnings-as-errors, analyzers, doc file | `Directory.Build.props` + `.editorconfig` |
| Package versions | `Directory.Packages.props` |
| Architecture boundaries | `tests/Architecture.Tests` (runs in CI) |
| PR requirements | `.github/pull_request_template.md` + branch protection |
| Per-service conventions | `templates/service-template/` (scaffolds them in) |
| Agent + team rules | `CLAUDE.md` (root and per-service) |

New services inherit all of this by starting from the service template — the
golden rules are the build configuration, not tribal knowledge.
