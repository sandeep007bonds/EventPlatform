# 01 — Requirements

## Functional requirements

### Event & catalog
- Organizers create events with venue, date/time, description, media, and
  categories.
- Support **seated** (assigned-seat) and **general admission** (GA / capacity)
  events, and mixed events.
- Configure **ticket types** (Adult, Child, VIP, Accessible) and **pricing
  tiers** per seating section.
- Configure **sales windows**: presale (access-code gated), general on-sale,
  and end time.
- Support seat maps with sections, rows, and individual seats.
- Publish / unpublish / reschedule / cancel events.

### Discovery & storefront
- Search and browse events by name, artist, venue, city, date, category.
- Event detail page with live availability (approximate, cached) and pricing.
- Interactive seat map with real-time-ish seat status.

### Waiting queue
- When an event is in high-demand mode, all buyers pass through a **virtual
  waiting room** before reaching the store.
- Fair ordering (first-come or randomized-at-open, configurable).
- Users see live queue position / estimated wait.
- Controlled admission into the store at a configurable rate.
- Signed admission tokens with expiry; re-queue on abuse.

### Selecting & holding tickets
- Users select seats (map) or quantity (GA).
- Selected seats are **held** for the user for a fixed TTL (e.g., 8–10 min).
- Holds auto-expire and return inventory if checkout isn't completed.
- **No overselling under any concurrency.**
- Per-user purchase limits (e.g., max 4 per event) enforced across sessions.

### Checkout & payment
- Collect buyer details; apply fees, taxes, discounts, access codes.
- Pay via integrated gateway(s); support cards, wallets, and local methods.
- **Idempotent** order creation and payment capture.
- Handle payment success, failure, timeout, and gateway webhooks reliably.
- On success: confirm order, finalize inventory, issue tickets.
- On failure/expiry: release the hold, no charge.

### Ticket issuance & delivery
- Generate tickets with a **rotating/secure QR or barcode** (anti-screenshot).
- Deliver via email, in-app, and **Apple/Google Wallet** passes.
- Support transfer of tickets to another user.

### Access control
- Gate scanning validates tickets, prevents re-use / duplicates.
- Works **offline** at the gate and reconciles later.

### Orders, refunds, support
- Order history, resend tickets, transfer.
- Refunds and partial refunds; automatic refunds on event cancellation.
- Rescheduling flow (keep, refund, or exchange).

### Organizer dashboard & reporting
- Real-time sales, revenue, inventory remaining, sell-through rate.
- Sales by tier/section/channel/time.
- Financial reconciliation and payout reports.
- Exportable reports (CSV/PDF) and scheduled email reports.

### Integrations
- Payment gateways, email/SMS, wallets, anti-bot/CAPTCHA, anti-fraud,
  analytics, and a **partner API + webhooks**.

## Non-functional requirements

| Area | Requirement |
|------|-------------|
| **Performance** | Post-admission checkout p99 < 2s; storefront reads p99 < 300ms (cached). |
| **Scalability** | Horizontally scalable; handle 500k+ queued and 10k+/s admitted ops per hot event; scale out ahead of scheduled on-sales. |
| **Availability** | 99.95%+ during on-sale windows; multi-AZ; graceful degradation. |
| **Correctness** | Zero oversell, zero double-charge — hard invariants, enforced at the data layer, not just app logic. |
| **Consistency** | Strong consistency for inventory & payments; eventual consistency acceptable for search, availability counts, and reporting. |
| **Durability** | No confirmed order or payment is ever lost. Event-sourced/append-only audit trail for money and inventory. |
| **Security** | PCI-DSS SAQ-A (no raw card data touches our servers); OWASP Top 10; strong authN/Z; bot defense. |
| **Fairness** | Queue prevents bots/scalpers from jumping ahead; per-user limits enforced. |
| **Observability** | Full tracing, metrics, logs; live on-sale "war room" dashboards. |
| **Compliance** | GDPR/data-privacy, tax handling, accessibility (WCAG 2.1 AA), local consumer laws. |
| **Recoverability** | RTO < 15 min, RPO < 1 min for core transactional data. |

## Key invariants (never violated)

1. A specific seat is sold to **at most one** confirmed order.
2. A confirmed order has **exactly one** successful payment (or is fully
   refunded).
3. Inventory released by an expired hold is **exactly** the inventory that was
   held — no leaks, no double-release.
4. A user cannot exceed the per-event purchase limit across concurrent sessions.
