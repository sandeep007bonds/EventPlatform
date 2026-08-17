#!/usr/bin/env bash
# Build every image and push it to this environment's ACR, then point deploy/overlays/dev at what
# was just pushed. The local equivalent of .github/workflows/cd.yml's build-and-push +
# update-manifests jobs, for when CD cannot run.
#
#   ./scripts/push-images.sh                     # build and push all 11 images
#   ./scripts/push-images.sh catalog gateway     # only the ones you name
#   ./scripts/push-images.sh --manifest-only     # skip building; just repoint the overlay at
#                                                # images already in ACR for this commit
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

# --- 0. arguments -------------------------------------------------------------
# --manifest-only exists because the build and the manifest bump are separate failure domains:
# the builds can all succeed and the rewrite still fail (it did, on a machine with no python3),
# leaving eleven good images in ACR and an overlay still pointing at placeholders. Rebuilding
# eleven images to redo a text edit is a waste of twenty minutes.
manifest_only=false
args=""
for arg in "$@"; do
  case "$arg" in
    --manifest-only) manifest_only=true ;;
    -*) die "Unknown option: ${arg}" ;;
    *) args="${args} ${arg}" ;;
  esac
done

# --- 1. tools -----------------------------------------------------------------
command -v az >/dev/null 2>&1 || die "Azure CLI not found."
command -v terraform >/dev/null 2>&1 || die "Terraform not found (needed to read the ACR name)."
if [ "$manifest_only" = false ]; then
  command -v docker >/dev/null 2>&1 || die "Docker not found."
  docker info >/dev/null 2>&1 || die "Docker is installed but not running - start Docker Desktop."
fi

# --- 2. which services --------------------------------------------------------
services="${args:-$all_services}"
for svc in $services; do
  [ -n "${project_path[$svc]+set}" ] || die "Unknown service '${svc}'. Known: ${all_services}"
done

# --- 3. where to push ---------------------------------------------------------
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
if [ "$manifest_only" = true ]; then
  echo "    Mode     : manifest-only (no build, no push)"
fi
echo
if [ "$manifest_only" = true ]; then
  read -r -p "Point the overlay at these images? [y/N] " confirm
else
  read -r -p "Build and push these? [y/N] " confirm
fi
[ "$confirm" = "y" ] || [ "$confirm" = "Y" ] || die "Nothing changed."

# --- 4. build + push ----------------------------------------------------------
built="$services"
if [ "$manifest_only" = true ]; then
  say "Skipping build - assuming these are already in ACR at ${tag:0:7}"
else
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
fi

# --- 5. point the overlay at what was just pushed -----------------------------
# Only the services actually built, for the same reason cd.yml restricts its bump: rewriting a tag
# for a service whose image was never pushed for this commit points it at something that is not there.
kustomization="deploy/overlays/dev/kustomization.yaml"
say "Updating ${kustomization}"
for svc in $built; do
  # sed, not python: Git Bash on Windows has no python3 (the `python3` there is Microsoft Store's
  # install-prompt stub, which exits non-zero and takes the script with it), and this script has to
  # run on the machine that has Docker.
  #
  # The range address is what keeps this safe: `/- name: <svc>-placeholder/,+2` limits both
  # substitutions to that entry's own three lines, so rewriting `media` cannot touch another entry
  # that happens to contain the same substring. A bare s|newName: .*| would rewrite all eleven.
  grep -q -- "- name: ${svc}-placeholder" "$kustomization" \
    || die "No image entry named ${svc}-placeholder in ${kustomization}."
  sed -i.bak \
    "/- name: ${svc}-placeholder/,+2{s|newName: .*|newName: ${acr}/${svc}|;s|newTag: .*|newTag: ${tag}|;}" \
    "$kustomization"
  rm -f "${kustomization}.bak"
  echo "    ${svc} -> ${acr}/${svc}:${tag}"
done

# Fail loudly rather than committing a manifest that still cannot be pulled - a leftover
# REPLACE_ME is an InvalidImageName at deploy time, which reads as a manifest bug rather than a
# skipped rewrite.
for svc in $built; do
  if grep -A2 -- "- name: ${svc}-placeholder" "$kustomization" | grep -q REPLACE_ME; then
    die "${svc} still has a REPLACE_ME after the rewrite - not committing. Check ${kustomization}."
  fi
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
