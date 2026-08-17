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
# Empty when enable_github_oidc = false (no directory permissions to create the
# app registration). Not fatal: the cluster is fully provisioned either way, only
# CD's push identity is missing. `|| true` because -raw on a null output errors.
github_client_id="$(terraform output -raw github_actions_client_id 2>/dev/null || true)"
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

  # A plain push fails whenever the branch moved on the remote since you last
  # pulled, which is easy to hit here - provisioning takes long enough that a
  # push from elsewhere lands in the meantime. Rebase and retry once rather
  # than aborting the whole bootstrap over it. Rebase, not merge: this commit
  # only touches two generated placeholder files, so there is nothing to
  # conflict with and no merge commit worth creating.
  branch="$(git -C "$repo_root" rev-parse --abbrev-ref HEAD)"
  if ! git -C "$repo_root" push -u origin "$branch"; then
    echo "==> Push rejected - the remote has commits this clone doesn't. Rebasing onto it."
    if git -C "$repo_root" pull --rebase origin "$branch" \
       && git -C "$repo_root" push -u origin "$branch"; then
      echo "==> Pushed after rebase."
    else
      # Deliberately not fatal: the infrastructure is already up and this file
      # is committed locally. Argo CD reconciles from the REMOTE, though, so
      # nothing syncs until this lands - say so rather than exiting silently.
      echo
      echo "    WARNING: could not push. The commit exists locally but Argo CD reads the"
      echo "    remote, so it will not sync until you resolve this and push:"
      echo "      git -C \"$repo_root\" pull --rebase origin ${branch} && git -C \"$repo_root\" push"
      echo
    fi
  fi
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
if [ -z "$github_client_id" ]; then
  echo "    AZURE_CLIENT_ID       = (not created - enable_github_oidc is false)"
  echo
  echo "    The OIDC identity CD logs in with does not exist, so image pushes will fail"
  echo "    until you give CD a push identity another way - see infra/README.md"
  echo "    (\"GitHub OIDC needs directory permissions\"). Everything else below is"
  echo "    still correct and still worth setting."
else
  echo "    AZURE_CLIENT_ID       = ${github_client_id}"
fi
echo "    AZURE_TENANT_ID       = ${tenant_id}"
echo "    AZURE_SUBSCRIPTION_ID = ${subscription_id}"
echo "    ACR_LOGIN_SERVER      = ${acr_login_server}"

if command -v gh >/dev/null 2>&1 && gh auth status >/dev/null 2>&1; then
  read -r -p "==> gh CLI is authenticated - set these 4 secrets on this repo now? [y/N] " confirm
  if [ "$confirm" = "y" ] || [ "$confirm" = "Y" ]; then
    if [ -n "$github_client_id" ]; then
      gh secret set AZURE_CLIENT_ID --body "$github_client_id"
    else
      echo "    Skipping AZURE_CLIENT_ID - no OIDC identity was created."
    fi
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
