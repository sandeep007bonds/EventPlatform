# ADR-0034 — Promo codes and per-event tax: one money model, computed in Ordering

**Status:** Accepted · **Date:** 2026-08-19

## Context

Until now nothing in the platform could adjust a price. `Order.TotalMinor` was a plain
`SUM(OrderLine.PriceMinor)`, each line's price came straight from Catalog's seat-map tier via
Inventory's hold snapshot, and there was no tax anywhere — the amount charged to Stripe was
exactly the sum of the ticket face values.

Two requirements arrived together, and they interact:

1. **Discounts.** An organizer running a real on-sale needs codes they control: early-bird,
   press comps, partner allocations. Percentage *or* fixed amount, with a validity window, a
   restriction to particular price tiers, and caps on how many times a code can be used in
   total and per buyer. Buyers should be able to type a private code *and* pick a public one
   from a list.
2. **Tax.** India/GST is the immediate driver. One rate per event, charged on the discounted
   amount — a discount reduces the taxable base, it does not sit alongside it.

They interact because the answer to "how much is this order?" stops being a single sum and
becomes an ordered calculation. Getting that order wrong is not a rounding bug — it changes
what the buyer is charged and what is remitted.

Two existing facts shaped the design, both already in the codebase before this work:

- `HoldLineSnapshot.PriceTier` already carries the tier from Inventory into Ordering. Tier-scoped
  discounts needed no new propagation; the value was simply being dropped when building
  `OrderLineSpec`.
- Ordering already called Catalog over Dapr (`ICatalogEventClient`) to fetch the event's currency
  as saga step 2. Tax and promo lookups extend that existing seam rather than adding new plumbing.

## Decision

### One money model, stated once

```
subtotal  = Σ line.PriceMinor
discount  = f(code, lines eligible by tier)        # clamped to the eligible subtotal
tax       = round((subtotal − discount) × rate/100, AwayFromZero)
total     = subtotal − discount + tax              # what Stripe charges
```

`TotalMinor` keeps its existing meaning — the payable amount — so `CreateIntentInput`,
`ConfirmInput` and the `OrderConfirmed` contract needed no change. `Order` gains
`SubtotalMinor`, `DiscountMinor`, `TaxMinor`, `TaxRatePercent`, `TaxLabel`, `PromoCodeId`
and `PromoCodeText` alongside it, so a placed order carries its own arithmetic and can be
re-explained to a buyer months later without recomputing anything.

The whole calculation lives in one pure static class,
`Ordering.Domain/OrderPricingCalculator.cs` — no I/O, no clock, no repository. It is the only
place in the platform that decides what someone is charged, and it is unit-tested in isolation.

### Catalog owns the definition; Ordering owns the arithmetic and the counting

A promo code is a property of an event's commercial setup, which is Catalog's business — so
`PromoCode` (with its child `PromoCodeTier`) is a Catalog aggregate, created and deactivated
through Catalog's API, alongside `TaxRatePercent`/`TaxLabel` on `Event`.

But **redemption counting has to happen in Ordering**, because Ordering owns the orders. Catalog
cannot count redemptions without reading another service's database, which the layering rules
forbid outright. So the split is: Catalog answers *what are this code's rules*, Ordering answers
*may this buyer use it now, and what is it worth here*.

`PromoCodeEvaluator` (Ordering.Application) is the single implementation of that second question,
deliberately shared between the `/v1/checkout/quote` preview and the saga's own re-check, so the
price a buyer is quoted and the price they are charged cannot be produced by two different code
paths.

### The quote is advisory; the saga re-prices from scratch

`POST /v1/checkout/quote` creates nothing. It exists so the buyer's "Apply" button has something
to call, and so the breakdown shown above the pay button is the server's arithmetic rather than
the browser's.

The checkout saga re-runs the full evaluation at order creation and recomputes the total itself.
A code can expire, be deactivated, or hit its cap in the seconds between quoting and paying —
and if it does, the checkout **fails** with `CheckoutOutcome.PromoCodeInvalid` (409) rather than
quietly charging full price. The buyer agreed to a discounted total; charging more than they
agreed to, without telling them, would be the worse failure by a wide margin.

### An empty tier list means "all tiers"

`PromoCodeTier` rows restrict a code to particular price tiers; **no rows means no restriction**.
The unrestricted case is the common one, and modelling it as the absence of restrictions means an
organizer discounting a whole order never has to enumerate their tiers — and a tier added to the
seat map later is automatically covered rather than silently excluded.

