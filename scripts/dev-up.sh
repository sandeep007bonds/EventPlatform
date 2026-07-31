#!/usr/bin/env bash
# One-click local dev startup: backing services (Docker) + all six EventPlatform
# services with their Dapr sidecars + the gateway and Media.Api (neither uses
# Dapr) — a single command, a single terminal, Ctrl+C stops everything.
#
# Usage: ./scripts/dev-up.sh
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# The local Dapr secret store reads this file (git-ignored; only local dummy
# values belong here, per local-development.md). Auto-create it so first-run
# doesn't fail on a missing file.
if [ ! -f "$repo_root/platform/dapr/secrets.local.json" ]; then
  echo "==> Creating platform/dapr/secrets.local.json from the example (local dummy values only)..."
  cp "$repo_root/platform/dapr/secrets.local.example.json" "$repo_root/platform/dapr/secrets.local.json"
fi

echo "==> Starting backing services (Postgres, Redis, Jaeger)..."
docker compose up -d

wait_for() {
  local name="$1" cmd="$2" tries=30
  echo -n "==> Waiting for $name..."
  until eval "$cmd" >/dev/null 2>&1; do
    tries=$((tries - 1))
    if [ "$tries" -le 0 ]; then
      echo " timed out."
      echo "    '$name' did not become ready. Check 'docker compose logs $name'."
      exit 1
    fi
    echo -n "."
    sleep 1
  done
  echo " ready."
}

wait_for postgres "docker compose exec -T postgres pg_isready -U eventplatform"
wait_for redis "docker compose exec -T redis redis-cli ping | grep -q PONG"

if ! command -v dapr >/dev/null 2>&1; then
  echo "==> Dapr CLI not found. Install it: https://docs.dapr.io/getting-started/install-dapr-cli/"
  exit 1
fi

dapr_version="$(dapr --version 2>/dev/null | awk '/CLI version/ {print $3}')"
echo "==> Dapr CLI version: ${dapr_version:-unknown} (multi-app run needs >= 1.13)"

dapr_home="${DAPR_HOME:-$HOME/.dapr}"
if [ ! -x "$dapr_home/bin/daprd" ] && [ ! -x "$dapr_home/bin/daprd.exe" ]; then
  echo "==> Dapr runtime not initialized locally — running 'dapr init' (one-time, uses Docker)..."
  dapr init
fi

# Build once, up front. All six services (and the gateway) reference the same
# building-blocks projects (EventPlatform.Contracts, .Hosting, .Messaging);
# launching several concurrent `dotnet run`s without this can make two of them
# try to compile and write the same shared DLL at once, failing with CS2012
# ("cannot open ... for writing -- being used by another process"). Pre-building
# means each `dotnet run` below finds everything already up to date and doesn't
# race the others.
echo "==> Building the solution once (avoids a concurrent-build race on shared projects)..."
dotnet build "$repo_root/EventPlatform.slnx"

echo "==> Starting the gateway, Media.Api, all six services and their Dapr sidecars (Ctrl+C stops everything)..."
echo "    Gateway       http://localhost:5090/scalar/v1"
echo "    Catalog       http://localhost:5080/scalar/v1"
echo "    Inventory     http://localhost:5081/scalar/v1"
echo "    Ordering      http://localhost:5082/scalar/v1"
echo "    Payments      http://localhost:5083/scalar/v1"
echo "    Ticketing     http://localhost:5084/scalar/v1"
echo "    Communication http://localhost:5085/scalar/v1"
echo "    Media         http://localhost:5086/scalar/v1"
echo "    Jaeger UI     http://localhost:16686"
echo "    Mint a dev auth token in another terminal: ./scripts/dev-token.sh"
echo "    Or call the gateway's dev-login: POST http://localhost:5090/api/auth/dev-login"
echo

# The gateway is a plain HTTP proxy, not a Dapr participant — it never runs with
# a sidecar. Start it in the background here, and make sure it's cleaned up
# alongside everything else on exit (both branches below rely on this).
gateway_pid=""
start_gateway() {
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5090" \
    dotnet run --no-launch-profile --project "$repo_root/gateways/EventPlatform.Gateway" &
  gateway_pid="$!"
}

# Media.Api has no database and no pub/sub — it never runs with a Dapr sidecar
# either (see services/media/CLAUDE.md). Started the same way as the gateway.
media_pid=""
start_media() {
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5086" \
    dotnet run --no-launch-profile --project "$repo_root/services/media/Media.Api" &
  media_pid="$!"
}

case "${OSTYPE:-}" in
  msys*|cygwin*|win32*) is_windows=1 ;;
  *) is_windows=0 ;;
esac

if [ "$is_windows" -eq 0 ]; then
  start_gateway
  start_media

  cleanup_non_windows() {
    echo
    echo "==> Stopping the gateway and Media.Api..."
    kill "$gateway_pid" 2>/dev/null || true
    kill "$media_pid" 2>/dev/null || true
  }
  trap cleanup_non_windows EXIT INT TERM

  # Not exec'd (unlike earlier versions of this script) so the trap above still
  # runs to stop the gateway when this is interrupted.
  dapr run -f "$repo_root/platform/dapr/dapr.yaml"
  exit $?
fi

echo "==> Windows shell detected — starting each service as its own dapr run process"
echo "    (Dapr's multi-app run has a known issue spawning dotnet on Windows)."
echo

pids=()

start_service() {
  local app_id="$1" port="$2" project="$3"
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:$port" \
    dapr run --app-id "$app_id" --app-port "$port" --app-protocol http \
    --resources-path "$repo_root/platform/dapr/components" \
    --config "$repo_root/platform/dapr/config.yaml" \
    -- dotnet run --no-launch-profile --project "$project" &
  pids+=("$!")
}

cleanup() {
  echo
  echo "==> Stopping all services..."
  kill "$gateway_pid" 2>/dev/null || true
  kill "$media_pid" 2>/dev/null || true
  for pid in "${pids[@]}"; do
    kill "$pid" 2>/dev/null || true
  done
  wait 2>/dev/null || true
}
trap cleanup EXIT INT TERM

start_gateway
sleep 3
start_media
sleep 3

# Staggered: launching several dapr/dotnet processes at once on Windows can trip a
# separate, intermittent Windows job-object assignment race ("failed to assign
# process to job object"), which kills the app before it starts. A couple of
# seconds between launches gives each one time to finish registering before the
# next starts.
start_service catalog 5080 "$repo_root/services/catalog/Catalog.Api"
sleep 3
start_service inventory 5081 "$repo_root/services/inventory/Inventory.Api"
sleep 3
start_service ordering 5082 "$repo_root/services/ordering/Ordering.Api"
sleep 3
start_service payments 5083 "$repo_root/services/payments/Payments.Api"
sleep 3
start_service ticketing 5084 "$repo_root/services/ticketing/Ticketing.Api"
sleep 3
start_service communication 5085 "$repo_root/services/communication/Communication.Api"

wait
