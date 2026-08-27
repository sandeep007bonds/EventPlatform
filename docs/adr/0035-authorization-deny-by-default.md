# ADR-0035 — Authorization: role policies at the route, deny-by-default last

**Status:** Accepted · **Date:** 2026-08-20

## Context

An audit of the endpoint surface found that **no endpoint in the platform was protected by the
authorization system at all**. There is no `RequireAuthorization()` anywhere in the codebase and
no fallback policy, so `app.UseAuthorization()` — which every service calls — enforces nothing.
In ASP.NET Core, an endpoint carrying no authorization metadata is anonymous; silence is not a
deny.

What stood in for it was a per-handler convention: read `ITenantContext.TenantId` or the `sub`
claim, return `Unauthorized()` when absent. That works right up until a handler forgets, and
three had:

| Endpoint | Returned to anyone with an id |
|---|---|
| `GET /v1/orders/{id}` | the full order, **including the Stripe `PaymentClientSecret`** |
| `GET /v1/tickets/{id}` | the ticket, **including its scan `Token`** |
| `GET /v1/orders/{orderId}/tickets` | the same tokens for a whole order |

All three were routed publicly at the gateway. The client secret can act on a buyer's payment;
the scan token admits its bearer at the gate. Order and ticket ids are bare GUIDs in URLs, which
makes them hard to guess — obscurity, not access control.

The tell that this was drift rather than design: `GET /v1/tickets/{id}/qrcode` **did** carry an
ownership check. Someone protected the QR *image* of the token while the endpoint returning the
same token as text went unguarded.

Separately, the `role` claim (`buyer`/`organizer`) that Identity mints was never checked by any
backend. It drove which themed section the SPA rendered and nothing else — `ProtectedRoute` asks
"is anyone logged in", never "is this the right role" (tracked as S4).

## Decision

### Two distinct questions, both required

**Who may call this endpoint** is answered at the route, by policy. **Which records they may
reach** is answered in the handler, by an ownership check. An organizer being an organizer says
nothing about whether a given event is theirs, so a policy alone is never sufficient and the
extension methods' XML docs say so explicitly.

`EventPlatform.Hosting` gains:

- **`EventPlatformClaims`** — `sub`, `role`, `tenant_id`, and the `buyer`/`organizer` role values,
  so no service spells one by hand. `TenantContextMiddleware` now reads the shared constant.
- **Two policies** — organizer and buyer, each requiring an authenticated user plus the matching
  `role` claim.
- **`RequireOrganizer()` / `RequireBuyer()` / `RequireAuthenticatedCaller()`** route-builder
  extensions.

`RequireAuthenticatedCaller` exists for endpoints a buyer and an organizer both legitimately
reach — reading an order, which its buyer and the selling tenant can each see.

### Ownership: opaque not-found, never forbidden

A caller who does not own a record gets **404, not 403**. A 403 confirms the record exists, which
turns a public URL carrying a GUID into an existence oracle. This matches the pattern
`DefineSeatMap` and `PublishEvent` already used for tenant mismatches.

`PaymentClientSecret` is returned **only to the buyer**, never to the selling tenant's organizer.
It exists so the buyer can resume Payment Element after a reload or a 3-D Secure redirect; an
organizer reading their tenant's order has no use for it and should not hold a credential that
can act on someone else's payment.

### Deny-by-default goes on last, deliberately

A fallback policy requiring an authenticated user is the right end state, but applying it first
would have denied nearly every endpoint at once — including the Dapr pub/sub subscribers at
`/integration/*`, which the sidecar calls with no user token. Denying those silently breaks every
integration event in the platform, and the failure mode is messages that stop flowing rather than
an obvious error.

So the rollout was staged, and each stage was independently shippable:

1. Primitives, with no fallback (done).
2. Annotate every endpoint explicitly, service by service — including `AllowAnonymous` on the
   `/integration/*` subscribers, health checks and OpenAPI (done).
3. Set the fallback policy, so a new endpoint added later is protected unless its author says
   otherwise (done).

Step 3 was the point of the exercise. Steps 1–2 without it leave the next forgotten handler
exactly as exposed as the three above.

### What step 3 turned up

Annotating "every endpoint" in step 2 meant every endpoint *in an `Endpoints/` file* — which is
what `scripts/check-endpoint-auth.py` looked at, and it passed. Three kinds of endpoint are not
registered there and were all still unannotated:

| Endpoint | Registered in | What the fallback would have done |
|---|---|---|
| `/health/live`, `/health/ready` | `HealthCheckExtensions` | 401 to the kubelet — liveness failing restarts every pod, on all eight services |
| `/openapi/v1.json`, `/scalar/v1` | `OpenApiExtensions` | 401, taking the Scalar UI with it |
| `/dapr/subscribe` | five `Program.cs` files | sidecar registers no subscriptions — pub/sub dies exactly as quietly as the failure mode above |

All are now explicitly `AllowAnonymous`, and the checker has a second sweep covering
`Program.cs` and `EventPlatform.Hosting` so the same class of gap fails the build rather than the
cluster. The lesson generalises: "the checker passes" is a statement about the checker's glob.

`AllowAnonymous` is what makes this safe — ASP.NET Core's authorization middleware short-circuits
on `IAllowAnonymous` metadata *before* evaluating the fallback policy, so an explicitly-anonymous
endpoint is unaffected by it.

## Consequences

- The three IDORs are closed, and the ownership pattern is now uniform across order and ticket
  reads.
- **An endpoint with no annotation is now denied, not anonymous.** A forgotten annotation surfaces
  as a 401 the first time the endpoint is exercised, which is a bug report rather than a breach.
  The cost is that "public" must be said out loud, every time.
- Service-to-service calls over Dapr carry no user token, so every endpoint they reach is
  explicitly `AllowAnonymous` — the `/integration/*` subscribers, the subscribe manifest, and
  Inventory's `GET /v1/holds/{id}/snapshot`, which exists precisely so the public hold endpoint
  could require a buyer without breaking the checkout saga's first step. Anonymous here means
  "no user token", not "unauthenticated caller from outside": these endpoints are
  `ExcludeFromDescription` and not routed by the gateway. A real service identity for those calls
  is **not** in scope here, and remains the honest gap.
- The `role` claim is now a real security boundary on the backend, which it was not before.
  `frontend/CLAUDE.md` has been corrected accordingly: route guards there remain cosmetic, and the
  API is where the door is.
- Multi-user-per-tenant and finer-grained organizer permissions (P8) are **not** addressed. This
  establishes only buyer-vs-organizer.

## Alternatives considered

- **Flip the fallback policy first, fix breakage after.** Fails safe in the authorization sense
  and catastrophically in the operational one: the first thing to break is asynchronous message
  handling, which fails quietly.
- **Keep per-handler checks and just fix the three misses.** Cheapest, and leaves the same trap
  for the next endpoint. The three were found by an audit, not by a test — nothing would have
  caught the fourth.
- **Enforce authorization at the gateway instead.** Attractive because the gateway already knows
  which routes are public, but it puts the security boundary in a component that can be bypassed
  by anything reaching a service directly, including other services and anything inside the
  cluster.
