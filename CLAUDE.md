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

1. **One type per file.** File-scoped namespaces; nullable enabled.
2. **XML doc comments on all public/protected members.** Private members:
   self-documenting names; comment only the non-obvious *why*.
3. **StyleCop + analyzers pass with zero warnings** — warnings are errors.
4. **Central Package Management** — never add a version to a `.csproj`; pin it in
   `Directory.Packages.props`. Check a package's licence before adding it.
5. **Respect the layers.** Domain never depends on Infrastructure. Never read
   another service's database — talk via API or events only.
6. **Idempotency + outbox** on anything touching money or inventory.
   Every pub/sub subscriber uses `.SubscribesTo(topic, deadLetterTopic)` rather than a bare
   `.WithTopic(...)`: one call, both conventions — the message's correlation chain is adopted, and
   a message that cannot be handled has somewhere to go (ADR-0040). Each fails *silently* on its
   own, which is why they are one call and why the checker rejects the bare form.
7. **No secrets in code.** Key Vault only.
8. **Every service** ships: tests (unit + integration), health/readiness
   endpoints, OpenTelemetry, a README, and a `CLAUDE.md`.
9. **Never hit the same build error twice.** Every analyzer or compiler error
   that reaches a build gets recorded in
   [docs/build-error-log.md](docs/build-error-log.md) — the rule, the cause, the
   fix. If it is mechanically detectable, add it to
   `scripts/check-csharp-style.py` in the *same* commit as the fix, calibrated to
   zero findings on the passing tree and verified to catch the real failure. If
   it is not detectable without semantic analysis, say so in the log rather than
   approximating it. The log is the record; the checker is the enforcement.

## Coding conventions (C#) — match these exactly

Enforced by `.editorconfig` + `Directory.Build.props` (warnings are errors).

- **Global usings ONLY.** Every project has a `GlobalUsings.cs` holding its
  `global using` directives. **Do not put `using` directives in individual
  `.cs` files** — add the namespace to that project's `GlobalUsings.cs`
  instead. Every other file starts straight at `namespace X;`.
- **File-scoped namespaces** (`namespace X;`); usings live **outside** the
  namespace (required for file-scoped ns + global usings).
- **One type per file** (SA1402); small nested records/enums may share.
- **XML docs on all public/protected members** (CS1591 / SA1600 as errors).
- **Central Package Management** — versions only in `Directory.Packages.props`;
  never `Version="..."` on a `.csproj` `PackageReference`.
- **.NET 10 framework packages are NOT referenced explicitly** (e.g.
  `System.Security.Cryptography.Xml`) — they're framework-provided and
  auto-pruned; an explicit reference fails with **NU1510**. Fix a vulnerable
  *framework* transitive by removing the ref and letting pruning use the
  patched framework version.
- **Vulnerable NON-framework transitives** are fixed with a direct
  `PackageReference` at the patched version in the project that pulls them
  (transitive pinning is off — it conflicts with EF Core Design's tree).
- **EF Core Design** is referenced only where migrations are generated, not in
  every Infrastructure project (its MSBuild/Roslyn tree pulls vulnerable
  transitives).
- **Forward `CancellationToken`** through async calls (CA2016).
- **Generated code is exempt, and must stay that way.** `Migrations/` and
  `Generated/` have all analyzer diagnostics silenced in `.editorconfig` — via
  a `**.cs` glob, not `**/*.cs`, which would miss files sitting directly in
  those folders (which is where EF puts them). EF
  rewrites those files on every `migrations add`, so hand-fixing a style
  violation there is undone by the next model change — do not "tidy" a
  migration, and do not add rule IDs back. Review a migration for what its DDL
  does to real data, not for how it is formatted.
- **Deliberately-disabled analyzer rules** (see `.editorconfig`): SA1101
  (`this.` prefix), SA1623 (`Gets/Sets` doc prefix), SA1642 (ctor boilerplate),
  CA1716 (keyword-like type names such as `Event`). Don't re-enable without
  discussion.

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

### Check before you build

```bash
python3 scripts/check-csharp-style.py     # analyzer errors, whole tree, no toolchain
python3 scripts/check-endpoint-conventions.py  # auth decisions + subscriber envelopes
git config core.hooksPath .githooks       # once per clone: run both on commit
```

`dotnet build` is still the authority, but it is the *slow* one: it stops at the
first failing project, so one run surfaces one project's errors and the next run
surfaces the next project's. `check-csharp-style.py` checks all 867 files in under
a second and front-loads the rules that keep breaking the build — SA1117, S125,
SA1506, SA1515, the `<param>` rules, record arity, and the four that a re-key
keeps producing (CS0102, CS1061, S1144, S4136).

**A green checker is not a green build.** It is a regex tool with no type
information, so it cannot see a `StringComparer` handed to a `List<Guid>`. A clean
run means the recurring mistakes are absent; only `dotnet build` says it compiles.

**Its rules are calibrated, and new ones must be too.** The bar is zero findings on
a tree that compiles: a rule that fires on passing code is a wrong rule, not a
finding, and a checker that cries wolf gets ignored. Rules needing real semantic
analysis (SA1204/SA1201 ordering, nullability, the CA performance rules) are
deliberately absent rather than approximated — the script's docstring says so, and
that list should stay honest.

## Repo layout

```
services/         # one independently-deployed service per bounded context
building-blocks/  # shared libs; contracts/ is the ONLY cross-service dependency
gateways/         # YARP gateway + BFFs
frontend/         # React + Vite SPA (buyer + admin) — own conventions, see frontend/CLAUDE.md
platform/         # Dapr components, KEDA scalers, K8s/Helm (portable)
infra/            # Terraform (Azure) — dev topology diverges from production ADRs, see ADR-0017
deploy/           # GitOps manifests reconciled by Argo CD
templates/        # dotnet new service template (carries the golden rules)
docs/             # architecture, ADRs, design, guidelines
```

The C# golden rules and coding conventions above apply to `services/`,
`building-blocks/`, `gateways/`, and `templates/`. `frontend/` is a
TypeScript/React project with its own conventions — see
[frontend/CLAUDE.md](frontend/CLAUDE.md) — not these C# rules.

## Per-service structure (Clean + Vertical Slices)

`X.Api` (host) · `X.Application` (Features/ slices) · `X.Domain` (invariants) ·
`X.Infrastructure` (adapters) · `X.Workflow` (Dapr workflow, where needed) ·
`tests/`. The **Inventory** service is intentionally leaner on the hot path.
See [LLD §2](docs/design/lld-phase1-seated.md).
