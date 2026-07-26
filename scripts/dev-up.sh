#!/usr/bin/env bash
# One-click local dev startup: backing services (Docker) + all five EventPlatform
# services with their Dapr sidecars (Dapr multi-app run) — a single command,
# a single terminal, Ctrl+C stops everything.
#
# Usage: ./scripts/dev-up.sh
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

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

echo "==> Starting all five services with their Dapr sidecars (Ctrl+C stops everything)..."
echo "    Catalog    http://localhost:5080/scalar/v1"
echo "    Inventory  http://localhost:5081/scalar/v1"
echo "    Ordering   http://localhost:5082/scalar/v1"
echo "    Payments   http://localhost:5083/scalar/v1"
echo "    Ticketing  http://localhost:5084/scalar/v1"
echo "    Jaeger UI  http://localhost:16686"
echo "    Mint a dev auth token in another terminal: ./scripts/dev-token.sh"
echo

exec dapr run -f "$repo_root/platform/dapr/dapr.yaml"
