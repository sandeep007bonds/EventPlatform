# Local end-to-end walkthrough

Run the **whole platform** on your machine and drive a full purchase:
**create event → seat map → publish → provision inventory → hold → checkout
(pay) → order confirmed → ticket issued.**

No Azure, no Kubernetes. One command starts everything: Docker (Postgres,
Redis, Jaeger) plus all five services, each with a Dapr sidecar.

**What "no oversell" means:** the platform's core promise is that the same seat
is never sold to two people. In a flash sale, thousands of buyers can hit "buy"
on the same seat within the same second; exactly one may win it — everyone else
must get a clean "sorry, taken," not a double sale. Section 4 below proves this
with a load test that races 200 simulated buyers for one seat.

**What "the flow script" is:** section 3 is a small shell script of `curl`
calls that drives one purchase through the real, running services — create an
event, define its seats, publish it, wait for inventory, place a hold, check
out, and read back the confirmed order and issued ticket. It's a way to see the
whole system work end-to-end without building a UI first.

## 0. Prerequisites

| Tool | Install |
|------|---------|
| Docker Desktop | runs Postgres, Redis, Jaeger |
| .NET 10 SDK | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Dapr CLI (>= 1.13) | <https://docs.dapr.io/getting-started/install-dapr-cli/> |

Optional, only if you use the example flow script in step 3: `jq` (JSON parsing
in `curl` output) and `python3` (to mint the dev JWT, used by
`scripts/dev-token.sh`). Neither is a platform dependency — you can equally
paste ids by hand and mint a token at <https://jwt.io>.

These scripts are Bash — on Windows use WSL or Git Bash.

## 1. One-click start

```bash
./scripts/dev-up.sh
```

This single command:
1. Starts Postgres, Redis, and Jaeger (`docker compose up -d`) and waits for
   them to report healthy.
2. Installs the local Dapr runtime the first time you run it (`dapr init`).
3. Starts all five services — Catalog, Inventory, Ordering, Payments,
   Ticketing — each with its own Dapr sidecar, via
   [Dapr multi-app run](https://docs.dapr.io/developing-applications/local-development/multi-app-dapr-run/)
   (`platform/dapr/dapr.yaml`).

Each service creates its own Postgres schema from its current EF Core model
the first time it starts (`Database.EnsureCreatedAsync()`, Development only) —
there's no separate migration step and nothing else to run.

**Ctrl+C stops everything** (all five services and their sidecars). Then, to
also stop the Docker containers:

```bash
./scripts/dev-down.sh          # add -v to also wipe the Postgres volume
```

When it's up you have:

| Service | Scalar API docs |
|---------|------------------|
| Catalog | http://localhost:5080/scalar/v1 |
| Inventory | http://localhost:5081/scalar/v1 |
| Ordering | http://localhost:5082/scalar/v1 |
| Payments | http://localhost:5083/scalar/v1 |
| Ticketing | http://localhost:5084/scalar/v1 |
| Jaeger UI | http://localhost:16686 |

**Payments gateway:** with no Stripe secret configured, Payments uses the
`SimulatedPaymentGateway` (captures synchronously, always succeeds) — perfect
for E2E. To exercise real Stripe instead, set a **test** key *before* running
`dev-up.sh`: `dotnet user-secrets set "Payments:Stripe:SecretKey" "sk_test_..." --project services/payments/Payments.Api`
(never commit it).

> **Under the hood:** `dev-up.sh` wraps `dapr run -f platform/dapr/dapr.yaml`,
> which is equivalent to running `dapr run --app-id catalog --app-port 5080
> --app-protocol http -- dotnet run --project services/catalog/Catalog.Api` for
> each service in its own terminal — one Dapr multi-app run template replaces
> five manual commands. `--app-port`/`--app-protocol http` are what let Dapr
> deliver pub/sub events and route service-to-service calls by app-id.

## 2. Mint a dev JWT

Every write endpoint reads the tenant from the token (`tenant_id` claim) and
the buyer from `sub`). In Development every service accepts an HS256 token
signed with `Jwt:DevSigningKey` (set in each `appsettings.Development.json`) —
no identity provider needed. In a **second terminal** (the first is running
`dev-up.sh`):

```bash
export TOKEN=$(./scripts/dev-token.sh)
echo "$TOKEN"
```

Use the **same** `$TOKEN` for the hold and the checkout below — the saga checks
that the hold's owner (`sub`) matches the checkout caller.

## 3. Drive the full flow

