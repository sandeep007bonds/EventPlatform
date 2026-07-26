# Local end-to-end walkthrough

Run the **whole platform** on your machine and drive a full purchase:
**create event → seat map → publish → provision inventory → hold → checkout
(pay) → order confirmed → ticket issued.**

No Azure, no Kubernetes. Everything is Docker + five .NET processes, each with a
Dapr sidecar. See [local-development.md](local-development.md) for the background;
this file is the copy-paste runbook.

**What "no oversell" means:** the platform's core promise is that the same seat
is never sold to two people. In a flash sale, thousands of buyers can hit "buy"
on the same seat within the same second; exactly one may win it — everyone else
must get a clean "sorry, taken," not a double sale. Section 6 below proves this
with a load test that races 200 simulated buyers for one seat.

**What "the flow script" is:** step 5 is a small shell script of `curl` calls
that drives one purchase through the real, running services — create an event,
define its seats, publish it, wait for inventory, place a hold, check out, and
read back the confirmed order and issued ticket. It's a way to see the whole
system work end-to-end without building a UI first.

## 0. Prerequisites

| Tool | Install |
|------|---------|
| Docker Desktop | runs Postgres, Redis, Jaeger |
| .NET 10 SDK | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Dapr CLI | <https://docs.dapr.io/getting-started/install-dapr-cli/> then `dapr init` (one-time) |

Optional, only if you use the example flow script in step 5: `jq` (JSON parsing
in `curl` output) and `python3` (to mint the dev JWT). Neither is a platform
dependency — you can equally paste ids by hand and mint the token at
<https://jwt.io>.

## 1. Start the backing services

```bash
docker compose up -d          # Postgres:5432, Redis:6380, Jaeger:16686
```

All five services share one Postgres database (`eventplatform`), each in its own
schema (`catalog`, `inventory`, …). Redis is the inventory fast gate **and** the
Dapr pub/sub + workflow state store (see `platform/dapr/components/`).

## 2. Schema — fully automatic, nothing to run

Each service creates its own schema from its current EF Core model the first
time it starts in Development (`Database.EnsureCreatedAsync()` in `Program.cs`)
— there is no `dotnet ef` command to run and no `Migrations/` folder to
generate or commit. Just start the services (step 3) and the tables appear.

> This is deliberately **not** real EF Core migrations (`Migrate`) — that's the
> right tool once the schema needs to evolve without dropping data (staging/
> production), and is tracked separately. For disposable local dev,
> `EnsureCreated` is simpler and needs zero commands from you. If you change a
> domain model and the columns look stale, the fastest fix locally is
> `docker compose down -v && docker compose up -d` to drop and recreate the
> Postgres volume — it'll be rebuilt from the model on next service start.

## 3. Run the five services, each with a Dapr sidecar

Open **five terminals** (or use a multiplexer). Each service runs over plain
HTTP on its `508x` port so Dapr's callbacks (pub/sub delivery, service
invocation) and your `curl`s don't fight the dev TLS cert. `--app-port` /
`--app-protocol http` are what let Dapr deliver events and route
service-to-service calls by app-id.

```bash
# terminal 1 — Catalog (events + seat maps)
ASPNETCORE_URLS=http://localhost:5080 dapr run --app-id catalog --app-port 5080 --app-protocol http \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/catalog/Catalog.Api

# terminal 2 — Inventory (no-oversell holds)
ASPNETCORE_URLS=http://localhost:5081 dapr run --app-id inventory --app-port 5081 --app-protocol http \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/inventory/Inventory.Api

# terminal 3 — Ordering (checkout saga / Dapr Workflow)
ASPNETCORE_URLS=http://localhost:5082 dapr run --app-id ordering --app-port 5082 --app-protocol http \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/ordering/Ordering.Api

# terminal 4 — Payments (simulated gateway unless a Stripe key is set)
ASPNETCORE_URLS=http://localhost:5083 dapr run --app-id payments --app-port 5083 --app-protocol http \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/payments/Payments.Api

# terminal 5 — Ticketing (issues tickets on OrderConfirmed)
ASPNETCORE_URLS=http://localhost:5084 dapr run --app-id ticketing --app-port 5084 --app-protocol http \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/ticketing/Ticketing.Api
```

