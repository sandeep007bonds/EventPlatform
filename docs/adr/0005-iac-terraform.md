# ADR-0005 — Infrastructure as Code: Terraform

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

All infrastructure must be reproducible and version-controlled. We had considered
multi-cloud but committed to single-cloud Azure (ADR-0001).

## Decision

Use **Terraform** for all infrastructure, in a layered structure:

- `infra/` — Azure resources (AKS, PostgreSQL, Redis, Service Bus / Event Hubs,
  Key Vault, networking).
- `platform/` — portable Kubernetes / Dapr / KEDA config (applied via Terraform
  or Helm).
- `services/` — per-service deployment values.

Remote state stored in Azure Storage.

## Consequences

- Reproducible, auditable environments (dev / staging / prod).
- Layering keeps app-platform config decoupled from cloud-specific infra; the
  `infra/` layer could gain an `aws/` module later if the model changes, but this
  is not built now.
- A stable interface between `platform` and `infra` keeps changes from rippling
  upward.

## Alternatives considered

- **Bicep / ARM** — Azure-native, but Terraform's module ecosystem and
  portability optionality win.
- **Pulumi** — capable, but Terraform is the more common standard.
