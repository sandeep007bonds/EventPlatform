// No-oversell load test — the headline correctness proof.
//
// Many concurrent users all race for the SAME single seat. Exactly one hold must win; every other
// attempt must get 409. The `holds_succeeded: count<2` threshold is the hard no-oversell gate — if
// two holds ever win the same seat, the test FAILS.
//
// Prereqs: the full stack running with Dapr (Catalog, Inventory) and Postgres + Redis, with
// `Jwt:DevSigningKey` set (it is, in appsettings.Development.json). Then:
//
//   k6 run platform/loadtest/no-oversell.js
//   k6 run -e VUS=500 platform/loadtest/no-oversell.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';
import { mintToken, uuidv4 } from './lib/jwt.js';

const CATALOG = __ENV.CATALOG_URL || 'https://localhost:7080';
const INVENTORY = __ENV.INVENTORY_URL || 'https://localhost:7081';
const DEV_KEY = __ENV.DEV_SIGNING_KEY || 'eventplatform-dev-hs256-signing-key-not-a-secret';
const TENANT = __ENV.TENANT_ID || '11111111-1111-1111-1111-111111111111';
const VUS = parseInt(__ENV.VUS || '200', 10);

const held = new Counter('holds_succeeded');
const conflicted = new Counter('holds_conflicted');

export const options = {
  insecureSkipTLSVerify: true, // dev self-signed cert
  scenarios: {
    contention: { executor: 'per-vu-iterations', vus: VUS, iterations: 1, maxDuration: '60s' },
  },
  thresholds: {
    // THE no-oversell guarantee: at most one hold wins the single seat.
    holds_succeeded: ['count<2'],
    'checks': ['rate==1.0'],
  },
};

function auth(sub) {
  const token = mintToken(DEV_KEY, { tenant_id: TENANT, sub });
  return { headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' } };
}

export function setup() {
  const admin = auth('00000000-0000-0000-0000-000000000001');

  const startsAt = new Date(Date.now() + 30 * 24 * 3600 * 1000).toISOString();
  const created = http.post(`${CATALOG}/v1/events`, JSON.stringify({
    venueId: '22222222-2222-2222-2222-222222222222',
    title: 'Load Test — one seat',
    startsAt,
    currency: 'USD',
  }), admin);
  check(created, { 'event created (201)': (r) => r.status === 201 });
  const eventId = created.json('id');

  const seatmap = http.post(`${CATALOG}/v1/events/${eventId}/seatmap`, JSON.stringify({
    name: 'One seat',
    sections: [{ name: 'A', priceTier: 'Std', priceAmount: 50.0, rows: 1, seatsPerRow: 1 }],
  }), admin);
  check(seatmap, { 'seatmap defined (201)': (r) => r.status === 201 });

  const published = http.post(`${CATALOG}/v1/events/${eventId}/publish`, null, admin);
  check(published, { 'event published (204)': (r) => r.status === 204 });

  // Inventory provisions via Dapr pub/sub (async) — wait for it.
  let seatCount = 0;
  for (let i = 0; i < 30 && seatCount === 0; i++) {
    const inv = http.get(`${INVENTORY}/v1/events/${eventId}/inventory`, admin);
    if (inv.status === 200) seatCount = inv.json('seatCount') || 0;
    if (seatCount === 0) sleep(1);
  }
  check(null, { 'inventory provisioned': () => seatCount > 0 });

  const map = http.get(`${CATALOG}/v1/events/${eventId}/seatmap`, admin);
  const seatId = map.json('seats')[0].id;

  return { eventId, seatId };
}

export default function (data) {
  // Each VU is a distinct user racing for the one seat.
  const res = http.post(`${INVENTORY}/v1/holds/`, JSON.stringify({
    eventId: data.eventId,
    seatIds: [data.seatId],
  }), auth(uuidv4()));

  if (res.status === 201) held.add(1);
  else if (res.status === 409) conflicted.add(1);

  check(res, { 'hold resolved (201 or 409)': (r) => r.status === 201 || r.status === 409 });
}

export function handleSummary(data) {
  const won = (data.metrics.holds_succeeded && data.metrics.holds_succeeded.values.count) || 0;
  const lost = (data.metrics.holds_conflicted && data.metrics.holds_conflicted.values.count) || 0;
  const verdict = won === 1
    ? 'PASS — exactly one hold won the seat (no oversell)'
    : `FAIL — ${won} holds won the SAME seat (oversell!)`;
  return {
    stdout: `\n=================== NO-OVERSELL ===================\n${verdict}\nheld=${won}  conflicted=${lost}  attempts=${won + lost}\n==================================================\n`,
  };
}
