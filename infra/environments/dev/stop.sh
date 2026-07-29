#!/usr/bin/env bash
# Deallocates the dev environment's billable compute to save cost when it's not
# in use, WITHOUT destroying anything — cluster/server config, data, and
# Terraform state are all untouched. Run ./start.sh later to bring it back.
#
# Stops billing for: AKS node VMs, Postgres compute.
# Keeps billing (no stop/pause capability in Azure): the AKS Standard Load
# Balancer + public IP, Redis Basic C0, ACR, Key Vault — see infra/README.md's
# cost table. For a longer pause, `terraform destroy` + `terraform apply`
# later gets you to zero instead.
#
# Usage: ./stop.sh   (run from infra/environments/dev, after a successful apply)
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

resource_group="$(terraform output -raw resource_group_name)"
aks_name="$(terraform output -raw aks_cluster_name)"
postgres_fqdn="$(terraform output -raw postgres_fqdn)"
postgres_name="${postgres_fqdn%%.*}"

echo "==> Stopping AKS cluster '$aks_name' (deallocates node VMs)..."
az aks stop --resource-group "$resource_group" --name "$aks_name"

echo "==> Stopping Postgres Flexible Server '$postgres_name'..."
az postgres flexible-server stop --resource-group "$resource_group" --name "$postgres_name"

echo "==> Done. Redis, the Load Balancer/public IP, ACR, and Key Vault keep billing"
echo "    (no stop capability) — see infra/README.md's cost table."
echo "    Note: Azure auto-resumes a stopped Postgres server after 7 days regardless."
echo "    Run ./start.sh to resume."
