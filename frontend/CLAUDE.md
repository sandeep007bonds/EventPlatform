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
    http/        the ONE axios instance + interceptors + tokenStore +
                 requestActivity (in-flight request counter, drives
                 TopProgressBar)
    auth/        identityApi (buyer OTP) + organizerApi (organizer
                 email+password) API clients
    catalog/ inventory/ ordering/ ticketing/   typed API client per bounded
                 context, hand-written types mirroring the backend DTOs
  components/common/
    skeletons/   ListSkeleton, TableSkeleton, CardSkeleton, DetailSkeleton
    errors/      UnauthorizedPage (401→403 visual), NotFoundPage (404),
                 ServerErrorPage (500, route-level only), RouteErrorBoundary,
                 LoadError (inline "couldn't load this" + retry, for a
                 list/panel's own failed GET — see design notes below)
    feedback/    toast.ts + ToastHolder (stashes Ant's message/notification
                 instances so non-React code can call toast.*); TopProgressBar
                 (global top-of-page loading indicator, no third-party dep)
  layouts/       BuyerLayout, AdminLayout — each wraps its own <ConfigProvider>
  pages/auth/    BuyerLoginPage (OTP flow), OrganizerLoginPage
                 (email+password register/login), LogoutPage
  features/
    buyer/  events/ (list + detail) seatmap/ (interactive picker) checkout/
            (hold summary + countdown + Stripe Payment Element via
            CheckoutPaymentForm, plus CheckoutReturnPage for the
            redirect-out-and-back authentication flows) orders/
            (order+tickets, order history) auth/ (OtpLoginFlow)
    admin/  events/ (list, create, detail with seat-map form + publish)
            eventGroups/ (create/manage tours, TourDetailPage with "Add
                          leg", EventGroupPicker + inline quick-create,
                          TourLegsList — a tour's upcoming/past legs)
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
- **Global loading indicator, same pattern.** `services/http/requestActivity.ts`
  is a plain module tracking the in-flight request count; `httpClient`'s request/
  response interceptors call `beginRequest`/`endRequest`. `TopProgressBar`
  subscribes to it and renders a fixed top-of-page bar — mounted once near the
  root, next to `ToastHolder`. No third-party dependency (no NProgress); a small
  hand-rolled indeterminate CSS transition instead. Individual pages/actions
  still use their own skeletons (`components/common/skeletons/`) and per-button
  `loading` props for local state — this bar is only for "something is happening
  somewhere," not a replacement for those.
- **Ant's `message` toasts are pinned to top-right via CSS** (`index.css`) — the
  `message` API has no placement prop and defaults to top-center; `notification`
  (used by `toast.notifyError`) already defaults to top-right.
- **`httpClient`'s response interceptor never auto-toasts, for any request.**
  It used to show a generic toast for 403/5xx — on top of whatever the calling
  page's own `.catch()` already showed, doubling every single failure (load
  _and_ action alike; see git history for the exact bug report). Every call
  site owns its own failure UI now: a data load shows `LoadError` (list/panel —
  tracked as its own `loadError` state distinct from "succeeded with zero
  results," so the two don't get confused) or, for a single-resource top-level
  page, `NotFoundPage` on a genuine 404 vs `ServerErrorPage` on anything else
  (mirrored in `EventDetailPage`/`AdminEventDetailPage`/`OrderPage` — check
  `error.response?.status === 404` before choosing which); an action (save,
  publish, block/unblock, login attempt) shows its own specific `toast.error(...)`.
  The interceptor's only remaining global behavior is clearing the session on a
  401, since that has to happen regardless of which call triggered it.
  `EventGroupPicker`'s own tours fetch degrades fully silently (no `LoadError`
  either) since it's a minor dropdown inside an otherwise-fully-usable form.
- **`CreateEventPage` is a single page/form that creates 1..N legs (cities/dates) in one visit,
  optionally creating their tour inline.** It defaults to looking exactly like a plain
  single-event form — no tour language visible — with one leg card
  (`EventLegFields`, a `Form.List name="legs"` repeater following `SeatMapSectionsFields.tsx`'s
  "one Card per item, dashed 'Add' button, remove hidden once only one item's left" shape). The
  "Add another city/date" button appends another leg card **on the same page** and, once there's
  more than one leg, the tour picker (`EventGroupPicker`) becomes required — a multi-leg batch
  always needs somewhere to attach its legs — auto-switching to `NEW_TOUR_OPTION` (its "+ New
  tour" sentinel) if nothing was already picked. Submission creates the tour first if needed
  (title-only; nothing is created until submit, so an abandoned form never leaves an orphan tour
  behind), then calls `POST /v1/events` **sequentially, one leg at a time** (never
  `Promise.all`) — the server's sibling-overlap check re-queries fresh state per call, so legs
  must land in order for that check to be correct. A newly-created tour's advertised range is
  backfilled afterward from `min`/`max` across all the legs just submitted (`updateEventGroup`) —
  not done when attaching to an _already-existing_ tour, since that tour may have other legs
  outside this submission that a naive min/max would wrongly ignore.
  **No delete/rollback endpoint exists for either `Event` or `EventGroup`**, so a batch that fails
  partway through leaves whatever succeeded permanently saved — `CreateEventPage` tracks each
  leg's status (`pending`/`created`/`failed`) and locks/tags already-created cards so a retry only
  (re-)submits what's left, never re-creating a leg twice; the submit button relabels to "Create
  remaining legs" while any leg has failed.
  `CreateEventGroupPage` (`/admin/tours/new`) still exists separately, for setting a tour's full
  date range/contact/social defaults up front before any legs exist; it lands on `TourDetailPage`
  (`/admin/tours/:id` — dates/contact summary, an "Edit dates" action, + `TourLegsList`, upcoming
  visible/past collapsed), whose "Add leg" button opens the same `CreateEventPage` pre-scoped via
  `?eventGroupId=` — the organizer can still add just one leg, or several at once, from there.
  `EventGroupsPage`'s rows are clickable through to `TourDetailPage`. Both entry points funnel
  into the same `CreateEventPage`/`EventLegFields`/`EventGroupPicker` machinery and the same
  create-then-conditionally-`updateEventGroup` sequence server-side.
- **`GET /v1/events?mine=true` vs the plain public list.** The buyer events
  list and the admin events list call the _same_ Catalog endpoint with
  different query params — `mine=true` switches from "everyone's non-draft
  events" to "only my tenant's events, any status." Don't try to reuse one
  fetch for both; the visibility semantics are genuinely different.
- **Order lists need `mine=true` or `forTenant=true`, never neither.** Ordering
  has no "list everything" mode — the endpoint 400s without one of these.
  `orderingApi.listMyOrders`/`listTenantOrders` set the right one; don't call
  the raw endpoint without going through them.
- **Stripe Payment Element is a deliberate, justified exception to "no UI
  library beyond Ant Design."** `@stripe/stripe-js`/`@stripe/react-stripe-js`
  collect payment details for `CheckoutPage` — but the only UI surface is
  `PaymentElement`, a Stripe-controlled iframe, not a competing
  component/design-system library, so it doesn't collide with that rule's
  actual intent. `services/payments/stripeClient.ts` loads Stripe.js once at
  module scope (`stripePromise`, `isStripeConfigured` off
  `VITE_STRIPE_PUBLISHABLE_KEY`). **The intent is created before the payment
  form ever mounts** (ADR-0028): `CheckoutPage.tsx` calls `checkout(...)`
  directly on submit, which creates (but doesn't confirm) a PaymentIntent
  server-side; only once a `clientSecret` comes back does it mount
  `<Elements stripe={stripePromise} options={{ clientSecret }}>` around
  `CheckoutPaymentForm.tsx` — the only place `useStripe`/`useElements` are
  called (it only ever renders inside `<Elements>`, so there's no
  conditional-hook hazard). `CheckoutPaymentForm` calls
  `stripe.confirmPayment({ elements, confirmParams: { return_url }, redirect:
  'if_required' })` — this handles 3-D Secure/UPI-app-switch authentication
  natively; most methods resolve in-page, the rest redirect out and back via
  `CheckoutReturnPage.tsx` (`/checkout/:holdId/return`, reads Stripe's own
  `payment_intent_client_secret` query param via `stripePromise` directly,
  since there's no `<Elements>` context on that page). Raw payment details
  only ever reach Stripe's own iframe/API, never our backend (PCI SAQ-A).
  When `clientSecret` comes back `null` (payment already resolved
  synchronously — the no-Stripe-configured dev fallback), `CheckoutPage`
  navigates straight to the order page with no payment form ever shown, so
  local/CI checkout needs zero Stripe setup.

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
