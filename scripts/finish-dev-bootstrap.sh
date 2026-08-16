#!/usr/bin/env bash
# Everything terraform apply can't do by itself for infra/environments/dev:
# fill deploy/overlays/dev/keyvault-secretproviderclass.yaml's placeholders
# from terraform output and commit that one file, then (optionally) push the
# matching GitHub Actions secrets via the gh CLI. Deliberately NOT folded
# into terraform apply itself - see that file's SecretProviderClass note in
# deploy/README.md for why (deploy/ is GitOps-owned, not infra-owned).
#
# Usage: ./scripts/finish-dev-bootstrap.sh
# Prerequisite: terraform apply already completed in infra/environments/dev.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
tf_dir="$repo_root/infra/environments/dev"
spc_file="$repo_root/deploy/overlays/dev/keyvault-secretproviderclass.yaml"
ingress_file="$repo_root/deploy/overlays/dev/ingress.yaml"

cd "$tf_dir"
if [ -z "$(terraform state list 2>/dev/null)" ]; then
  echo "==> No Terraform state found in $tf_dir — run 'terraform apply' there first."
  exit 1
fi

echo "==> Reading terraform output..."
client_id="$(terraform output -raw aks_key_vault_secrets_provider_client_id)"
kv_name="$(terraform output -raw key_vault_name)"
tenant_id="$(terraform output -raw aks_tenant_id)"
github_client_id="$(terraform output -raw github_actions_client_id)"
subscription_id="$(terraform output -raw subscription_id)"
acr_login_server="$(terraform output -raw acr_login_server)"
gateway_hostname="$(terraform output -raw gateway_hostname)"
azure_ingress_fqdn="$(terraform output -raw azure_ingress_fqdn)"
cd "$repo_root"

echo "==> Filling in $spc_file"
sed -i.bak \
  -e "s/REPLACE_ME_AKS_KV_SECRETS_PROVIDER_CLIENT_ID/${client_id}/" \
  -e "s/REPLACE_ME_KEY_VAULT_NAME/${kv_name}/" \
  -e "s/REPLACE_ME_TENANT_ID/${tenant_id}/" \
  "$spc_file"
rm -f "$spc_file.bak"

echo "==> Filling in $ingress_file"
sed -i.bak \
  -e "s/REPLACE_ME_GATEWAY_HOST/${gateway_hostname}/g" \
  "$ingress_file"
rm -f "$ingress_file.bak"

if git -C "$repo_root" diff --quiet -- "$spc_file" "$ingress_file"; then
  echo "==> No changes (already filled in, or values match what's committed)."
else
  echo "==> Committing $spc_file and $ingress_file"
  git -C "$repo_root" add "$spc_file" "$ingress_file"
  git -C "$repo_root" commit -m "chore(deploy): fill Key Vault and ingress host values for dev"
  git -C "$repo_root" push
fi

echo
echo "==> Gateway will be served at: https://${gateway_hostname}"
if [ "$gateway_hostname" != "$azure_ingress_fqdn" ]; then
  echo "    Custom domain in use - point its CNAME at ${azure_ingress_fqdn} BEFORE Argo CD syncs,"
  echo "    or cert-manager's HTTP-01 challenge will fail until it resolves."
fi
echo "    The certificate is issued on first sync and takes a minute or two; until then the"
echo "    browser sees the ingress controller's self-signed default and warns. Check progress with:"
echo "      kubectl describe certificate -n eventplatform-dev gateway-tls"

echo
echo "==> GitHub Actions repository secrets needed by .github/workflows/cd.yml:"
echo "    AZURE_CLIENT_ID       = ${github_client_id}"
echo "    AZURE_TENANT_ID       = ${tenant_id}"
echo "    AZURE_SUBSCRIPTION_ID = ${subscription_id}"
echo "    ACR_LOGIN_SERVER      = ${acr_login_server}"

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  read -r -p "==> gh CLI is authenticated - set these 4 secrets on this repo now? [y/N] " confirm
  if [ "$confirm" = "y" ] || [ "$confirm" = "Y" ]; then
    gh secret set AZURE_CLIENT_ID --body "$github_client_id"
    gh secret set AZURE_TENANT_ID --body "$tenant_id"
    gh secret set AZURE_SUBSCRIPTION_ID --body "$subscription_id"
    gh secret set ACR_LOGIN_SERVER --body "$acr_login_server"
    echo "==> Secrets set."
  else
    echo "==> Skipped - set them by hand in Settings -> Secrets and variables -> Actions."
  fi
else
  echo "==> gh CLI not found/authenticated - set the 4 values above by hand in"
  echo "    Settings -> Secrets and variables -> Actions."
fi

echo
echo "==> Done. Push any commit and .github/workflows/cd.yml will build, push,"
echo "    and deploy via Argo CD from here on."
