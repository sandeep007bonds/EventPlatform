# PouchNation feature audit → gap analysis vs EventPlatform

## Context

Goal: log into `https://events.pouchnation.com/`, explore every screen end to end with
screenshots, note every feature, and produce a **feature-by-feature gap table against
EventPlatform** — what they have, what we have, what's missing. Underlying question:
*is their platform similar to ours?*

**This session could not do it.** The environment's egress policy refused the connection:

```
"kind": "connect_rejected",
"detail": "gateway answered 403 to CONNECT (policy denial or upstream failure)",
"host": "events.pouchnation.com:443"
```

Not host-specific — `example.com` fails identically. Only GitHub and package registries are
reachable, which is why `git push` works and nothing else does. The proxy's own docs
(`/root/.ccr/README.md`) state a 403 is an organization policy denial that must be reported,
never retried or routed around. Chromium and Playwright **are** installed and working, so the
browser was never the obstacle.

Chosen route: **open network access, then run the crawl in a new session.** This file carries
everything that session needs so it starts warm.

⚠️ The credentials were pasted into the previous session's transcript. Rotate that password
regardless of whether the audit runs.

## Step 1 — open egress for the host

Network policy is fixed at environment creation, so this needs an environment whose policy
permits `events.pouchnation.com` (and whatever CDN/auth hosts its login depends on — expect at
least one third-party auth or asset domain). See
https://code.claude.com/docs/en/claude-code-on-the-web.

Verify before crawling — one command, and it must print a real status code:

```bash
curl -sS -o /dev/null -w "status=%{http_code}\n" -L --max-time 25 https://events.pouchnation.com/
curl -sS "$HTTPS_PROXY/__agentproxy/status" | grep -A5 recentRelayFailures
```

If that still 403s, stop and report it — do not attempt another route to the host.

## Step 2 — crawl methodology

Drive Chromium via Playwright. Do **not** run `playwright install` (browsers are at
`/opt/pw-browsers`, `PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1` is set); if a version pin fights you,
launch with `executablePath: '/opt/pw-browsers/chromium'`.

Ground rules for the crawl:

- **Read-only.** Explore, filter, open, expand, paginate. Do **not** create, edit, delete,
  publish, refund, or send anything — it is a live account and side effects are real. If a
  feature can only be understood by exercising it, note that and move on.
- Screenshot every distinct screen and every non-obvious modal to
  `screenshots/<area>/<screen>.png`, full-page.
- Capture the **navigation tree first** (sidebar, tabs, account menu, per-row action menus),
  then walk it breadth-first so nothing is missed if the session is cut short.
- Note per screen: what the feature does, the fields/columns it exposes, what is configurable,
  and anything with no EventPlatform equivalent.
- Watch the network tab for API shapes — they reveal capabilities the UI hides.
- Keep running notes in `docs/research/pouchnation-audit.md` as you go, not at the end.

**Prior to verify, not assume:** PouchNation is positioned as event/venue technology beyond
ticketing — RFID wristbands, cashless top-ups/POS, access control, guest analytics. If that
holds, the honest headline is "overlapping but broader scope," and the gap table needs a
clearly-marked *out-of-scope-for-us* section so genuine ticketing gaps aren't drowned in
hardware/cashless features we never intended to build. Confirm from the product itself.

## Step 3 — EventPlatform's side of the table (already derived, reuse this)

Nine services behind a YARP gateway; React SPA. Endpoints as they exist today:

| Area | What EventPlatform has |
|---|---|
| **Events (Catalog)** | Create/list/get, publish, Draft-only detail edits, pause/resume sales, inline location, `EventGroup` tours with multi-leg create, entry gates, on-sale/booking-cutoff windows, max tickets per buyer |
| **Seat maps** | Reserved seats + general-admission sections, add/replace/remove sections, per-section price tier + entry gate |
| **Pricing** | Price tiers per section; **promo codes** (percentage/fixed, validity window, tier scoping, total + per-buyer caps, public/private) and **per-event tax** applied after discount (ADR-0034) |
| **Inventory** | Redis-backed holds with TTL + extension, no-oversell (Testcontainers-proven), GA allocations, seat block/unblock, expiry reaper |
| **Queue** | Virtual waiting room: paced admission, HMAC admission tokens, per-event settings tunable post-publish (ADR-0026) |
| **Checkout (Ordering)** | Dapr Workflow saga, idempotency-keyed, `POST /v1/checkout/quote` price preview, async Stripe Payment Element w/ 3DS + UPI (ADR-0028), buyer-initiated cancel + refund saga |
| **Payments** | Stripe PaymentIntents, refunds, webhook + buyer-nudged + polled reconciliation, PCI SAQ-A |
| **Ticketing** | QR tickets, scan/check-in with event scoping + time window + gate checks, warm local scan cache (ADR-0025), void on cancel |
| **Comms** | Transactional email on order confirmed / tickets issued |
| **Identity** | Buyer OTP login, organizer email+password, JWKS/OIDC discovery |
| **Buyer UI** | Anonymous browse → seat picker → queue → hold → checkout → order + tickets, order history |
| **Organizer UI** | Event list/detail, seat-map editor, tours, promo codes, seat blocking, queue settings, order list, scan page |

**Known gaps already on our tracker** — check each against PouchNation first, since a hit here
is immediately actionable:

- P3 archive/purge consumed tickets · P7 complimentary/comp tickets · P8 RBAC + multi-user
  per tenant · P9 multi-event cart
- S1 bot/CAPTCHA defense · S2 API rate limiting · S3 fraud detection · S4 real RBAC
  enforcement · S5 hardened token storage · S6 audit log
- Within P4: per-line discount allocation, post-publish tax edits, code stacking,
  tax-inclusive pricing
- No reporting/analytics surface at all · no refund UI for organizers · no seated-map
  *visual* designer (sections are form-defined) · no marketing/CRM features

## Step 4 — deliverable

`docs/research/pouchnation-gap-analysis.md`:

1. **Verdict** — how similar it actually is, in a short paragraph, answering the original
   question directly.
2. **Gap table** — Feature | PouchNation | EventPlatform | Gap | Notes. Sorted so real gaps
   surface first.
3. **Out-of-scope section** — their features that are deliberately not our product.
4. **Recommended backlog** — the gaps worth acting on, ordered, cross-referenced to existing
   P/S tracker IDs where they already exist so nothing is filed twice.
5. Screenshots linked inline from `screenshots/`.

## Unfinished, unrelated to this audit

- `dotnet test` has never been run — `OrderPricingCalculatorTests` compiles but has not executed.
- Unconfirmed whether commit `cd23e34` silenced the `fail:` lines on the user's machine.
