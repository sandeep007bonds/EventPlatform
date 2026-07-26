// Throughput / capacity test — sustained hold rate against a large seat map.
//
// Unlike no-oversell.js (everyone fights over ONE seat), this proves the hot path holds up under
// sustained flash-sale load: a big inventory, many users, each grabbing a *different* seat. We
// measure hold latency (p95/p99) and error rate — no contention is expected, so almost every hold
// should win.
//
// Prereqs: full stack running with Dapr (Catalog, Inventory) + Postgres + Redis, with
// `Jwt:DevSigningKey` set (it is, in appsettings.Development.json). Then:
//
//   k6 run platform/loadtest/throughput.js
//   k6 run -e VUS=300 -e DURATION=2m -e SEATS=20000 platform/loadtest/throughput.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate } from 'k6/metrics';
import { mintToken, uuidv4 } from './lib/jwt.js';

const CATALOG = __ENV.CATALOG_URL || 'https://localhost:7080';
const INVENTORY = __ENV.INVENTORY_URL || 'https://localhost:7081';
const DEV_KEY = __ENV.DEV_SIGNING_KEY || 'eventplatform-dev-hs256-signing-key-not-a-secret';
const TENANT = __ENV.TENANT_ID || '11111111-1111-1111-1111-111111111111';
const VUS = parseInt(__ENV.VUS || '100', 10);
const DURATION = __ENV.DURATION || '1m';
const SEATS = parseInt(__ENV.SEATS || '10000', 10);

const held = new Counter('holds_succeeded');
const failed = new Rate('holds_failed');

export const options = {
  insecureSkipTLSVerify: true, // dev self-signed cert
  scenarios: {
    ramp: {
      executor: 'constant-vus',
      vus: VUS,
      duration: DURATION,
    },
  },
  thresholds: {
    // Capacity bar: fast holds and a low error rate under sustained load.
    'http_req_duration{name:PlaceHold}': ['p(95)<250', 'p(99)<500'],
    holds_failed: ['rate<0.01'],
  },
};

function auth(sub) {
  const token = mintToken(DEV_KEY, { tenant_id: TENANT, sub });
  return { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };
}

export function setup() {
  const admin = auth('00000000-0000-0000-0000-000000000001');

  // A single big section: SEATS seats in one row keeps the payload small.
  const startsAt = new Date(Date.now() + 30 * 24 * 3600 * 1000).toISOString();
  const created = http.post(`${CATALOG}/v1/events`, JSON.stringify({
    venueId: '22222222-2222-2222-2222-222222222222',
    title: `Load Test — ${SEATS} seats`,
    startsAt,
    currency: 'USD',
  }), admin);
  check(created, { 'event created (201)': (r) => r.status === 201 });
  const eventId = created.json('id');

  const seatmap = http.post(`${CATALOG}/v1/events/${eventId}/seatmap`, JSON.stringify({
    name: 'Big section',
    sections: [{ name: 'GA', priceTier: 'Std', priceAmount: 50.0, rows: 1, seatsPerRow: SEATS }],
  }), admin);
  check(seatmap, { 'seatmap defined (201)': (r) => r.status === 201 });

  const published = http.post(`${CATALOG}/v1/events/${eventId}/publish`, null, admin);
  check(published, { 'event published (204)': (r) => r.status === 204 });

  // Inventory provisions via Dapr pub/sub (async) — wait until all seats land.
  let seatCount = 0;
  for (let i = 0; i < 60 && seatCount < SEATS; i++) {
    const inv = http.get(`${INVENTORY}/v1/events/${eventId}/inventory`, admin);
    if (inv.status === 200) seatCount = inv.json('seatCount') || 0;
    if (seatCount < SEATS) sleep(1);
  }
  check(null, { 'inventory fully provisioned': () => seatCount >= SEATS });

  const map = http.get(`${CATALOG}/v1/events/${eventId}/seatmap`, admin);
  const seatIds = map.json('seats').map((s) => s.id);

  return { eventId, seatIds };
}

export default function (data) {
  // Each iteration grabs a random seat — collisions are rare, so this measures raw throughput.
  const seatId = data.seatIds[Math.floor(Math.random() * data.seatIds.length)];
  const res = http.post(`${INVENTORY}/v1/holds/`, JSON.stringify({
    eventId: data.eventId,
    seatIds: [seatId],
  }), Object.assign({ tags: { name: 'PlaceHold' } }, auth(uuidv4())));

  if (res.status === 201) held.add(1);
  // A 409 here is a random seat collision, not an error. Anything else is a real failure.
  failed.add(res.status !== 201 && res.status !== 409);

  check(res, { 'hold resolved (201 or 409)': (r) => r.status === 201 || r.status === 409 });
}
