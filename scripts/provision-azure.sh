#!/usr/bin/env bash
# One command to stand up an environment on Azure: log in, choose the subscription, choose the
# environment, bootstrap remote state if it does not exist yet, then plan and (on confirmation)
# apply.
#
#   ./scripts/provision-azure.sh            # prompts for everything
#   ./scripts/provision-azure.sh dev        # skips the environment prompt
#
# This replaces a multi-step README dance whose steps had to be done in the right order with values
# copied between them by hand. Two of this repo's real incidents came from exactly that: Terraform
# state created in whichever subscription the CLI happened to default to, and backend-config values
# transcribed wrongly.
#
# It NEVER applies without showing you the plan first and reading a typed confirmation. Infra here
# is billable and not always reversible, so "run the script and walk away" is not a mode this
# offers on purpose.
#
# Safe to re-run: existing bootstrap state, tfvars and init are detected and reused rather than
# recreated.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

say() { printf '\n==> %s\n' "$1"; }
die() { printf '\nERROR: %s\n' "$1" >&2; exit 1; }

confirm() {
  local prompt="$1" answer
  read -r -p "${prompt} [y/N] " answer
  [ "$answer" = "y" ] || [ "$answer" = "Y" ]
}

# --- 0. tools ---------------------------------------------------------------
command -v az >/dev/null 2>&1 || die "Azure CLI not found. Install it: https://aka.ms/azure-cli"
command -v terraform >/dev/null 2>&1 || die "Terraform not found. Install >= 1.9: https://terraform.io/downloads"

# --- 1. environment ---------------------------------------------------------
environment="${1:-}"
available="$(find infra/environments -mindepth 1 -maxdepth 1 -type d -exec basename {} \; | sort)"
if [ -z "$environment" ]; then
  say "Environments available:"
  echo "$available" | sed 's/^/    /'
  read -r -p "Which environment? " environment
fi
env_dir="infra/environments/${environment}"
[ -d "$env_dir" ] || die "No such environment: ${environment}. Available: $(echo "$available" | tr '\n' ' ')"

# --- 2. login ---------------------------------------------------------------
if ! az account show >/dev/null 2>&1; then
  say "Not logged in to Azure — opening a browser."
  az login >/dev/null
fi

# --- 3. subscription --------------------------------------------------------
# Asked every time, never inherited silently. A machine signed into a personal and a work tenant
# will happily default to the wrong one, and Terraform would create this environment there without
# a word about it.
say "Subscriptions on this account:"
az account list --query '[].{Name:name, Id:id, Default:isDefault}' --output table

current_sub="$(az account show --query id --output tsv)"
current_name="$(az account show --query name --output tsv)"
read -r -p "Subscription ID [${current_sub} — ${current_name}]: " subscription_id
subscription_id="${subscription_id:-$current_sub}"

az account set --subscription "$subscription_id" \
  || die "Could not select subscription ${subscription_id}."
say "Using: $(az account show --query name --output tsv) (${subscription_id})"

# --- 4. bootstrap (remote state) --------------------------------------------
# The one config that cannot use remote state, because it is what creates it.
#
# Local state is checked against the SELECTED subscription, not merely for existence. Bootstrap
# applied before subscription_id was required could have landed in whatever the CLI defaulted to at
# the time, and its state file would still name resources here that live in another tenant
# entirely — or nowhere, if someone deleted them. Trusting the file alone turns that into a
# confusing `terraform init` 404 several steps later.
bootstrap_usable=false
if [ -f infra/bootstrap/terraform.tfstate ] \
   && terraform -chdir=infra/bootstrap output -raw storage_account_name >/dev/null 2>&1; then
  stale_rg="$(terraform -chdir=infra/bootstrap output -raw resource_group_name)"
  stale_sa="$(terraform -chdir=infra/bootstrap output -raw storage_account_name)"

  if az storage account show --name "$stale_sa" --resource-group "$stale_rg" \
       --subscription "$subscription_id" >/dev/null 2>&1; then
    bootstrap_usable=true
    say "Bootstrap state already exists and its resources are present — reusing."
  else
    say "Local bootstrap state names ${stale_sa} (rg ${stale_rg}), which does NOT exist in this"
    echo "    subscription. Most likely it was applied against a different one before"
    echo "    subscription_id became required, so the real resources live in another tenant."
    echo
    echo "    If any environment already has state in that account, DO NOT re-bootstrap here —"
    echo "    log in to that subscription instead, or you will lose track of live resources."
    echo "    If nothing has been applied yet, re-bootstrapping is safe and cheap."
    echo
    if confirm "Re-bootstrap into ${subscription_id}? (the old state file is kept, renamed)"; then
      mv infra/bootstrap/terraform.tfstate \
         "infra/bootstrap/terraform.tfstate.orphaned-$(date +%Y%m%d%H%M%S)"
    else
      die "Stopped. Select the subscription holding ${stale_sa}, or remove the stale state by hand."
    fi
  fi
fi

if [ "$bootstrap_usable" = false ]; then
  say "Creating the storage account that holds every environment's Terraform state."
  echo "    Applied once, never destroyed."
  confirm "Create bootstrap resources in ${subscription_id}?" || die "Stopped before bootstrap."

  terraform -chdir=infra/bootstrap init -input=false
  terraform -chdir=infra/bootstrap apply -var="subscription_id=${subscription_id}"
