# ADR-0007 — Repository layout: Monorepo

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

We have multiple independently-deployable services, plus shared contracts, a
service template, and IaC. We want extensibility (adding a service should be
cheap) without recreating a distributed monolith.

## Decision

Use a **single monorepo** (`eventplatform`) containing all services, shared
building-blocks, gateways, platform config, IaC, and templates. Each service
deploys **independently** via **path-filtered CI/CD pipelines**.

Top-level structure: `services/`, `building-blocks/` (incl. `contracts/`),
`gateways/`, `platform/`, `infra/`, `templates/`, `deploy/`, `.github/workflows/`.

## Consequences

- One place for shared contracts, templates, and IaC → adding a service is
  copy-template + reference-contracts.
- Guardrails against a distributed monolith: (1) path-filtered pipelines preserve
  independent deploy; (2) the only shared dependency is
  `building-blocks/contracts` (events/DTOs, SemVer, additive changes only) — no
  shared business logic, no shared database.

## Alternatives considered

- **Polyrepo (one repo per service)** — strongest isolation but heavy cross-repo
  overhead for contracts/template/IaC. Rejected.
- **Hybrid (platform repo + service repos)** — more moving parts than needed now.
  Rejected.
