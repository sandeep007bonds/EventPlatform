# ADR-0002 — Runtime: AKS (Kubernetes) from day one

- **Status:** Accepted
- **Date:** 2026-07-23

## Context

Services are containerized and must be independently deployable and able to
survive extreme, scheduled on-sale spikes. We evaluated Azure Container Apps
(managed, simpler) vs Azure Kubernetes Service (AKS). The tenancy model
(ADR-0011) is hybrid and requires hot-path cell isolation so one tenant's
on-sale cannot starve others.

## Decision

Run on **AKS from day one**, paired with GitOps (ADR-0004). The deciding factor
is **node-pool / cell isolation**: the platform's core job is surviving spikes,
and hybrid tenancy needs dedicated node pools for hot events — which Container
Apps cannot provide. The rationale is **control and isolation, not portability**
(portability ceased to matter once we committed to single-cloud SaaS in
ADR-0001).

## Consequences

- Full control: node pools (system / general / dedicated hot-path / spot), custom
  networking, service mesh, fine-grained autoscaling (KEDA + cluster autoscaler /
  node autoprovisioning).
- Higher operational burden than Container Apps — mitigated by managed AKS
  add-ons (managed Dapr, KEDA, Prometheus/Grafana, app-routing ingress, Key
  Vault CSI + Workload Identity) and by treating the platform (cluster + GitOps +
  IaC) as its own owned workstream.
- Dapr/KEDA/Helm are native, so nothing else in the design changes.
- **Accepted caveat:** AKS is more ops early; since we are single-cloud, a
  "Container Apps now → AKS later" path existed and was consciously declined in
  favour of being right from day one.

## Alternatives considered

- **Azure Container Apps** — faster to start, less ops, but no node-level
  isolation and Azure-proprietary. Rejected for the isolation requirement.
- **Container Apps now, AKS later** — viable but adds a migration and delays the
  isolation capability we know we need. Rejected.
