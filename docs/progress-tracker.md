# Progress & Tech-Debt Tracker

Single place to track what's done, what's in progress, and — importantly — the
**deferred cloud tasks and tech debt**, so nothing is lost while we focus on
local development. Update this with each meaningful change.

**Legend:** ✅ done · 🚧 in progress · ⏸️ deferred (on hold) · ⬜ not started

---

## Progress

### Foundations & documentation
- ✅ Architecture, requirements, feature flows (6)
- ✅ ADRs 0001–0014
- ✅ HLD, DFD, LLD (Phase 1 seated)
- ✅ Engineering guidelines + golden rules
- ✅ Build config: `.editorconfig`, `Directory.Build.props`, CPM, PR template
- ✅ Root + per-service `CLAUDE.md`
- ✅ Roadmap issues (#1–#11) on GitHub

### Code scaffold
- ✅ Solution (`.slnx`)
- ✅ `EventPlatform.Hosting` — shared service defaults (auth, OpenAPI, JSON, OTel, health)
- ✅ `EventPlatform.Contracts` — base integration event + sample
- ✅ Local Docker dev stack (compose + Dapr components + guide)
- ✅ **Build hardening — green under warnings-as-errors** (deps patched, .NET 10 pruning, global usings, analyzer config)
- ✅ **Service structure flattened** (no `src/`) — layers directly under `services/<name>/`; standard for all services
- 🚧 **Catalog service implementation (issue #6)** ← current focus
  - ✅ Domain (`Event` aggregate, `EventStatus`)
  - ✅ Application slices: `CreateEvent`, `GetEvent` (+ FluentValidation pipeline)
  - ✅ Infrastructure: EF Core + Postgres (`CatalogDbContext`, repository)
  - ✅ API wired: `POST /v1/events`, `GET /v1/events/{id}` via MediatR
  - ✅ Local `dotnet build` green; runs against local Postgres (`EnsureCreated`)
  - ✅ `PublishEvent` slice (draft → published)
  - ✅ **Transactional outbox + Dapr (first use):** `PublishEvent` enqueues `EventPublished` into the `outbox` table in the same transaction; shared `EventPlatform.Messaging` relay publishes it to Dapr pub/sub (at-least-once, CloudEvent id = outbox id) ← **verify build locally**
  - 🚧 Seat map (seats + hand-off so Inventory can generate inventory) ← **next**
  - ⬜ EF Core migrations (replace `EnsureCreated` — see T8)
- ⬜ Inventory & Hold — no-oversell core (issue #7) — consumes `EventPublished`
- ⬜ Order + checkout saga (#8) — Dapr Workflow
- ⬜ Payment — Stripe test (#9)
- ⬜ Ticketing (#10)
- ⬜ No-oversell load test (#11)

---

## ⏸️ Deferred — CLOUD tasks (ON HOLD — do not lose)

Intentionally paused while we build locally. Revisit before first deploy.

| # | Task | Related |
|---|------|---------|
| C1 | GitHub Actions CI (`dotnet build`/`test`, analyzers, coverage) | ADR-0004 |
| C2 | Terraform: AKS cluster + node pools (system/general/hot-path/spot) | ADR-0002/0005 |
| C3 | Argo CD / GitOps + `deploy/` manifests | ADR-0004 |
| C4 | Dockerfiles + Helm charts per service | Phase 0 |
| C5 | Azure infra: PostgreSQL, Redis, Service Bus/Event Hubs, Key Vault | ADR-0001 |
| C6 | Entra (Azure AD B2C) identity — prod JWT authority | Security |
| C7 | Cloud Dapr components (Service Bus / Azure Cache / Key Vault) — mirror local | ADR-0006 |
| C8 | Azure Front Door + WAF + bot management (edge) | Phase 3 |
| C9 | Observability backend (Azure Monitor / managed Grafana) | Observability |
| C10 | Multi-AZ + DR runbook (RTO < 15m / RPO < 1m) | Phase 3 |

## ⏸️ Deferred — GitHub / ops actions (need repo owner)

| # | Action | Notes |
|---|--------|-------|
| O1 | Make repo **private** | currently public |
| O2 | Branch protection on `main` (PR + review + linked issue + checks) | enforces golden rules |
| O3 | Add `ADD_TO_PROJECT_PAT` secret | for project auto-add workflow |
| O4 | Move auto-add workflow to `main` | issue-triggered workflows run from default branch |
| O5 | Invite teammates to repo + project | |
| O6 | Remove old `services/catalog/src/` (git rm) | leftover from the structure flatten |

---

## Tech debt / to verify

| # | Item | Notes |
|---|------|-------|
| T1 | Confirm CPM package versions | ✅ build green — versions resolve, vulns patched |
| T2 | Build the scaffold locally (`dotnet build`) | ✅ green |
| T3 | MediatR pinned to **12.5.0** | do NOT let Dependabot bump to 13.x (commercial) — ADR-0014 |
| T4 | Dev HTTPS cert | `dotnet dev-certs https --trust` |
| T5 | Architecture-tests project (NetArchTest) | enforce ADR-0008/0009 boundaries in CI |
| T6 | Coverage gate + integration tests (Testcontainers) | Phase 1 exit criteria |
| T7 | Replace Catalog placeholder endpoint | ✅ done — real CreateEvent/GetEvent slices |
| T8 | Catalog EF Core migrations | dev uses `EnsureCreated`; add `dotnet ef migrations add InitialCreate` before any shared env |
| T9 | Transactional outbox building-block | ✅ `EventPlatform.Messaging` — write path (`IEventPublisher`/`OutboxMessage`/`ApplyOutbox`) + `OutboxRelay` (Dapr pub/sub, at-least-once). Consumed side (dedupe on CloudEvent id) lands with Inventory (#7). Needs the `outbox` table in migrations (T8) |

---

## How this is maintained

- Update on every meaningful change (part of "done").
- Cloud tasks stay in the **Deferred** tables until we explicitly resume cloud work.
- When board work resumes, mirror open items as GitHub issues.
