#!/usr/bin/env bash
# Applies pending EF Core migrations to every service database.
#
#   ./scripts/db-migrate.sh                 # all eight
#   ./scripts/db-migrate.sh catalog         # just one
#
# This runs each service with `--migrate`: the service applies its migrations, logs what it did, and
# exits without serving anything. It is the same entry point a deployed environment uses (there, an
# Argo CD PreSync job), so the path that runs in production is the path exercised locally — no
# separate dev-only schema mechanism to drift (ADR-0029).
#
# Safe to re-run: EF skips migrations already recorded in the history table.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# Each service keeps its local connection string in appsettings.Development.json, which only loads
# when the environment is Development — and the `--no-launch-profile` below deliberately skips the
# launch profile that would otherwise set it (a schema-only run must not bind ports or open a
# browser). Without this the run lands in Production, finds no connection string at all, and dies
# with "The ConnectionString property has not been initialized". Overridable for a non-local
# database.
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

declare -A projects=(
  [catalog]="Catalog"
  [inventory]="Inventory"
  [ordering]="Ordering"
  [payments]="Payments"
  [ticketing]="Ticketing"
  [communication]="Communication"
  [identity]="Identity"
  [queue]="Queue"
)

services=("$@")
if [ ${#services[@]} -eq 0 ]; then
  services=(catalog inventory ordering payments ticketing communication identity queue)
fi

for svc in "${services[@]}"; do
  ns="${projects[$svc]:-}"
  if [ -z "$ns" ]; then
    echo "==> unknown service '$svc'" >&2
    exit 1
  fi

  echo "==> $svc: applying migrations"
  dotnet run --project "services/$svc/$ns.Api" --no-launch-profile -- --migrate
done

echo
echo "==> All databases up to date."