fi

state_rg="$(terraform -chdir=infra/bootstrap output -raw resource_group_name)"
state_sa="$(terraform -chdir=infra/bootstrap output -raw storage_account_name)"
state_container="$(terraform -chdir=infra/bootstrap output -raw container_name)"
say "Remote state: ${state_sa}/${state_container} (rg ${state_rg})"

# --- 5. tfvars --------------------------------------------------------------
# Gitignored (.gitignore excludes *.tfvars but keeps *.tfvars.example), which is why the password
# and contact address can live here.
tfvars="${env_dir}/terraform.tfvars"
if [ -f "$tfvars" ]; then
  say "Using existing ${tfvars} — edit it by hand to change anything."

  # The subscription chosen above only sets the CLI's context; Terraform reads var.subscription_id.
  # An existing tfvars written against a different subscription silently wins over the prompt, so
  # the environment would land somewhere other than where this run said it would.
  tfvars_sub="$(grep -E '^[[:space:]]*subscription_id[[:space:]]*=' "$tfvars" \
    | tail -1 | sed 's/.*=[[:space:]]*"\(.*\)".*/\1/')"
  if [ -n "$tfvars_sub" ] && [ "$tfvars_sub" != "$subscription_id" ]; then
    say "MISMATCH: ${tfvars} sets subscription_id = ${tfvars_sub}"
    echo "    but this run selected              ${subscription_id}"
    echo
    echo "    Terraform uses the tfvars value, not the one selected above, so leaving this alone"
    echo "    deploys into ${tfvars_sub}."
    echo
    if confirm "Rewrite ${tfvars} to use ${subscription_id}?"; then
      sed -i.bak "s|^[[:space:]]*subscription_id[[:space:]]*=.*|subscription_id                 = \"${subscription_id}\"|" "$tfvars"
      rm -f "${tfvars}.bak"
      say "Updated."
    else
      die "Stopped. Fix ${tfvars} by hand, or re-run and select ${tfvars_sub}."
    fi
  fi
else
  say "Creating ${tfvars}"
  cp "${env_dir}/terraform.tfvars.example" "$tfvars"

  read -r -p "Let's Encrypt contact email (for TLS expiry notices): " letsencrypt_email
  [ -n "$letsencrypt_email" ] || die "letsencrypt_email is required — certificate issuance needs it."

  # Generated rather than prompted: it is never typed again (Terraform writes it to Key Vault), so
  # a memorable one buys nothing and a weak one is a real exposure on a server with a public
  # firewall rule.
  pg_password="$(LC_ALL=C tr -dc 'A-Za-z0-9!#%*+-=?' </dev/urandom | head -c 32)"

  {
    echo ""
    echo "# --- written by scripts/provision-azure.sh ---"
    echo "subscription_id                 = \"${subscription_id}\""
    echo "letsencrypt_email               = \"${letsencrypt_email}\""
    echo "postgres_administrator_password = \"${pg_password}\""
  } >> "$tfvars"

  say "Generated a random Postgres admin password into ${tfvars} (gitignored)."
  echo "    Terraform also stores it in Key Vault, so you never need to read it from here."
fi

# --- 6. init + plan ---------------------------------------------------------
say "terraform init (backend config from bootstrap outputs)"
terraform -chdir="$env_dir" init -input=false -reconfigure \
  -backend-config="resource_group_name=${state_rg}" \
  -backend-config="storage_account_name=${state_sa}" \
  -backend-config="container_name=${state_container}"

say "terraform plan — read this before answering the next question"
terraform -chdir="$env_dir" plan -input=false -out=tfplan

# --- 7. apply, only on an explicit yes --------------------------------------
echo
echo "The plan above is what will be created, changed or destroyed in:"
echo "    subscription : $(az account show --query name --output tsv) (${subscription_id})"
echo "    environment  : ${environment}"
echo
if confirm "Apply it?"; then
  terraform -chdir="$env_dir" apply -input=false tfplan
else
  rm -f "${env_dir}/tfplan"
  die "Not applied. Nothing was changed."
fi
rm -f "${env_dir}/tfplan"

# --- 8. what Terraform cannot do for itself ---------------------------------
say "Infrastructure is up. Finishing the GitOps wiring."
echo "    scripts/finish-dev-bootstrap.sh fills deploy/overlays/${environment}'s Key Vault and"
echo "    ingress-host placeholders from the outputs, commits them, and offers to set the"
echo "    GitHub Actions secrets that let CD push images."
echo
if confirm "Run scripts/finish-dev-bootstrap.sh now?"; then
  ./scripts/finish-dev-bootstrap.sh
else
  echo "    Skipped. Run it before expecting Argo CD to sync anything."
fi

say "Done."
echo "    Gateway URL : https://$(terraform -chdir="$env_dir" output -raw gateway_hostname)"
echo "    Cluster     : az aks get-credentials --resource-group $(terraform -chdir="$env_dir" output -raw resource_group_name) --name $(terraform -chdir="$env_dir" output -raw aks_cluster_name)"
echo
echo "    The certificate takes a minute or two to issue on first sync; until then the browser"
echo "    warns about the ingress controller's self-signed default. Check progress with:"
echo "      kubectl describe certificate -n eventplatform-${environment} gateway-tls"
