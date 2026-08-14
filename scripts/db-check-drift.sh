#!/usr/bin/env bash
# Fails if a service's EF model has moved on without a migration to match.
#
#   ./scripts/db-check-drift.sh              # all eight
#   ./scripts/db-check-drift.sh catalog      # just one
#
# This is the one real weakness in the migrations setup (ADR-0029): the model is the source of
# truth, migrations are generated from it, and nothing otherwise stops a model change merging
# without the migration that carries it into a real database. The drift only surfaces later, as a
# deploy that fails or — worse — succeeds against a schema that no longer matches the code.
#
# A service with NO Migrations/ directory is skipped, not failed. "Never migrated" and "migrated,
# then the model moved on" are different states and only the second is drift; `dotnet ef
# migrations has-pending-model-changes` reports both identically, so the distinction is made here.
# A service is covered by this guard from the moment its first migration is committed, with no
# further change needed.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

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

if ! dotnet ef --version >/dev/null 2>&1; then
  echo "==> dotnet-ef not found. Install it once with:" >&2
  echo "    dotnet tool install --global dotnet-ef" >&2
  exit 1
fi

drifted=()
skipped=()

for svc in "${services[@]}"; do
  ns="${projects[$svc]:-}"
  if [ -z "$ns" ]; then
    echo "==> unknown service '$svc'" >&2
    exit 1
  fi

  project="services/$svc/$ns.Infrastructure"

  if [ ! -d "$project/Migrations" ]; then
    echo "==> $svc: no migrations yet — skipping (nothing to compare the model against)"
    skipped+=("$svc")
    continue
  fi

  echo "==> $svc: checking for pending model changes"
  # Infrastructure is both --project and --startup-project, same as db-add-migration.sh: its
  # IDesignTimeDbContextFactory builds the context without starting the API host.
  if dotnet ef migrations has-pending-model-changes \
    --project "$project" \
    --startup-project "$project" >/dev/null 2>&1; then
    echo "    up to date"
  else
    echo "    DRIFT: the model has changes with no migration"
    drifted+=("$svc")
  fi
done

echo
if [ ${#skipped[@]} -gt 0 ]; then
  echo "==> Skipped (no migrations committed yet): ${skipped[*]}"
fi

if [ ${#drifted[@]} -gt 0 ]; then
  echo "==> Model drift in: ${drifted[*]}" >&2
  echo "    Generate the missing migration and commit it alongside the model change:" >&2
  for svc in "${drifted[@]}"; do
    echo "      ./scripts/db-add-migration.sh <Name> $svc" >&2
  done
  exit 1
fi

echo "==> No model drift."
