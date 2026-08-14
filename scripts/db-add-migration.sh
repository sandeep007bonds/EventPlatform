#!/usr/bin/env bash
# Adds an EF Core migration to every service that owns a database.
#
#   ./scripts/db-add-migration.sh InitialCreate
#   ./scripts/db-add-migration.sh AddEntryGates catalog inventory
#
# With no service names it runs all eight. Each service owns its own schema and its own migration
# history (database-per-service, ADR-0008), so a change that only touches Catalog produces a
# migration only in Catalog — pass the names you actually changed rather than generating eight
# empty migrations.
#
# Migrations are generated from the EF model, never hand-written: `migrations add` diffs the current
# IEntityTypeConfiguration set against the last migration, so the model stays the single source of
# truth (ADR-0029).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

name="${1:-}"
if [ -z "$name" ]; then
  echo "usage: $0 <MigrationName> [service ...]" >&2
  echo "       services: catalog inventory ordering payments ticketing communication identity queue" >&2
  exit 1
fi
shift

# service : Infrastructure project (where the model + migrations live) : startup project
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

for svc in "${services[@]}"; do
  ns="${projects[$svc]:-}"
  if [ -z "$ns" ]; then
    echo "==> unknown service '$svc'" >&2
    exit 1
  fi

  echo "==> $svc: adding migration '$name'"
  # The Infrastructure project is BOTH --project and --startup-project.
  #
  # It holds the DbContext, the EF configurations and Microsoft.EntityFrameworkCore.Design, and its
  # IDesignTimeDbContextFactory builds the context standalone — reading <SERVICE>_DB or falling back
  # to the local dev connection string. So the tools never need the API host, which is the point of
  # those factories: no Dapr sidecar, no outbox relay, no host startup just to diff a model.
  #
  # Pointing --startup-project at the Api instead would mean adding Design to all eight Api projects,
  # which the root CLAUDE.md warns against — its MSBuild/Roslyn tree drags in vulnerable transitives.
  dotnet ef migrations add "$name" \
    --project "services/$svc/$ns.Infrastructure" \
    --startup-project "services/$svc/$ns.Infrastructure" \
    --output-dir Migrations
done

echo
echo "==> Done. Review the generated Migrations/ files before committing —"
echo "    a migration is real DDL against real data, not generated noise."