### Rounding, explicitly

`MidpointRounding.AwayFromZero` on both the percentage discount and the tax. Banker's rounding
(.NET's default) is defensible for aggregates but surprising on a single receipt, where a buyer
can check the arithmetic by hand. Both roundings land on whole minor units; the calculator
assumes a 2-decimal currency (`MinorUnitsPerMajor = 100`), which is true of every currency the
platform accepts today and is flagged in the source where it matters.

### No editing a code after creation

Following `EntryGate`'s precedent: a code that has already been advertised should not silently
change what it is worth. Deactivate it and create another. `Deactivate()` is idempotent.

## Consequences

- **The buyer sees the same breakdown twice** — at checkout (from the quote) and on the placed
  order (from the stored fields) — rendered by one shared `PriceRow` component so the two cannot
  drift apart visually.
- **A rejected code is not an error.** `/v1/checkout/quote` returns 200 with a machine-readable
  `promoCodeRejection` (`NotFound`, `Expired`, `RedemptionLimitReached`, …) alongside the real,
  undiscounted price. The buyer-facing prose for each reason lives in the frontend, where it can
  be translated.
- **Tier applicability is checked before the redemption caps**, so a code scoped to tiers the
  buyer isn't holding reports exactly that, rather than a misleading "fully claimed".
- **An unrecognised discount type parses as `FixedAmount`.** If Catalog and Ordering ever disagree
  about the enum's spelling, a percentage misread as an amount discounts pennies; an amount
  misread as a percentage could discount the entire order. The safe misread is the deliberate
  default.
- **Two migrations are required** — Catalog (`promo_codes`, `promo_code_tiers`,
  `events.tax_rate_percent`, `events.tax_label`) and Ordering (`orders.subtotal_minor` and
  siblings, `order_lines.price_tier`, an index on `orders.promo_code_id`). Per ADR-0029 these are
  generated by `dotnet ef migrations add`, never hand-written; CI's drift guard catches an
  omission.
- **`OrderPricingCalculatorTests` covers the arithmetic** — tier filtering, case-insensitive tier
  matching, the clamp at the eligible subtotal, discount-then-tax ordering, and `.5` rounding on
  both. These were written in an environment with no .NET SDK and have **not been executed**;
  first proof is a CI run.

### Known limits, accepted deliberately

1. **The discount is stored at order level, not allocated per line.** A partial refund of one line
   cannot currently attribute its share of the discount. Allocating it needs a rounding-remainder
   policy that nothing today requires.
2. **`MaxRedemptions` has a residual race.** Two concurrent checkouts can both pass the count
   check and overshoot the cap by one or two. Bounded, self-limiting, and far less severe than
   seat oversell; a reservation protocol like Inventory's would be disproportionate here. Called
   out rather than hidden.
3. **Tax rate is editable only while the event is a Draft**, like every other detail field. Tax is
   a compliance concern rather than a marketing one, so this is worth revisiting — an organizer
   who publishes with the wrong rate currently has no path to fix it.
4. **No stacking, no auto-applied "best" discount, no per-buyer targeted codes.** One code per
   order.
5. **Tax is added on top, never inclusive.** Prices shown are pre-tax. A jurisdiction requiring
   tax-inclusive display would need a genuinely different model, not a flag.

## Alternatives considered

- **Catalog computes the discounted price.** Rejected: it cannot count redemptions without
  reading Ordering's database, and splitting the arithmetic across two services to avoid that
  would put half the money model on each side of a network call.
- **Discount as a synthetic negative order line.** Attractive for reporting, and it makes
  per-line refunds fall out naturally — but it makes `SUM(lines)` no longer mean "what the
  tickets cost", and every existing consumer of `Lines` (Ticketing mints a ticket per line)
  would need to learn to skip it.
- **Tax as a per-tier rate** rather than per event. More faithful to jurisdictions that tax
  ticket categories differently, and a real requirement for some venues — but the immediate
  driver (GST) is a single rate, and per-tier rates can be added later without changing the
  order of operations, which is the part that is expensive to change.
- **Trusting the quote at checkout** instead of re-evaluating. One fewer Catalog round-trip on
  the hot path, at the cost of making an expired code chargeable by a client that simply replays
  an old quote.
