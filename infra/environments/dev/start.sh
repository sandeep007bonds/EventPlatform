#!/usr/bin/env bash
# Resumes the dev environment's compute after ./stop.sh deallocated it.
#
# Usage: ./start.sh   (run from infra/environments/dev)
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

resource_group="$(terraform output -raw resource_group_name)"
aks_name="$(terraform output -raw aks_cluster_name)"
postgres_fqdn="$(terraform output -raw postgres_fqdn)"
postgres_name="${postgres_fqdn%%.*}"

echo "==> Starting Postgres Flexible Server '$postgres_name'..."
az postgres flexible-server start --resource-group "$resource_group" --name "$postgres_name"

echo "==> Starting AKS cluster '$aks_name'..."
az aks start --resource-group "$resource_group" --name "$aks_name"

echo "==> Done. Reconnect with:"
echo "    az aks get-credentials --resource-group $resource_group --name $aks_name"
