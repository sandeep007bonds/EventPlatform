# Communication service

Owns every outbound Email/SMS/WhatsApp message the platform sends — one
service, one audit trail, one place to swap vendors.

## Flow

Two ways in:

- **Synchronous**: `POST /v1/notifications/send` (internal, Dapr service
  invocation only — never gateway-routed). Sends Email (template-driven),
  SMS, or WhatsApp, and records the outcome to the delivery log.
- **Async, event-driven**: subscribes to `OrderConfirmed` (Ordering) and
  `TicketIssued` (Ticketing) over Dapr pub/sub. Both are wired for
  redelivery-safety (a dedup ledger keyed on the event id) but currently
  record every delivery as `Skipped` — neither event carries recipient
  contact info, and no Identity/user-profile service exists yet to resolve
  a `UserId` to an email/phone. See [ADR-0016](../../docs/adr/0016-buyer-identity-and-notifications.md)
  and this service's [CLAUDE.md](CLAUDE.md) for the deferred-delivery design.

Unlike every other service, Communication never publishes an integration
event, so it has **no transactional outbox**.

## Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/v1/notifications/send` | Send one notification (Email/SMS/WhatsApp) |
| POST | `/integration/ordering/order-confirmed` | Dapr pub/sub topic `OrderConfirmed` (dedup-safe; delivery deferred) |
| POST | `/integration/ticketing/ticket-issued` | Dapr pub/sub topic `TicketIssued` (dedup-safe; delivery deferred) |

## Vendors

Each channel is selected independently by configuration
(`Communication:{Email,Sms,WhatsApp}:Provider` = `Acs` or `Twilio`),
falling back to a dev/logging sender that logs the payload and returns a
synthetic success — no live vendor credentials needed for local dev or CI.
See [CLAUDE.md](CLAUDE.md) for the exact config keys.

## Layers

`Communication.Api` · `Communication.Application` (sending + subscribing +
ports) · `Communication.Domain` (`DeliveryLogEntry`, `NotificationTemplate`)
· `Communication.Infrastructure` (EF Core + Postgres, vendor senders,
embedded templates — no outbox) · `tests/Communication.Tests`.

See [service CLAUDE.md](CLAUDE.md) and [ADR-0016](../../docs/adr/0016-buyer-identity-and-notifications.md).

## Run locally

See [docs/local-development.md](../../docs/local-development.md). Needs a
Dapr sidecar and Postgres. With no vendor config set, every send uses the
dev/logging senders — check the console output or the `communication.delivery_log`
table to see what would have been sent.
