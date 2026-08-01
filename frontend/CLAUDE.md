# CLAUDE.md — Frontend

Inherits the [root CLAUDE.md](../CLAUDE.md), but the C# golden rules
(Central Package Management, XML docs, StyleCop, etc.) do **not** apply
here — this project has its own conventions, below.

## Responsibility

The buyer + organizer web app: one React SPA, two Ant Design–themed
sections (`BuyerLayout`, `AdminLayout`) sharing one login flow, one HTTP
client, and one router. See ADR-0015.

## Owns

- **No data store.** Everything is fetched from the gateway.
- **Public routes:** buyer event browsing (anonymous). Everything else sits
  behind `ProtectedRoute` (logged in — not a role check; see below).

## Structure

```
src/
  router/        AppRouter (route tree), ProtectedRoute
  theme/         buyerTheme.ts, adminTheme.ts — Ant ThemeConfig token sets
  i18n/          react-i18next bootstrap + locales/en/*.json
  contexts/      AuthContext (provider) + useAuth + authContextValue (split
                 three ways only so react-refresh's lint rule is happy —
                 treat them as one unit)
  services/
    http/        the ONE axios instance + interceptors + tokenStore
    auth/        dev-login API client
    catalog/ inventory/ ordering/ ticketing/   typed API client per bounded
                 context, hand-written types mirroring the backend DTOs
  components/common/
    skeletons/   ListSkeleton, TableSkeleton, CardSkeleton, DetailSkeleton
    errors/      UnauthorizedPage (401→403 visual), NotFoundPage (404),
                 ServerErrorPage (500), RouteErrorBoundary
    feedback/    toast.ts + ToastHolder (stashes Ant's message/notification
                 instances so non-React code can call toast.*)
  layouts/       BuyerLayout, AdminLayout — each wraps its own <ConfigProvider>
  pages/auth/    LoginPage (dev-login form), LogoutPage
  features/
    buyer/  events/ (list + detail) seatmap/ (interactive picker) checkout/
            (hold summary + countdown) orders/ (order+tickets, order history)
    admin/  events/ (list, create, detail with seat-map form + publish)
            inventory/ (SeatBlockPanel) orders/ (tenant order list)
            tickets/ (ScanTicketPage — check in a ticket by its scan token)
  utils/         formatMoney, eventStatusColor — small, shared across features
```

## Design notes

- **Only the gateway.** Never call a backend service's port directly —
  always `VITE_GATEWAY_BASE_URL` (the gateway). CORS only exists there.
- **Auth is dev-login today, real auth later.** `AuthContext.loginWithDevCredentials`
  calls the gateway's `POST /api/auth/dev-login` — a safe stand-in until
  Identity (buyer OTP) and Entra External ID (organizer OIDC) exist (ADR-0015,
  ADR-0016). Swapping it out should not require touching `ProtectedRoute` or
  any page that calls `useAuth()`.
- **`role` is UI routing, not security.** The dev-login `role` claim decides
  which themed section a user lands in after login. It is not enforced by
  any backend authorization check today — `ProtectedRoute` only checks "is
  anyone logged in," never "is this the right role."
- **Token storage: sessionStorage, explicitly, for now.** Scopes an XSS leak
  to the tab's lifetime. The hardened target — an httpOnly/Secure/SameSite
  cookie issued and read only by the gateway, plus CSRF protection — is
  required before a real production rollout, not before. See ADR-0015.
- **One axios instance.** `services/http/client.ts` is the only place a
  request leaves the browser. Request interceptor attaches the bearer token;
  response interceptor clears the session and dispatches
  `SESSION_EXPIRED_EVENT` on 401, toasts on 403/5xx/network errors.
- **Toast without a React context.** `toast.*` (in `components/common/feedback`)
  is a plain module so the axios interceptor (outside React) can call it;
  `ToastHolder` mounts once near the root and stashes Ant's instances into it.
- **`GET /v1/events?mine=true` vs the plain public list.** The buyer events
  list and the admin events list call the _same_ Catalog endpoint with
  different query params — `mine=true` switches from "everyone's non-draft
  events" to "only my tenant's events, any status." Don't try to reuse one
  fetch for both; the visibility semantics are genuinely different.
- **Order lists need `mine=true` or `forTenant=true`, never neither.** Ordering
  has no "list everything" mode — the endpoint 400s without one of these.
  `orderingApi.listMyOrders`/`listTenantOrders` set the right one; don't call
  the raw endpoint without going through them.

## Local run

```bash
cp .env.example .env.development.local   # once; points at the gateway
npm install
npm run dev
```

Needs the gateway (and ideally the 5 backend services) running — see
`docs/local-e2e-walkthrough.md`.

```bash
npm run lint         # ESLint
npm run format:check # Prettier
npm run typecheck    # tsc -b
npm run build        # production build
```

## Do not

- Call a backend service directly — only the gateway.
- Store a token in `localStorage` — `sessionStorage` only (see above).
- Treat the dev-login `role` claim as a security boundary — it isn't one.
- Add a UI library beyond Ant Design without discussion.
