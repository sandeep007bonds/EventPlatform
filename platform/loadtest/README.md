# Load tests (k6)

Two [k6](https://k6.io/) scripts that exercise the flash-sale hot path end-to-end
(Catalog → Inventory) over HTTP, the way real buyers hit it:

| Script | What it proves | The gate |
| --- | --- | --- |
| [`no-oversell.js`](no-oversell.js) | **Correctness.** Hundreds of users race for the **same single seat**. | `holds_succeeded: count<2` — exactly one hold may win, or the test fails. |
| [`throughput.js`](throughput.js) | **Capacity.** Many users each grab a **different** seat under sustained load. | `p95<250ms`, `p99<500ms`, `holds_failed rate<1%`. |

`no-oversell.js` is the headline: it's the automated proof of the platform's
core promise — **zero oversell under contention**. The fast gate is a Redis Lua
script; the final authority is Postgres optimistic concurrency (a `Version`
token). Both together must let exactly one hold through.

## Prerequisites

1. **k6** installed — <https://k6.io/docs/get-started/installation/>.
2. **The stack running locally** with Dapr, Postgres, and Redis, so that
   publishing an event in Catalog provisions inventory in Inventory (via Dapr
   pub/sub). At minimum you need **Catalog** and **Inventory**:

   ```bash
   # Catalog
   dapr run --app-id catalog \
     --resources-path platform/dapr/components --config platform/dapr/config.yaml \
     -- dotnet run --project services/catalog/Catalog.Api

   # Inventory
   dapr run --app-id inventory \
     --resources-path platform/dapr/components --config platform/dapr/config.yaml \
     -- dotnet run --project services/inventory/Inventory.Api
   ```

3. **Dev auth.** The scripts mint their own HS256 JWTs and sign them with
   `Jwt:DevSigningKey`, which is already set in each service's
   `appsettings.Development.json`. No identity provider is needed. This path is
   **Development-only** — production still uses OIDC (see
   `EventPlatform.Hosting/AuthenticationExtensions.cs`); the symmetric-key
   branch is only taken when `Jwt:DevSigningKey` is present, which it never is
   outside Development config or Key Vault.

## Running

```bash
# No-oversell (default 200 users fighting over 1 seat)
k6 run platform/loadtest/no-oversell.js
k6 run -e VUS=500 platform/loadtest/no-oversell.js

# Throughput (default 100 VUs for 1m over 10k seats)
k6 run platform/loadtest/throughput.js
k6 run -e VUS=300 -e DURATION=2m -e SEATS=20000 platform/loadtest/throughput.js
```

`no-oversell.js` prints a clear verdict at the end:

```
=================== NO-OVERSELL ===================
PASS — exactly one hold won the seat (no oversell)
held=1  conflicted=199  attempts=200
==================================================
```

k6 exits non-zero if any threshold fails, so both scripts drop straight into
CI or a pre-release gate.

## Configuration (env vars)

| Var | Default | Applies to | Meaning |
| --- | --- | --- | --- |
| `CATALOG_URL` | `https://localhost:7080` | both | Catalog base URL |
| `INVENTORY_URL` | `https://localhost:7081` | both | Inventory base URL |
| `DEV_SIGNING_KEY` | `eventplatform-dev-hs256-signing-key-not-a-secret` | both | must match `Jwt:DevSigningKey` |
| `TENANT_ID` | `11111111-1111-1111-1111-111111111111` | both | tenant claim (`tenant_id`) on minted tokens |
| `VUS` | `200` / `100` | both | concurrent virtual users |
| `DURATION` | `1m` | throughput | sustained-load duration |
| `SEATS` | `10000` | throughput | seat-map size to provision |

The scripts skip TLS verification (`insecureSkipTLSVerify`) so the dev
self-signed cert doesn't get in the way.

## How they drive the system

Both scripts do the same setup in `setup()` (once, before the load):

1. `POST /v1/events` — create a draft event.
2. `POST /v1/events/{id}/seatmap` — define the seats (1 for no-oversell, many
   for throughput).
3. `POST /v1/events/{id}/publish` — publish; Catalog emits `EventPublished`.
4. Poll `GET /v1/events/{id}/inventory` until Inventory has provisioned the
   seats (the pub/sub hand-off is async).
5. `GET /v1/events/{id}/seatmap` — read back the seat ids.

Then each VU `POST /v1/holds/`s as a **distinct user** (a fresh `sub` per
iteration), which is what makes the contention real rather than a single user
retrying.
