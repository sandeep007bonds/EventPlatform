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
