# infra/environments/dev

Minimal-cost Azure dev environment: one AKS cluster, one shared Postgres
Flexible Server (5 databases), one Redis cache (pub/sub + Dapr Workflow
state), one ACR, one Key Vault. Deliberately diverges from the production
topology in the ADRs — see
[ADR-0017](../../../docs/adr/0017-dev-environment-cost-topology.md) for every
divergence and why.

This pass provisions the Azure resources only. Deploying the app onto the
cluster (Helm/K8s manifests, Argo CD, Dapr install, CI OIDC federation) is
out of scope here — a future pass once these resources exist.

## Prerequisites

- `infra/bootstrap` has been applied once (see its README) — you need its
  `resource_group_name`, `storage_account_name`, and `container_name`
  outputs.
- Azure CLI logged in (`az login`) with a subscription that can create the
  resources below.
- Terraform >= 1.9.

## Apply steps

```bash
cd infra/environments/dev

terraform init \
  -backend-config="resource_group_name=<bootstrap resource_group_name output>" \
  -backend-config="storage_account_name=<bootstrap storage_account_name output>" \
  -backend-config="container_name=<bootstrap container_name output>"

cp terraform.tfvars.example terraform.tfvars
# edit terraform.tfvars: subscription_id, postgres_administrator_password, etc.
# never commit terraform.tfvars.

terraform plan   # review resource count and cost before proceeding
terraform apply  # when satisfied with the plan
```

After apply:

```bash
az aks get-credentials --resource-group "$(terraform output -raw resource_group_name)" \
  --name "$(terraform output -raw aks_cluster_name)"
kubectl get nodes

az acr login --name "$(terraform output -raw acr_login_server | cut -d. -f1)"

# Postgres admin password and Redis access key are in Key Vault, not in
# terraform output, other than the Postgres FQDN/database names.
```

## GitHub Actions CD secrets

This apply also creates a GitHub Actions OIDC identity (see
`github-oidc.tf`) with `AcrPush` on this environment's registry — no client
secret involved, GitHub proves its identity with a short-lived token per
workflow run. After apply, set these as repository secrets (Settings →
Secrets and variables → Actions) so `.github/workflows/cd.yaml` can log in:

```bash
terraform output -raw github_actions_client_id   # -> AZURE_CLIENT_ID
terraform output -raw aks_tenant_id               # -> AZURE_TENANT_ID
# AZURE_SUBSCRIPTION_ID is whatever you set `subscription_id` to in your tfvars
```

## Validating without Azure credentials

This environment's real backend needs Azure reachability that a plain
`terraform init` doesn't have without credentials. To check syntax/config
only:

```bash
terraform init -backend=false
terraform validate
```

## Cost

See the cost table in [infra/README.md](../../README.md). `node_count`
defaults to 1. Run `./stop.sh` to deallocate the AKS node VMs and stop the
Postgres server overnight/when idle (no Terraform change needed — cluster and
server config are preserved, only their compute billing stops); `./start.sh`
resumes both. These are bash scripts — on Windows, run them from Git Bash or
WSL, or call the underlying `az aks stop`/`az postgres flexible-server stop`
commands directly from PowerShell.

Redis, the AKS Load Balancer + public IP, ACR, and Key Vault have no
stop/pause capability and keep billing regardless (~$35-40/mo baseline) — for
a longer pause, `terraform destroy` then `terraform apply` again later gets
you to zero cost while idle, since this is a disposable dev environment with
no data worth preserving across a teardown.

## Do not

- Do not commit a real `terraform.tfvars` — only `terraform.tfvars.example`.
- Do not hand-edit state. Use `terraform state` subcommands if you ever need
  to inspect or move resources.
- Do not remove a name from `db_names` in `locals.tf` expecting a clean
  apply — each database has `prevent_destroy = true`; you must deliberately
  remove the lifecycle block first if you really intend to drop a database.
- Do not assume `location` alone determines every region: Postgres Flexible
  Server has its own per-subscription offer restrictions and can reject a
  region that AKS/VNet/Key Vault/Redis accept fine. Use the separate
  `postgres_location` variable to target Postgres at a different region
  without moving (and replacing) everything else.
- If `terraform apply` fails creating the AKS cluster with
  `AKSCapacityHeavyUsage`, that's a transient Azure-side capacity throttle on
  new cluster creation in that region, not a config problem — it clears
  unpredictably. Retry `apply` first; if it persists, override `aks_location`
  (which also moves the VNet/subnet, since AKS requires them in the same
  region) to a region with available capacity, leaving Key Vault/Postgres/
  Redis where they already are.