**Payments gateway:** with no Stripe secret configured, Payments uses the
`SimulatedPaymentGateway` (captures synchronously, always succeeds) — perfect for
E2E. To exercise real Stripe instead, set a **test** key before starting it:
`dotnet user-secrets set "Payments:Stripe:SecretKey" "sk_test_..." --project services/payments/Payments.Api`
(never commit it).

## 4. Mint a dev JWT

Every write endpoint reads the tenant from the token (`tenant_id` claim) and the
buyer from `sub`. In Development the services accept an HS256 token signed with
`Jwt:DevSigningKey` (set in each `appsettings.Development.json`) — no identity
provider needed. Mint one:

```bash
export TOKEN=$(python3 - <<'PY'
import base64, hmac, hashlib, json, time, uuid
secret = "eventplatform-dev-hs256-signing-key-not-a-secret"
tenant = "11111111-1111-1111-1111-111111111111"
b64 = lambda b: base64.urlsafe_b64encode(b).rstrip(b"=")
now = int(time.time())
head = {"alg": "HS256", "typ": "JWT"}
body = {"iss": "eventplatform-dev", "aud": "eventplatform", "iat": now, "exp": now + 3600,
        "tenant_id": tenant, "sub": str(uuid.uuid4())}
si = b64(json.dumps(head).encode()) + b"." + b64(json.dumps(body).encode())
sig = b64(hmac.new(secret.encode(), si, hashlib.sha256).digest())
print((si + b"." + sig).decode())
PY
)
echo "$TOKEN"
```

Use the **same** `$TOKEN` for the hold and the checkout — the saga checks that the
hold's owner (`sub`) matches the checkout caller.

## 5. Drive the full flow

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

## 6. Prove the guarantees

- **No oversell under contention.** With the stack up, run the load test — 200
  users race for one seat and exactly one may win:
  ```bash
  k6 run -e CATALOG_URL=http://localhost:5080 -e INVENTORY_URL=http://localhost:5081 \
    platform/loadtest/no-oversell.js
  ```
  (See [platform/loadtest/README.md](../platform/loadtest/README.md). The scripts
  default to the `https` 7080/7081 ports; pass the `http` URLs as above to match
  this runbook.)
- **Idempotent checkout.** Re-run step 7 with the **same** `Idempotency-Key` — you
  get the same `orderId` back, no second charge.
- **Self-healing fast gate.** `docker compose restart redis`, then place another
  hold: the `InventoryReconciler` rebuilds Redis from Postgres (held/sold seats)
  within its interval, so previously-sold seats are still rejected.
- **Traces.** Open <http://localhost:16686> and pick a service to see the request
  span across Catalog → Inventory → Ordering → Payments → Ticketing.
- **Health:** `curl http://localhost:508x/health/ready` for each service.

## 7. Payments webhooks (optional, real Stripe only)

If you ran Payments with a real Stripe test key and want to exercise async
capture/refunds, set a webhook signing secret and forward events with the Stripe
CLI:

```bash
dotnet user-secrets set "Payments:Stripe:WebhookSecret" "whsec_..." --project services/payments/Payments.Api
stripe listen --forward-to http://localhost:5083/v1/payments/webhooks/stripe
```

The endpoint verifies the `Stripe-Signature`, dedupes on the Stripe event id, and
reconciles the payment idempotently. Without a signing secret it returns `503`.

## 8. Teardown

```bash
# Ctrl-C each dapr run; then:
docker compose down          # add -v to also wipe the Postgres volume
```

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `401 Unauthorized` on a write | `$TOKEN` expired (1 h) — mint a new one (step 4) |
| `inventory` stays `seatCount: 0` | Inventory sidecar not up, or `--app-port`/`--app-protocol http` missing so pub/sub can't deliver |
| Checkout hangs / 500 | Ordering needs the `statestore` component (Dapr Workflow) — it's in `platform/dapr/components`; ensure `--resources-path` points there |
| `relation does not exist` | The schema wasn't created — check the service's own startup logs for an `EnsureCreated`/Postgres connection error (bad connection string, Postgres not up yet) |
| Dapr can't reach a service | app-id mismatch — the ids must be exactly `catalog`/`inventory`/`ordering`/`payments`/`ticketing` |
