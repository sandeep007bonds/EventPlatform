# ADR-0016 — Buyer identity & notifications: Communication service (notifications scope)

- **Status:** Accepted
- **Date:** 2026-07-28

## Context

This filename/number was already forward-referenced from
`gateways/EventPlatform.Gateway/README.md`, `gateways/EventPlatform.Gateway/CLAUDE.md`,
and `docs/adr/0015-frontend-react-vite-antd-and-bff-gateway.md` as the future
record for two related-but-separate concerns: real buyer authentication
(OTP-based Identity) and outbound notifications. Only the notifications half
— the new **Communication** service — is built as of this ADR. Buyer
Identity (OTP issuance, session tokens, Entra External ID for organizers)
remains **Deferred** (see below) and gets its own decision content once that
service is actually designed; this ADR's number and title stay reserved for
that, extending rather than superseding this record when it lands.

`docs/design/hld.md`'s pre-implementation sketch calls this component
"Notification." **Communication** is the as-built name — see
`docs/data-flow-and-service-boundaries.md` for that reconciliation; `hld.md`
itself is left untouched, consistent with how none of the other five
services' entries there were retroactively renamed either.

## Decision

Build `services/communication/` as a sixth backend service owning every
outbound Email/SMS/WhatsApp message the platform sends.

- **Dual-vendor, config-gated adapters, one port per channel.** `IEmailSender`
  / `ISmsSender` / `IWhatsAppSender` each support Azure Communication
  Services (ACS) and/or Twilio, selected per channel via configuration
  (`Communication:{Email,Sms,WhatsApp}:Provider`), falling back to a
  dev/logging sender when nothing is configured — the same config-key-presence
  gate already proven in Payments (`IPaymentGateway` →
  `SimulatedPaymentGateway`/`StripePaymentGateway`). Three narrow ports
  rather than one unified interface, because Twilio has no first-party email
  product under its `Twilio` package (that's SendGrid, a different SDK) —
  forcing Email's `subject`/`htmlBody` into a shared shape with SMS/WhatsApp's
  plain `body` would help nothing. This also lets ACS and Twilio run
  simultaneously for different channels (e.g. ACS for Email, Twilio for
  WhatsApp while its sandbox is easier to get approved).
- **A single send endpoint**, `POST /v1/notifications/send`, internal
  (Dapr service invocation only, never gateway-routed), taking a channel
  discriminator plus either a template key (Email) or a raw body (SMS/WhatsApp).
- **Email is template-driven.** Templates are embedded resource files
  shipped with the service (`Communication.Infrastructure/Templates/Resources/`),
  not database rows — nothing in scope needs organizer-editable templates at
  runtime, and this avoids inventing a CRUD/admin-UI/seed-data mechanism this
  repo has no precedent for. Still port-shaped (`ITemplateStore`), so a
  future DB-backed store is a new adapter, not a caller change. Placeholder
  substitution uses Scriban (BSD-2-Clause) rather than hand-rolled string
  replace — it correctly handles a missing placeholder and HTML-escapes
  values by default (relevant since a placeholder could carry user-supplied
  text), and leaves room to grow (loops/conditionals) without a rewrite.
- **No transactional outbox.** Communication never publishes an integration
  event — it's a terminal consumer and a synchronous responder — so its
  `CommunicationDbContext` doesn't implement the outbox contract and its
  Infrastructure project has no reference to `EventPlatform.Messaging`.
  This is a deliberate deviation from every other service's scaffold.
- **`OrderConfirmed`/`TicketIssued` subscribers are wired for redelivery-safety
  now; real delivery is deferred.** Neither event carries an email/phone —
  only a bare `UserId` — and no Identity/user-profile service exists yet to
  resolve one. Both subscribers are fully wired (topics registered, a new
  `ProcessedNotificationEvent` dedup ledger checked-then-recorded in the same
  transaction as the delivery-log write) but resolve the recipient through a
  new port, `IRecipientResolver`, whose only implementation today
  (`UnavailableRecipientResolver`) always returns "unknown." An honest
  `Skipped` row is written to the delivery log instead of failing, crashing,
  or silently dropping the event. This becomes a one-adapter swap — not a
  change to the port or its callers — once Identity (or any user-profile
  source) exists.

## Deferred (not built here)

Buyer identity: OTP issuance via a new Identity service, phone-based
verification, Identity-issued session tokens validated by the 5 existing
services' existing OIDC code path, and organizer auth via Entra External ID.
These remain the original build order's steps 7-9 (tenant-derivation fix,
Identity service, Entra External ID) and are unaffected by anything decided
here. `IRecipientResolver` is the intended integration point once Identity
exists.

## Consequences

- The platform can send real Email/SMS/WhatsApp today via the synchronous
  `/v1/notifications/send` endpoint (verifiable end-to-end with only the
  dev/logging senders — no live vendor credentials needed), while the two
  async subscribers prove the redelivery-safety machinery works without yet
  delivering real notifications.
- Switching or dual-running ACS/Twilio per channel is a configuration
  change, not a code change.
- `docs/data-flow-and-service-boundaries.md`'s "not yet consumed" note for
  `OrderConfirmed`/`TicketIssued` is superseded for Communication
  specifically: they now have a subscriber, just not yet real delivery.
- Communication is the first service in this repo to ship a `tests/`
  project (`xunit`/`Shouldly`/`NSubstitute`/`Testcontainers.PostgreSql`/
  `NetArchTest.Rules` were already CPM-pinned and unused), and CI gained its
  first `dotnet test` step — zero risk to the other five services, which
  still have no test projects to run.

## Alternatives considered

- **Single vendor (ACS only)** — rejected per this pass's explicit
  requirement that both ACS and Twilio be addressable under the port.
- **Twilio for email too** — rejected; Twilio's email product is SendGrid,
  a separate SDK/package, out of scope for this pass.
- **Database-stored, organizer-editable templates** — rejected for now; no
  requirement calls for runtime editability, and embedded resources are
  simpler, code-reviewed, and versioned with the code that uses them. The
  port (`ITemplateStore`) already allows a DB-backed adapter later.
- **Adding `RecipientEmail`/`RecipientPhone` fields to `OrderConfirmed`/`TicketIssued`**
  — rejected; Ordering and Ticketing don't have that data either, so this
  would relocate the gap rather than close it, and would couple two other
  services' contracts to a Communication-service concern.
- **Leaving the two subscribers completely unwired** — rejected; this repo
  already has a "not yet consumed" precedent for that, but it would forfeit
  proving the dedup-ledger/redelivery-safety machinery end-to-end, and both
  events' XML docs already declare "consumed by notifications" as intent.

## References

- `services/payments/Payments.Infrastructure/DependencyInjection.cs` — the
  config-gated adapter-selection pattern this reuses.
- `services/ticketing/Ticketing.Api/Endpoints/TicketingEndpoints.cs` — the
  Dapr pub/sub subscriber pattern this reuses.
- `services/communication/README.md`, `services/communication/CLAUDE.md`.