```bash
CATALOG=http://localhost:5080
INVENTORY=http://localhost:5081
ORDERING=http://localhost:5082
TICKETING=http://localhost:5084
AUTH=(-H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json")

# 1) Create a draft event
EVENT_ID=$(curl -s "${AUTH[@]}" "$CATALOG/v1/events" -d '{
  "venueId":"22222222-2222-2222-2222-222222222222",
  "title":"Coldplay — Wembley","startsAt":"2026-09-01T19:00:00Z","currency":"GBP"
}' | jq -r .id)
echo "event=$EVENT_ID"

# 2) Define a seat map (2 seats here)
curl -s "${AUTH[@]}" "$CATALOG/v1/events/$EVENT_ID/seatmap" -d '{
  "name":"Main","sections":[{"name":"A","priceTier":"Std","priceAmount":50.0,"rows":1,"seatsPerRow":2}]
}' -o /dev/null -w "seatmap: %{http_code}\n"

# 3) Publish — Catalog emits EventPublished; Inventory provisions via pub/sub
curl -s "${AUTH[@]}" "$CATALOG/v1/events/$EVENT_ID/publish" -X POST -o /dev/null -w "publish: %{http_code}\n"

# 4) Wait for Inventory to provision (async pub/sub hand-off)
until [ "$(curl -s "${AUTH[@]}" "$INVENTORY/v1/events/$EVENT_ID/inventory" | jq -r .seatCount)" != "0" ]; do
  sleep 1; echo "waiting for inventory…"
done
curl -s "${AUTH[@]}" "$INVENTORY/v1/events/$EVENT_ID/inventory" | jq

# 5) Grab a seat id from the seat map
SEAT_ID=$(curl -s "${AUTH[@]}" "$CATALOG/v1/events/$EVENT_ID/seatmap" | jq -r '.seats[0].id')
echo "seat=$SEAT_ID"

# 6) Place a hold (note the trailing slash on /v1/holds/)
HOLD_ID=$(curl -s "${AUTH[@]}" "$INVENTORY/v1/holds/" -d "{\"eventId\":\"$EVENT_ID\",\"seatIds\":[\"$SEAT_ID\"]}" | jq -r .holdId)
echo "hold=$HOLD_ID"

# 7) Checkout — Idempotency-Key header required; runs the Dapr Workflow saga
#    (validate hold → create order → charge → convert-to-sold → confirm)
ORDER_ID=$(curl -s "${AUTH[@]}" -H "Idempotency-Key: $(uuidgen)" \
  "$ORDERING/v1/checkout" -d "{\"holdId\":\"$HOLD_ID\"}" | jq -r .orderId)
echo "order=$ORDER_ID"

# 8) Inspect the confirmed order
curl -s "${AUTH[@]}" "$ORDERING/v1/orders/$ORDER_ID" | jq

# 9) Ticketing issued a ticket on OrderConfirmed (async) — poll for it
until [ "$(curl -s "${AUTH[@]}" "$TICKETING/v1/orders/$ORDER_ID/tickets" | jq 'length')" != "0" ]; do
  sleep 1; echo "waiting for ticket…"
done
curl -s "${AUTH[@]}" "$TICKETING/v1/orders/$ORDER_ID/tickets" | jq
```

You now have a confirmed order and an issued ticket — the full contested-seat
purchase, end to end.

## 4. Prove the guarantees

- **No oversell under contention.** With the stack up, run the load test — 200
  users race for one seat and exactly one may win:
  ```bash
  k6 run -e CATALOG_URL=http://localhost:5080 -e INVENTORY_URL=http://localhost:5081 \
    platform/loadtest/no-oversell.js
  ```
  (See [platform/loadtest/README.md](../platform/loadtest/README.md). The scripts
  default to the `https` 7080/7081 ports; pass the `http` URLs as above to match
  this runbook.)
- **Idempotent checkout.** Re-run checkout (step 3.7) with the **same**
  `Idempotency-Key` — you get the same `orderId` back, no second charge.
- **Self-healing fast gate.** `docker compose restart redis`, then place another
  hold: the `InventoryReconciler` rebuilds Redis from Postgres (held/sold seats)
  within its interval, so previously-sold seats are still rejected.
- **Traces.** Open <http://localhost:16686> and pick a service to see the request
  span across Catalog → Inventory → Ordering → Payments → Ticketing.
- **Health:** `curl http://localhost:508x/health/ready` for each service.

## 5. Payments webhooks (optional, real Stripe only)

If you ran Payments with a real Stripe test key and want to exercise async
capture/refunds, set a webhook signing secret before starting `dev-up.sh` and
forward events with the Stripe CLI:

```bash
dotnet user-secrets set "Payments:Stripe:WebhookSecret" "whsec_..." --project services/payments/Payments.Api
stripe listen --forward-to http://localhost:5083/v1/payments/webhooks/stripe
```

The endpoint verifies the `Stripe-Signature`, dedupes on the Stripe event id, and
reconciles the payment idempotently. Without a signing secret it returns `503`.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `dev-up.sh` exits at "Dapr CLI not found" | Install it, then re-run — <https://docs.dapr.io/getting-started/install-dapr-cli/> |
| `dev-up.sh` hangs waiting for postgres/redis | Check `docker compose logs postgres` / `docker compose logs redis` |
| `dapr run -f` errors on the yaml | Your Dapr CLI is too old — multi-app run needs **>= 1.13**; `dapr --version` to check, then upgrade |
| `401 Unauthorized` on a write | `$TOKEN` expired (1 h) — mint a new one (section 2) |
| `inventory` stays `seatCount: 0` | Check the inventory sidecar's logs in the `dev-up.sh` output for a pub/sub delivery error |
| Checkout hangs / 500 | Ordering needs the `statestore` component (Dapr Workflow) — it's in `platform/dapr/components`, already wired into `platform/dapr/dapr.yaml` |
| `relation does not exist` | The schema wasn't created — check that service's startup logs for an `EnsureCreated`/Postgres connection error |
| Dapr can't reach a service | app-id mismatch — the ids must be exactly `catalog`/`inventory`/`ordering`/`payments`/`ticketing` (already set correctly in `platform/dapr/dapr.yaml`) |
