# ADR-0022 — Buyer-facing checkout endpoints derive tenant from the resource, not the caller's claim

- **Status:** Accepted
- **Date:** 2026-08-01

## Context

ADR-0011 established that tenant context is "trusted only from the token
claim, never from the request body," and every endpoint enforced that by
requiring `ITenantContext.TenantId` (populated purely from the JWT
`tenant_id` claim, see `TenantContextMiddleware`) to be non-null. That's
correct for organizer/admin-facing endpoints, where the caller genuinely
acts *as* a tenant.

It breaks down for two buyer-facing endpoints — `POST /v1/holds`
(`HoldService.PlaceHoldAsync`) and `POST /v1/checkout`
(`OrderingEndpoints.CheckoutAsync`) — because a real buyer is not
tenant-scoped: they buy from many different organizers over time, and
Identity's future buyer-OTP token (ADR-0016, deferred) will not carry a
`tenant_id` claim at all. Both endpoints previously 401'd unconditionally
when the claim was absent, before even looking at the request. This has been
invisible in all testing so far because dev-login
(`gateways/EventPlatform.Gateway/DevAuth/DevTokenIssuer.cs`) always stamps a
`tenant_id` claim regardless of role, defaulting to a fixed dev tenant guid.

In both cases the real tenant — the organizer who owns the event/inventory
being acted on — is already available server-side, from a resource
populated at an earlier, genuinely-trusted claim (the organizer's, at
publish/hold time), not from anything the buyer supplies:

- Inventory: `EventInventorySettings.TenantId`, written once by
  `InventoryProvisioningService.ProvisionAsync` from Catalog's
  `EventPublished.TenantId`, and already fetched early in
  `HoldService.PlaceHoldAsync` for the existing on-sale/booking-cutoff/
  buyer-limit checks.
- Ordering: `Hold.TenantId` (set at hold-placement time, itself now
  resource-derived per this same change), read back as the very first step
  of the checkout saga (`FetchHoldActivity` → `HoldSnapshot.TenantId`) but
  previously ignored in favor of the claim-derived value threaded through
  `CheckoutWorkflowInput`.

Precedent for "derive from the resource, not (only) the caller's claim"
already exists in Catalog: `Event.IsVisibleTo(Guid? callerTenantId)` uses the
loaded `Event.TenantId` as the source of truth and only consults the
caller's claim to decide Draft-visibility for cross-tenant/anonymous
callers.

