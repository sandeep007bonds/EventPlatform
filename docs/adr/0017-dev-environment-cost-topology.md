# ADR-0017 — Dev environment: minimal-cost topology, diverging from production

- **Status:** Accepted
- **Date:** 2026-07-28

## Context

The production topology locked in by ADR-0002 (AKS with dedicated hot-path
node pools) and ADR-0005 (Terraform, database-per-service) is designed to
survive extreme on-sale spikes across many tenants. Running a personal,
free/pay-as-you-go Azure subscription against that topology is not viable:
it would burn through subscription credit quickly and risks hitting quota
limits, for an environment that only ever needs to prove the system runs on
real infrastructure and support day-to-day development.

`infra/` did not exist before this ADR — this is the first real Terraform in
the repo. The local-dev Dapr component YAMLs in `platform/dapr/components/`
already document their intended **production** replacements (e.g.
`pubsub.yaml`: "In Azure this is replaced by an Azure Service Bus
component"). This ADR records a **dev-only** environment that intentionally
does not follow those production replacements either, for the same
cost reason.

## Decision

Introduce `infra/environments/dev`, a minimal-cost Azure topology, scoped
**only** to the `dev` environment. It does not change the production target
recorded in ADR-0002 or ADR-0005.

- **One Postgres Flexible Server, five databases** (`catalog`, `inventory`,
  `ordering`, `payments`, `ticketing`, matching every service's existing
  `ConnectionStrings` names), not five separate servers. Burstable
  `B_Standard_B1ms` — the cheapest usable tier, and may fall under a new
  subscription's 12-month free allowance.
- **One Azure Cache for Redis (Basic C0) serves both Dapr pub/sub AND the
  Dapr Workflow actor state store.** This is the single biggest divergence
  from the production plan, which uses Service Bus for pub/sub (ADR-0005).
  `statestore.yaml` sets `actorStateStore: "true"` for the checkout saga;
  Redis' default `maxmemory-policy` (`volatile-lru`) would silently evict
  in-flight saga state under memory pressure, so the Terraform module sets
  `maxmemory_policy = "noeviction"` explicitly — the failure mode becomes a
  loud write error, not silent data loss. Both roles intentionally share the
  same 250MB instance; acceptable only because dev traffic is low.
- **AKS**: single system node pool, no dedicated hot-path pools (the
  explicit divergence from ADR-0002's node-pool isolation), `sku_tier =
  "Free"` control plane (no SLA, $0), Azure CNI Overlay networking (not
  kubenet, which is legacy for new clusters), `oidc_issuer_enabled` and
  `workload_identity_enabled` both set at creation — free to enable now,
  avoids a disruptive cluster recreation later when Key Vault CSI/Workload
  Identity is actually wired up (that wiring itself remains future work).
- **Explicitly not built in this pass:** Front Door/WAF, Log
  Analytics/Container Insights, VNet peering/private DNS/Bastion/Azure
  Firewall, Microsoft Defender for Cloud, Service Bus, any K8s
  manifests/Helm/Argo CD, GitHub Actions OIDC federation for CI-driven
  applies. Listed here so each gap is a recorded decision, not an oversight.

This ADR does **not** supersede ADR-0002 (node-pool isolation remains the
production target) or ADR-0005 (Service Bus remains the production target
for pub/sub) — scoped only to `dev`, the same pattern ADR-0011 uses for
"promotion is config, not code."

## Consequences

- A personal/free subscription can run the real system end-to-end for
  ~$115-125/mo at the default 1-node pool (see `infra/README.md` for the
  full cost table), instead of the production topology's much higher floor.
- The shared Redis instance is a single point of contention between pub/sub
  and actor state, and a single point of failure for both — unacceptable in
  production, accepted here for dev traffic volumes.
- Promoting from dev to a staging/production environment requires actually
  building the ADR-0002/0005 topology (dedicated node pools, per-service
  Postgres servers, Service Bus) as a separate `infra/environments/*`
  config — not a parameter flip on this one.
- `node_count` defaults to 1; `az aks stop`/`az aks start` is documented as
  a no-Terraform-change way to stop node billing when idle.

## Alternatives considered

- **Build the full production topology now, just at smaller SKUs** —
  rejected: Service Bus, per-service Postgres servers, and dedicated node
  pools each carry their own fixed monthly floor regardless of SKU size,
  which is what actually drives cost here, not compute size alone.
- **Azure Container Apps instead of AKS for dev** — rejected for
  consistency: the same manifests/Helm charts/Dapr config should run in dev
  and production; switching runtimes between environments would mean
  maintaining two deployment models.
