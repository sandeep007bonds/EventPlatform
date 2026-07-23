# ADR-0004 — CI/CD: GitHub Actions + Argo CD (GitOps)

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

Source is on GitHub. We need independent per-service delivery to AKS, with a
strong security posture (money + PII domain) and reliable rollback.

## Decision

- **CI: GitHub Actions** — build, test, security/dependency scan, push container
  image, bump the image version in Git.
- **CD: Argo CD (pull-based GitOps)** — Argo CD runs in-cluster and reconciles
  desired state from Git; CI never holds cluster credentials.
- Deployment config lives in a `deploy/` tree in the monorepo (separated from app
  source to avoid CI-retrigger loops), with per-environment overlays.
- **Progressive delivery** via Argo Rollouts (canary / blue-green) to replace
  Container Apps' revision traffic-splitting.

## Consequences

- No cluster credentials in CI (a major security win); drift detection,
  `git revert` rollback, and a full deploy audit trail.
- Path-filtered pipelines keep each service independently deployable.
- Requires the team to learn GitOps; worth it for safety on live on-sale days.

## Alternatives considered

- **Azure DevOps Pipelines** — fine, but adds a second platform alongside the
  GitHub source. Rejected for simplicity.
- **Flux** — equivalent GitOps; Argo CD chosen for its UI and Rollouts. An
  acceptable substitute.
- **Push-based pipelines (helm/kubectl from CI)** — simpler but stores cluster
  credentials in CI and loses GitOps guarantees. Rejected.
