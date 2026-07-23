# 08 — API Design

Conventions and the key endpoints. This is illustrative — a starting contract to
align on, not the final spec (which will live in OpenAPI).

## Conventions

- **REST/JSON** over HTTPS for public/partner APIs; **gRPC** for internal
  service-to-service where latency matters.
- **Versioned**: `/v1/...`. Breaking changes → `/v2`.
- **Auth**: OAuth2 bearer tokens (users), client-credentials (partners),
  mTLS/service tokens (internal).
- **Idempotency**: `Idempotency-Key` header required on all POSTs that create
  orders/payments.
- **Pagination**: cursor-based (`?cursor=...&limit=...`).
- **Errors**: RFC 7807 problem+json (`type`, `title`, `status`, `detail`).
- **Rate limits**: per-token; `429` with `Retry-After`.

## Storefront / buyer API

| Method & path | Purpose | Notes |
|---------------|---------|-------|
| `GET /v1/events` | Search/browse events | Cached, read model |
| `GET /v1/events/{id}` | Event detail | Cached |
| `GET /v1/events/{id}/availability` | **Approximate** availability | Cached, not exact |
| `GET /v1/events/{id}/seatmap` | Seat map | Static/CDN |
| `POST /v1/queue/{eventId}/join` | Join waiting room | Returns queue token |
| `GET /v1/queue/{eventId}/status` | Queue position | Poll/WebSocket |
| `POST /v1/holds` | **Atomic hold** of seats/qty | Requires admission token; returns hold + TTL |
| `DELETE /v1/holds/{id}` | Release a hold early | |
| `POST /v1/orders` | Create order from a hold | `Idempotency-Key`; enforces limits |
| `POST /v1/orders/{id}/payment` | Start payment | `Idempotency-Key`; returns PSP client secret |
| `GET /v1/orders/{id}` | Order status | Poll for confirmation |
| `GET /v1/me/tickets` | My tickets | |
| `POST /v1/tickets/{id}/transfer` | Transfer a ticket | |

### Hold request (example)

```http
POST /v1/holds
Authorization: Bearer <user-token>
X-Admission-Token: <signed golden ticket>
Content-Type: application/json

{ "eventId": "evt_123", "seatIds": ["s_A12","s_A13"] }     // seated
{ "eventId": "evt_123", "ticketTypeId": "tt_ga", "qty": 2 } // GA
```

Responses: `201` held (`{holdId, expiresAt}`), `409` unavailable, `403` no/expired
admission token, `429` rate-limited.

### Order + payment (idempotent)

```http
POST /v1/orders
Idempotency-Key: 6f1c...client-generated
{ "holdId": "hold_789", "buyer": {...} }
```
→ `201 { orderId, status: "awaiting_payment", amount, currency }`

```http
POST /v1/orders/ord_456/payment
Idempotency-Key: 6f1c...same-or-linked
{ "method": "card" }
```
→ `200 { clientSecret }` (complete 3-DS client-side via PSP SDK) → confirmation
arrives via webhook; poll `GET /v1/orders/{id}`.

## Organizer API

| Method & path | Purpose |
|---------------|---------|
| `POST /v1/organizer/events` | Create event (draft) |
| `PUT /v1/organizer/events/{id}` | Update config |
| `POST /v1/organizer/events/{id}/publish` | Publish → generate inventory |
| `POST /v1/organizer/events/{id}/pause` | Pause/resume sales |
| `POST /v1/organizer/events/{id}/holdbacks/release` | Release held-back inventory |
| `PUT /v1/organizer/events/{id}/queue` | Tune admission rate/fairness |
| `GET /v1/organizer/events/{id}/reports/live` | Real-time on-sale metrics |
| `GET /v1/organizer/events/{id}/reports/financial` | Reconciled financials |
| `POST /v1/organizer/orders/{id}/refund` | Refund |

## Partner API & webhooks

- Same storefront read endpoints, scoped by API key + quota.
- **Outbound webhooks** (signed, retried, delivery-logged):
  `order.confirmed`, `order.refunded`, `event.updated`, `ticket.transferred`,
  `event.soldout`.

## Internal webhook ingress (from PSPs)

- `POST /internal/webhooks/{provider}` — signature-verified, deduped into an
  **inbox**, acked fast, processed async (see
  [Integrations](feature-flows/04-third-party-integrations.md)).

## Real-time channels

- **WebSocket** for queue position and (best-effort) live seat-status updates;
  polling fallback for reliability at extreme scale.
