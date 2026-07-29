#!/usr/bin/env bash
# Stops the local backing services. Ctrl+C on ./scripts/dev-up.sh already stops all six
# services and their Dapr sidecars — this just tears down Docker (Postgres, Redis, Jaeger).
#
# Usage:
#   ./scripts/dev-down.sh        # stop containers, keep data
#   ./scripts/dev-down.sh -v     # stop containers and wipe the Postgres volume
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [ "${1:-}" = "-v" ]; then
  echo "==> Stopping backing services and wiping the Postgres volume..."
  docker compose down -v
else
  echo "==> Stopping backing services (data preserved; use -v to wipe it)..."
  docker compose down
fi
