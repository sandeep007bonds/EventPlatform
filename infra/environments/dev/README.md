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
defaults to 1; run `az aks stop --resource-group <rg> --name <cluster>` to
deallocate node VMs overnight/when idle (no Terraform change needed — cluster
config is preserved, only node billing stops). Start it again with
`az aks start`.

## Do not

- Do not commit a real `terraform.tfvars` — only `terraform.tfvars.example`.
- Do not hand-edit state. Use `terraform state` subcommands if you ever need
  to inspect or move resources.
- Do not remove a name from `db_names` in `locals.tf` expecting a clean
  apply — each database has `prevent_destroy = true`; you must deliberately
  remove the lifecycle block first if you really intend to drop a database.
