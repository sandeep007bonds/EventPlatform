# infra/environments/dev

Minimal-cost Azure dev environment: one AKS cluster, one shared Postgres
Flexible Server (5 databases), one Redis cache (pub/sub + Dapr Workflow
state), one ACR, one Key Vault, a GitHub Actions OIDC identity, and Argo CD
(installed onto the cluster via the `helm`/`kubectl` providers in
`argocd.tf`, in the same apply — see `platform/argocd/README.md`).
Deliberately diverges from the production topology in the ADRs — see
[ADR-0017](../../../docs/adr/0017-dev-environment-cost-topology.md) for every
divergence and why.

This pass provisions the Azure resources and installs Argo CD; it does not
deploy the *application* onto the cluster — that's `deploy/` +
Argo CD reconciling it (GitOps), triggered by `.github/workflows/cd.yml`
after your first push.

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

Argo CD is already installed and watching `deploy/overlays/dev` at this
point — see `platform/argocd/README.md` for the admin password and UI
access.

## Finishing the bootstrap

Two things still need real values that only exist after this apply: the
Key Vault SecretProviderClass in `deploy/` (identity/vault/tenant IDs), and
the GitHub Actions secrets `.github/workflows/cd.yml` needs to log in to
Azure (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
`ACR_LOGIN_SERVER`). One script does both:

```bash
./scripts/finish-dev-bootstrap.sh
```

It reads everything from `terraform output`, fills in and commits
`deploy/overlays/dev/keyvault-secretproviderclass.yaml`, and — if the `gh`
CLI is installed and authenticated — offers to set the 4 GitHub secrets
directly; otherwise it prints them for you to paste into Settings → Secrets
and variables → Actions. See `deploy/README.md` for why this one file is a
script instead of a Terraform resource, unlike Argo CD's own install above.

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
