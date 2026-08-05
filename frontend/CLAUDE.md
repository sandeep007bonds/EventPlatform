# CLAUDE.md — Frontend

Inherits the [root CLAUDE.md](../CLAUDE.md), but the C# golden rules
(Central Package Management, XML docs, StyleCop, etc.) do **not** apply
here — this project has its own conventions, below.

## Responsibility

The buyer + organizer web app: one React SPA, two Ant Design–themed
sections (`BuyerLayout`, `AdminLayout`), each with its own real login flow
(buyer OTP, organizer email+password), sharing one HTTP client and one
router. See ADR-0015, ADR-0016, ADR-0023.

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
    auth/        identityApi (buyer OTP) + organizerApi (organizer
                 email+password) API clients
    catalog/ inventory/ ordering/ ticketing/   typed API client per bounded
                 context, hand-written types mirroring the backend DTOs
  components/common/
    skeletons/   ListSkeleton, TableSkeleton, CardSkeleton, DetailSkeleton
    errors/      UnauthorizedPage (401→403 visual), NotFoundPage (404),
                 ServerErrorPage (500), RouteErrorBoundary
    feedback/    toast.ts + ToastHolder (stashes Ant's message/notification
                 instances so non-React code can call toast.*)
  layouts/       BuyerLayout, AdminLayout — each wraps its own <ConfigProvider>
  pages/auth/    BuyerLoginPage (OTP flow), OrganizerLoginPage
                 (email+password register/login), LogoutPage
  features/
    buyer/  events/ (list + detail) seatmap/ (interactive picker) checkout/
            (hold summary + countdown) orders/ (order+tickets, order history)
            auth/ (OtpLoginFlow)
    admin/  events/ (list, create, detail with seat-map form + publish)
            inventory/ (SeatBlockPanel) orders/ (tenant order list)
            tickets/ (ScanTicketPage — check in a ticket by its scan token,
                      manual/hardware-wedge input plus BarcodeDetector/jsQR
                      camera scanning)
            auth/ (OrganizerAuthFlow)
  types/         ambient declarations not yet in TS's bundled DOM lib
                 (barcode-detector.d.ts)
  utils/         formatMoney, eventStatusColor — small, shared across features
```

## Design notes

- **Only the gateway.** Never call a backend service's port directly —
  always `VITE_GATEWAY_BASE_URL` (the gateway). CORS only exists there.
- **Both roles have real login now — dev-login has no UI presence.**
  `AuthContext.loginWithOtp` calls the gateway-routed Identity endpoints
  (`POST /api/identity/v1/otp/request`/`verify`) — the buyer's real login
  path (ADR-0016); the identity gate for buyers sits on the "Hold selection"
  action (`SeatSelectionPage`), not an upfront login wall — see
  `OtpLoginFlow`. `AuthContext.registerOrganizer`/`loginWithOrganizerCredentials`
  call the gateway-routed Identity organizer endpoints
  (`POST /api/identity/v1/organizers/register`/`login`) — the organizer's
  real login path (ADR-0023); see `OrganizerAuthFlow`. `/login` (buyer) and
  `/admin/login` (organizer) render different components accordingly. The
  gateway's `POST /api/auth/dev-login` endpoint still exists for
  curl/script testing (like `scripts/dev-token.sh`) but nothing in the
  frontend calls it any more — `LoginPage.tsx`/`authApi.ts` were deleted.
- **`role` is UI routing, not security.** The `role` claim on either token
  decides which themed section a user lands in after login. It is not
  enforced by any backend authorization check today — `ProtectedRoute` only
  checks "is anyone logged in," never "is this the right role."
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
- Treat the `role` claim as a security boundary — it isn't one.
- Reintroduce a dev-login path into the UI — both roles have real login now.
- Add a UI library beyond Ant Design without discussion.
