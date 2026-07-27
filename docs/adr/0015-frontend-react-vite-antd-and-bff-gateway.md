# ADR-0015 — Frontend: React + Vite + Ant Design, fronted by a YARP BFF gateway

- **Status:** Accepted
- **Date:** 2026-07-27

## Context

EventPlatform had five backend services and no UI at all — buyers and
organizers could only interact via curl. None of the five services has a
CORS policy, and none has ever needed one, so a browser could not call them
directly. No gateway existed either. No real identity provider is configured
today: only a shared-secret dev HS256 JWT path
(`AuthenticationExtensions.cs`'s `DevSigningKey` branch, driving
`scripts/dev-token.sh`) and an unconfigured "Production" OIDC branch.
`scripts/dev-token.sh` mints tokens client-side using a shared secret, which
a browser cannot safely replicate — the secret would ship to every client.

Design reference for the buyer-facing look: PouchNation's storefront
(`venue.pouchnation.com`) — teal accent, off-white ground, centered card,
friendly illustration on auth screens.

## Decision

- **A thin YARP reverse-proxy gateway** (`gateways/EventPlatform.Gateway`)
  becomes the frontend's one entry point. Explicit per-endpoint route
  allowlist, never a blanket per-service proxy — saga-internal endpoints
  (Inventory's hold convert/release, Payments' charge/refund/webhook) are
  deliberately never routed. The gateway forwards `Authorization` unchanged
  (auth pass-through, not re-validation) and owns the only CORS policy in
  the platform. It runs as a plain process with no Dapr sidecar — it is a
  browser-facing ingress concern, not a service participating in pub/sub or
  service invocation.
- **A Development-only dev-login endpoint** (`POST /api/auth/dev-login`) on
  the gateway mints tokens server-side, mirroring `scripts/dev-token.sh`'s
  claim shape. Gated the same way every service already gates
  `DevSigningKey` (only mapped when configured), plus a fail-fast guard: the
  host throws at startup if `Jwt:DevSigningKey` is set while
  `ASPNETCORE_ENVIRONMENT=Production`. This is a stand-in until Identity
  (buyer OTP) and Entra External ID (organizer OIDC) exist — see ADR-0016.
- **React + Vite + TypeScript**, not Next.js. This is an authenticated
  SPA behind login for both buyer and organizer journeys today; there is no
  server-side-rendering need yet. A future organizer-branded, SEO-indexed
  public marketing/CMS site (explicitly out of scope for this phase) is the
  kind of workload that would justify SSR, and can be a separate app/initiative
  when it exists.
- **Ant Design**, themed per-section via a nested `<ConfigProvider>` at
  `BuyerLayout` and `AdminLayout` — one `ThemeConfig` token set per section,
  not two separate component libraries or a runtime CSS-in-JS theme engine.
  Same primary accent color across both for brand consistency; buyer gets
  larger radius and a softer ground (PouchNation reference), admin stays
  close to Ant's information-dense defaults.
- **One React app, two themed sections** — not true micro-frontends. A
  single Vite build, a single router, a single `AuthContext`; `BuyerLayout`
  and `AdminLayout` are just two branches of one route tree. Splitting into
  separately-deployed micro-frontends is unwarranted complexity at this
  scale and can be revisited if the two sections ever need independent
  release cadences.
- **React Context + hooks for auth state**, not a DI container. `AuthContext`
  wraps `services/http/tokenStore` (a plain module, since axios interceptors
  run outside React's render cycle) and exposes `useAuth()`. This matches
  how the rest of the ecosystem (react-router, react-i18next) already works
  and avoids introducing a DI pattern this codebase doesn't otherwise use.
- **Token storage: sessionStorage for phase 1, explicitly, not implicitly.**
  Scopes an XSS token leak to the tab's lifetime rather than indefinitely.
  The hardened target state — an httpOnly/Secure/SameSite cookie issued and
  read only by the gateway, plus CSRF protection — is required before a real
  production rollout, not before this phase.
- **ESLint (flat config) + Prettier**, not the Vite template's default
  Oxlint. Standard, widely understood tooling for a team already used to
  StyleCop/analyzers-as-errors on the .NET side; own `.editorconfig`
  (`root = true`) since the repo root's is C#-only.

## Consequences

- The five backend services need **zero code changes** to be called from a
  browser — CORS and auth pass-through live entirely in the new gateway.
- A real login system (Identity + Entra External ID, ADR-0016) can replace
  dev-login later without touching `ProtectedRoute` or any page that calls
  `useAuth()` — `AuthContext`'s public shape (`user`, `logout`) does not leak
  how login happened.
- Because dev-login's `role` claim is UI-routing-only and not enforced by
  any backend authorization check, buyer/admin separation in the frontend is
  "which section are you in," not a security boundary, until real
  role/claim enforcement exists server-side.
- The sessionStorage/bearer-token choice is a known, documented gap versus
  the hardened cookie-based target state — tracked here so it isn't
  mistaken for the final design.

## Alternatives considered

- **Next.js (SSR)** — rejected for now; no current SSR/SEO need, and it
  would add build/deploy complexity (a Node server, not just a static
  bundle) for no present benefit. Revisit if/when the CMS-driven public
  marketing site is built.
- **True micro-frontends** (separately built/deployed buyer and admin
  apps) — rejected as premature; one app with two themed sections meets
  today's needs with far less operational overhead.
- **A DI container** (e.g. InversifyJS) for services — rejected; React
  Context + hooks is idiomatic here and the app's dependency graph is small
  enough not to need one.
- **httpOnly cookie + gateway-issued session from day one** — the more
  secure end-state, but it requires the gateway to own session issuance
  and CSRF protection, which doesn't exist yet. Deferred, not rejected —
  see the sessionStorage note above.

## References

- PouchNation (`venue.pouchnation.com`) — buyer-facing design reference.
- `gateways/EventPlatform.Gateway/CLAUDE.md`, `frontend/CLAUDE.md`.
