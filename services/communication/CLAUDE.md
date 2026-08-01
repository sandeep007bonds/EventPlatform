# CLAUDE.md — Communication service

Inherits the [root CLAUDE.md](../../CLAUDE.md) and [engineering guidelines](../../docs/engineering-guidelines.md).

## Responsibility

Owns every outbound Email/SMS/WhatsApp message the platform sends. Bounded
context: **Communication** (ADR-0016 — notifications scope only; buyer
identity is deferred, see that ADR). Not tied to any one vendor: Azure
Communication Services (ACS) and Twilio are both addressable, selected
per channel by configuration.

## Owns

- **Data store:** PostgreSQL `communication` DB (this service only) — the
  delivery log (`DeliveryLogEntry`) and the inbound-event dedup ledger
  (`ProcessedNotificationEvent`).
- **Public API:** `POST /v1/notifications/send` (internal only, Dapr service
  invocation — **not** gateway-routed).
- **Events published:** none. Communication is a terminal consumer and a
  synchronous responder, never a publisher — its `CommunicationDbContext`
  does **not** implement the outbox contract, and `Communication.Infrastructure`
  has no reference to `EventPlatform.Messaging`. This is the one service in
  the repo without an outbox; don't add one without a real publishing need.
- **Events consumed:** `OrderConfirmed` (Ordering), `TicketIssued` (Ticketing)
  — wired for dedup-safety, **delivery still deferred** (see below);
  `OrderTicketsIssued` (Ticketing) — **delivers today** (see below, ADR-0021).

## Design notes (ADR-0016)

- **Three narrow ports, not one unified sender.** `IEmailSender`/`ISmsSender`/
  `IWhatsAppSender`, each with a `Provider` string and one `SendAsync(...)`.
  Twilio has no first-party email product under the `Twilio` package
  (that's SendGrid, a different SDK) — Email stays ACS-only.
- **Config-gated vendor selection, per channel**, same shape as Payments'
  Stripe/simulator gate (`Communication.Infrastructure/DependencyInjection.cs`):
  read raw `IConfiguration`, never `IOptions<T>`, never `IsDevelopment()`.
  ```
  Communication:Acs:ConnectionString       # shared across ACS channels
  Communication:Acs:EmailFromAddress
  Communication:Acs:SmsFromNumber
  Communication:Acs:WhatsAppChannelId      # ACS Advanced Messaging channel registration id
  Communication:Email:Provider             # "Acs" | unset -> dev-log
  Communication:Sms:Provider               # "Acs" | "Twilio" | unset -> dev-log
  Communication:WhatsApp:Provider          # "Acs" | "Twilio" | unset -> dev-log
  Communication:Twilio:AccountSid
  Communication:Twilio:AuthToken
  Communication:Twilio:SmsFromNumber
  Communication:Twilio:WhatsAppFromNumber
  ```
  Absence of a provider (or its credentials) always falls back to the
  dev/logging sender — never crashes. Twilio's SDK doesn't integrate with
  `IHttpClientFactory` on its own, so `TwilioSmsSender`/`TwilioWhatsAppSender`
  are registered via `services.AddHttpClient<TwilioSmsSender>(...)` and build
  their `ITwilioRestClient` from the injected `HttpClient`, instead of the
  SDK's process-wide `TwilioClient.Init(...)`.
- **Email templates are embedded resource files**
  (`Communication.Infrastructure/Templates/Resources/{key}.subject.txt` /
  `{key}.body.txt`), not database rows — nothing here needs organizer-editable
  templates at runtime. Still port-shaped (`ITemplateStore`); a future
  DB-backed store is a new adapter, not a caller change. Rendering uses
  Scriban (`ITemplateRenderer`/`ScribanTemplateRenderer`), not hand-rolled
  string substitution — it handles a missing placeholder and HTML-escapes
  values by default. Add a new template by adding its two `.txt` files and a
  `TemplateKeys` constant — no renderer/store changes needed.
- **The `OrderConfirmed`/`TicketIssued` subscribers cannot yet deliver.**
  Neither event carries an email/phone — only a bare `UserId` — and no
  Identity/user-profile service exists to resolve one.
  `IntegrationEventNotificationHandler` dedupes via `ProcessedNotificationEvent`
  (keyed on the event's own `Guid EventId`, unlike Payments'
  `ProcessedWebhookEvent` which dedupes on a vendor `string` id), resolves
  the recipient through `IRecipientResolver`, and — since the only
  implementation (`UnavailableRecipientResolver`) always returns
  `null` — always records a `Skipped` delivery-log row instead of failing,
  crashing, or silently dropping the event. **To wire up real delivery**:
  replace `UnavailableRecipientResolver` with a real implementation (e.g.
  calling a future Identity service via Dapr service invocation) — no
  change needed to the port, the handler, or any caller.
  `TicketIssued` fires once per seat; a real implementation will need a
  batching decision (one email per ticket vs. one combined order receipt)
  — not solved yet.
- **`OrderTicketsIssued` is the one subscriber that delivers today (ADR-0021).**
  It carries the buyer's email directly on the event (captured at Ordering's
  checkout, not resolved via `IRecipientResolver`), so
  `IntegrationEventNotificationHandler.HandleOrderTicketsIssuedAsync` renders
  the new `order-tickets` template (ticket list pre-formatted into one
  flat placeholder string — no loop support in `ITemplateRenderer`) and
  calls `IEmailSender` **directly**, bypassing `NotificationSendService`.
  Reason: `NotificationSendService` does its own internal `SaveChangesAsync`,
  which would split "delivery logged" from "event marked processed" into two
  transactions — a crash in between could cause an at-least-once redelivery
  to double-send. This handler writes the delivery-log row and the
  processed-event marker in **one** `SaveChangesAsync` instead. Falls back to
  a `Skipped` row if `BuyerEmail` is absent (shouldn't happen once checkout
  requires it) or the template is missing.
- **No transactional outbox.** See "Owns" above — this is the one
  intentional structural difference from every other service's scaffold.

## Structure

`Communication.Api` (host + endpoints + Dapr subscriptions) ·
`Communication.Application` (`Sending/` for the sync send path, `Subscribing/`
for the two integration-event handlers, `Abstractions/` for every port) ·
`Communication.Domain` (`DeliveryLogEntry`, `NotificationTemplate`,
`TemplateKeys` + invariants) · `Communication.Infrastructure` (EF Core +
Postgres, `Senders/` vendor adapters, `Templates/` embedded store +
renderer, `Recipients/` resolver — no outbox) · `tests/Communication.Tests`
(the first test project in this repo — unit tests plus a
Testcontainers-backed dedup-ledger test and a NetArchTest layering check).

## Local run

```bash
dapr run --app-id communication \
  --resources-path platform/dapr/components --config platform/dapr/config.yaml \
  -- dotnet run --project services/communication/Communication.Api
```

With no vendor config set, every send uses the dev/logging senders (logs the
payload, returns a synthetic success) — no live ACS/Twilio credentials
needed for local dev.

## Do not

- Read another service's database (resolve recipients via a port, once one
  exists — never a direct query into Ordering/Ticketing/a future Identity DB).
- Add a package version to a `.csproj` (use `Directory.Packages.props`).
- Add an outbox to this service without a real publishing need — it doesn't
  publish integration events today, and adding one "just in case" is exactly
  the kind of premature abstraction the root guidelines warn against.
- Put a real ACS/Twilio secret in `appsettings*.json` — Key Vault/user-secrets
  only, same as Payments' Stripe keys.
