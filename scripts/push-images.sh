#!/usr/bin/env bash
# Build every image and push it to this environment's ACR, then point deploy/overlays/dev at what
# was just pushed. The local equivalent of .github/workflows/cd.yml's build-and-push +
# update-manifests jobs, for when CD cannot run.
#
#   ./scripts/push-images.sh              # build and push all 11 images
#   ./scripts/push-images.sh catalog api  # only the ones you name
#
# You need this when GitHub Actions has no identity to push with - which is the normal situation
# on a subscription where you lack Microsoft Entra ID directory permissions and therefore applied
# with enable_github_oidc = false (see infra/README.md). It is also the fastest way to get a first
# deploy onto a brand-new cluster without waiting on a CI round trip.
#
# This is deliberately NOT a replacement for CD. It tags images with your working tree's HEAD, so
# an image can end up in ACR that no commit on the remote ever produced. Push your branch first
# (this script warns if you have not) and treat it as a bootstrap/debug path, not routine.
set -euo pipefail
set -E
trap 'printf "\nERROR: failed at line %s: %s\n" "$LINENO" "$BASH_COMMAND" >&2' ERR

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

say() { printf '\n==> %s\n' "$1"; }
die() { printf '\nERROR: %s\n' "$1" >&2; exit 1; }

# service -> csproj. Empty means "brings its own Dockerfile and context" (the SPA) - the same
# convention cd.yml uses, kept identical on purpose so the two cannot drift in meaning.
declare -A project_path=(
  [catalog]="services/catalog/Catalog.Api/Catalog.Api.csproj"
  [inventory]="services/inventory/Inventory.Api/Inventory.Api.csproj"
  [ordering]="services/ordering/Ordering.Api/Ordering.Api.csproj"
  [payments]="services/payments/Payments.Api/Payments.Api.csproj"
  [ticketing]="services/ticketing/Ticketing.Api/Ticketing.Api.csproj"
  [communication]="services/communication/Communication.Api/Communication.Api.csproj"
  [media]="services/media/Media.Api/Media.Api.csproj"
  [identity]="services/identity/Identity.Api/Identity.Api.csproj"
  [queue]="services/queue/Queue.Api/Queue.Api.csproj"
  [gateway]="gateways/EventPlatform.Gateway/EventPlatform.Gateway.csproj"
  [frontend]=""
)
declare -A assembly_name=(
  [catalog]="Catalog.Api"          [inventory]="Inventory.Api"
  [ordering]="Ordering.Api"        [payments]="Payments.Api"
  [ticketing]="Ticketing.Api"      [communication]="Communication.Api"
  [media]="Media.Api"              [identity]="Identity.Api"
  [queue]="Queue.Api"              [gateway]="EventPlatform.Gateway"
  [frontend]=""
)
all_services="catalog inventory ordering payments ticketing communication media identity queue gateway frontend"

# --- 0. tools -----------------------------------------------------------------
command -v az >/dev/null 2>&1 || die "Azure CLI not found."
command -v docker >/dev/null 2>&1 || die "Docker not found."
docker info >/dev/null 2>&1 || die "Docker is installed but not running - start Docker Desktop."
command -v terraform >/dev/null 2>&1 || die "Terraform not found (needed to read the ACR name)."

# --- 1. which services --------------------------------------------------------
services="${*:-$all_services}"
for svc in $services; do
  [ -n "${project_path[$svc]+set}" ] || die "Unknown service '${svc}'. Known: ${all_services}"
done

# --- 2. where to push ---------------------------------------------------------
env_dir="infra/environments/dev"
acr="$(terraform -chdir="$env_dir" output -raw acr_login_server 2>/dev/null)" \
  || die "Could not read acr_login_server. Run ./scripts/provision-azure.sh dev first."
[ -n "$acr" ] || die "acr_login_server is empty."

# The tag is the working tree's HEAD, matching how cd.yml tags by commit SHA. Warn - do not block -
# on a dirty tree or unpushed commits: the image would then correspond to source nobody else can
# check out, which is fine while debugging and a trap if forgotten.
tag="$(git rev-parse HEAD)"
if ! git diff --quiet || ! git diff --cached --quiet; then
  say "WARNING: working tree has uncommitted changes."
  echo "    Images will be tagged ${tag:0:7}, which does NOT contain them. Commit first if that matters."
fi
if [ -n "$(git log --branches --not --remotes --oneline 2>/dev/null)" ]; then
  say "WARNING: you have commits that are not on the remote."
  echo "    Argo CD syncs from the remote, so push before expecting it to pick these images up."
fi

say "Registry : ${acr}"
echo "    Tag      : ${tag}"
echo "    Services : ${services}"
echo
read -r -p "Build and push these? [y/N] " confirm
[ "$confirm" = "y" ] || [ "$confirm" = "Y" ] || die "Nothing built."

# --- 3. build + push ----------------------------------------------------------
az acr login --name "${acr%%.*}" || die "Could not log in to ${acr}. Do you have AcrPush on it?"

built=""
for svc in $services; do
  image="${acr}/${svc}:${tag}"
  say "Building ${svc}"
  if [ -n "${project_path[$svc]}" ]; then
    # Every .NET service and the gateway share the parameterised repo-root Dockerfile.
    docker build -f Dockerfile \
      --build-arg PROJECT_PATH="${project_path[$svc]}" \
      --build-arg ASSEMBLY_NAME="${assembly_name[$svc]}" \
      -t "$image" .
  else
    # The SPA has its own Dockerfile and context. VITE_GATEWAY_BASE_URL is deliberately NOT passed:
    # unset makes the app call its own origin, which is what keeps one image valid for every
    # environment (ADR-0033). An unset publishable key still builds.
    docker build -f frontend/Dockerfile \
      --build-arg VITE_STRIPE_PUBLISHABLE_KEY="${VITE_STRIPE_PUBLISHABLE_KEY:-}" \
      -t "$image" frontend
  fi
  docker push "$image"
  built="${built} ${svc}"
done

# --- 4. point the overlay at what was just pushed -----------------------------
# Only the services actually built, for the same reason cd.yml restricts its bump: rewriting a tag
# for a service whose image was never pushed for this commit points it at something that is not there.
kustomization="deploy/overlays/dev/kustomization.yaml"
say "Updating ${kustomization}"
for svc in $built; do
  python3 - "$kustomization" "$svc" "$acr" "$tag" <<'PY'
import re, sys
path, svc, acr, tag = sys.argv[1:5]
text = open(path, encoding="utf-8").read()
# Rewrite only this service's three-line entry, anchored on its placeholder name, so a shared
# substring (e.g. "media" inside another value) can never rewrite the wrong block.
pattern = re.compile(
    rf"(- name: {re.escape(svc)}-placeholder\n\s+newName: )\S+(\n\s+newTag: )\S+"
)
new, n = pattern.subn(rf"\g<1>{acr}/{svc}\g<2>{tag}", text)
if n != 1:
    sys.exit(f"expected exactly 1 image entry for {svc}, found {n}")
open(path, "w", encoding="utf-8", newline="\n").write(new)
PY
  echo "    ${svc} -> ${acr}/${svc}:${tag}"
done

if git diff --quiet -- "$kustomization"; then
  say "Overlay already pointed at these images - nothing to commit."
else
  say "Committing the image tags"
  git add "$kustomization"
  git commit -m "chore(deploy): point dev overlay at locally-built images ${tag:0:7}"
  echo
  echo "    Push it - Argo CD reconciles from the remote, not your working tree:"
  echo "      git push"
fi

say "Done."
echo "    Watch the rollout with:"
echo "      kubectl get pods -n eventplatform-dev -w"
