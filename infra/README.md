# infra — Terraform (Azure)

Azure infrastructure for EventPlatform, per [ADR-0005](../docs/adr/0005-iac-terraform.md).

## Layout

```
infra/
  bootstrap/           # one-time, local-state: creates the remote-state storage account
  environments/dev/    # minimal-cost dev environment (see ADR-0017)
  modules/             # leaf modules (resource-group, networking, container-registry,
                        # aks, postgres, redis, key-vault) — reused across environments
```

Currently only `dev` exists. A `staging`/`production` environment would be a
new `infra/environments/*` config that follows the full topology in
[ADR-0002](../docs/adr/0002-runtime-aks.md) and
[ADR-0005](../docs/adr/0005-iac-terraform.md) (dedicated hot-path node
pools, per-service Postgres servers, Service Bus) — `dev` deliberately does
not, see [ADR-0017](../docs/adr/0017-dev-environment-cost-topology.md).

## Apply flow

1. `infra/bootstrap` — apply once, ever (creates remote state storage).
2. `infra/environments/dev` — init with the bootstrap outputs as
   `-backend-config`, then `plan`/`apply`.

Full steps are in each directory's own README:
[infra/bootstrap/README.md](bootstrap/README.md),
[infra/environments/dev/README.md](environments/dev/README.md).

## Cost (dev environment, pay-as-you-go, rough — verify with the Azure Pricing Calculator before applying)

| Item | Est. monthly |
|---|---|
| AKS control plane (Free tier) | $0 |
| Node pool (1× `Standard_D2s_v7`, default; `node_count`/`node_vm_size` are tfvars) | **verify — see note below** |
| Postgres Flexible B1ms + storage | ~$15-20 (possibly $0 under a new subscription's free allowance) |
| Redis Basic C0 | ~$16 |
| ACR Basic | ~$5 |
| AKS default Standard Load Balancer + egress IP (provisioned even with zero `LoadBalancer` Services) | ~$15-20 |
| Key Vault + tfstate Storage Account | <$2 |
| Log Analytics (Container Insights) | $0 at the default 0.15 GB/day cap — that keeps a month inside Azure Monitor's 5 GB free grant. Raise `log_analytics_daily_quota_gb` and you start paying ~$2.30/GB. |
| **Total (1 node, default)** | **~$55-65/mo + node pool (verify)** |

**Note on node pool cost:** `Standard_B2ms` (burstable, ~$60/mo) was the
original recommendation, but some subscriptions' allowed-SKU lists don't
include B-series at all — `terraform apply` fails with a 400 naming the
allowed sizes for yours. `Standard_D2s_v7` (this repo's current default) is
the smallest general-purpose size on such lists, but general-purpose pricing
is typically higher than burstable for the same vCPU/RAM — check the Azure
Pricing Calculator for your subscription's actual rate before relying on the
total below. If B-series becomes available on your subscription, switch back
via the `node_vm_size` tfvar.

Two nodes (headroom for Dapr sidecars across 6 services/gateway) roughly
adds the node-pool line again (~$175-185/mo total). `az aks stop`
deallocates node VMs (preserves cluster config) for nights/idle time with no
Terraform change.

## What observability you actually get

Container Insights ships **container stdout/stderr logs and node/pod metrics**
to Log Analytics, queryable with KQL in the portal. That is what makes a
deployed problem diagnosable at all, instead of racing `kubectl logs` against a
pod that may already have been replaced.

It does **not** give you distributed traces. The services do emit OpenTelemetry
traces, to whatever `OTEL_EXPORTER_OTLP_ENDPOINT` points at — and nothing in
`deploy/` sets that variable, so in AKS the exporter has no destination and
traces go nowhere. Locally they reach Jaeger; in the cluster they are simply
lost. Closing that needs an OpenTelemetry Collector running in-cluster (or the
Azure Monitor OTel distro wired into each service), which is real work and is
not done. Worth knowing before assuming a trace will be waiting for you.

The daily cap is a hard stop, not a warning: past it, ingestion stops for the
rest of the UTC day and that data is gone. On a personal subscription that is
the right trade — a chatty log loop should cost you a few hours of visibility
rather than a bill you did not expect.