Buyer *ownership* (as opposed to tenant) is unaffected by this change — it
already comes from the JWT `sub` claim (`userId`), not `tenant_id`, and
stays exactly as enforced today (`HoldService`'s `userId` checks on
release/reap, `CheckoutWorkflow`'s `hold.UserId != input.UserId` check).

Organizer/admin-facing endpoints (`BlockSeatsAsync`/`UnblockSeatsAsync` in
Inventory, `GET /v1/orders?forTenant=true` in Ordering) are **unaffected** —
`SeatBlockingService` genuinely uses `tenantId` as an authorization filter
(`items.Any(item => item.TenantId != tenantId)`), not just write-metadata,
so those correctly keep requiring the caller's own claim.

## Decision

For the two buyer-action paths, the tenant used to stamp downstream
writes/events is derived from the resource the buyer is acting on, and the
endpoint no longer requires a `tenant_id` claim to be present:

- `POST /v1/holds`: `HoldService.PlaceHoldAsync` no longer takes a
  caller-supplied `tenantId` parameter — it reads `EventInventorySettings.TenantId`
  for the event being held (a new `PlaceHoldOutcome.EventNotFound` covers the
  defensive case where the event hasn't been provisioned yet).
- `POST /v1/checkout`: `CheckoutWorkflow.RunAsync` uses `hold.TenantId`
  (already fetched as step 1 of the saga) for every downstream activity
  input, instead of a value threaded through `CheckoutWorkflowInput`, which
  drops its `TenantId` field entirely.

Checkout idempotency, previously keyed `(tenant_id, idempotency_key)`, moves
to `(user_id, idempotency_key)` — `Order.UserId` is always populated from
the JWT `sub` claim, present on every token including a future buyer token,
and a checkout attempt is fundamentally a buyer action, not a tenant action.
This also removes the last reason `CheckoutAsync` needed a tenant claim
before the workflow even starts (the idempotency pre-check runs before the
hold, and thus its tenant, is fetched).

This does **not** change `ITenantContext`/`TenantContext`/
`TenantContextMiddleware` (`building-blocks/EventPlatform.Hosting/`) — they
stay claim-only, per ADR-0011. This ADR does not supersede ADR-0011's
general principle; it is a scoped, documented exception for the specific
paths where a buyer's own resource already carries a more correct tenant
value than a claim a real buyer token will never have — the same "new ADR
carves out an exception, doesn't edit an Accepted one" pattern ADR-0017 uses
for ADR-0002/ADR-0005.

Dev-login itself is **not** changed in this pass (see Alternatives) —
instead, `scripts/dev-token.sh` gained an opt-out (`TENANT_ID=""` omits the
`tenant_id` claim from the minted token) so this fix is exercisable
end-to-end locally without waiting for a real Identity service.

## Consequences

- `POST /v1/holds` and `POST /v1/checkout` no longer 401 a caller whose
  token has no `tenant_id` claim — a precondition for the deferred
  Identity/buyer-OTP service (ADR-0016) to actually issue such tokens.
- `HoldService.PlaceHoldAsync` loses its `tenantId` parameter;
  `CheckoutWorkflowInput` loses its `TenantId` field.
- `Order`'s idempotency-key uniqueness moves from `(tenant_id,
  idempotency_key)` to `(user_id, idempotency_key)` — a schema change
  requiring a local dev database rebuild (`EnsureCreatedAsync()`, no EF
  migrations yet: `./scripts/dev-down.sh -v && ./scripts/dev-up.sh`).
- Two different buyers reusing the same idempotency-key string now succeed
  as two independent orders — previously impossible to observe distinctly
  from a tenant-scoped collision, since dev-login always supplied the same
  dev tenant.
- A malformed/malicious `EventInventorySettings`/`Hold` row is now the only
  way to stamp a wrong tenant on a buyer-initiated `Hold`/`Order` — but both
  are server-written, never client-supplied, so this is not a new
  attacker-controlled input.
- Dev-login (`DevTokenIssuer.cs`) still always stamps `tenant_id` by
  default, so exercising the buyer-token-without-tenant-claim path through
  the gateway's dev-login isn't possible without a separate, deliberately
  out-of-scope change; `scripts/dev-token.sh`'s `TENANT_ID=""` opt-out is the
  supported way to test this locally today.

## Alternatives considered

- **Keep requiring the claim, have Identity always mint a placeholder
  `tenant_id`** — rejected: fabricating a tenant on a token that has no
  natural tenant is exactly the "trust it from somewhere client-adjacent"
  pattern ADR-0011 exists to prevent; better for paths that don't need a
  tenant to simply not require one.
- **Keep `(tenant_id, idempotency_key)` idempotency, require an
  organizer-selected tenant at checkout** — rejected: nonsensical from the
  buyer's perspective (they chose a hold, not a tenant) and reintroduces the
  exact claim dependency this ADR removes.
- **Change `DevTokenIssuer.cs`/the gateway's dev-login to support a
  buyer-role token with no tenant claim** — rejected for this pass: `DevLoginRequest.TenantId`
  being `null` is currently indistinguishable from "not specified" (always
  replaced by a default), so supporting a true opt-out would require a new
  signal and cascade into `DevLoginUser.TenantId` becoming nullable, which
  in turn touches the frontend's hand-written `authApi.ts` type contract —
  real scope creep into a different project's conventions for a backend
  correctness fix. `scripts/dev-token.sh`'s opt-out achieves the same
  verification goal with no cross-project surface.

## References

- `services/inventory/CLAUDE.md`, `services/ordering/CLAUDE.md` — updated
  design-notes sections.
- ADR-0011 — the general tenant-from-claim principle this ADR carves a
  scoped exception out of.
- ADR-0016 — the deferred Identity/buyer-OTP service this change is a
  precondition for.
