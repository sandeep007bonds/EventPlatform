namespace Ordering.Domain;

/// <summary>
/// Works out what an order costs: subtotal, promo discount, tax, and the payable total.
/// </summary>
/// <remarks>
/// Pure arithmetic — no I/O, no clock, no repository — so the checkout saga and the
/// <c>/v1/checkout/quote</c> preview can share it and cannot disagree about the number. A buyer who
/// is quoted a total and then charged a different one has been mis-sold, so "the preview and the
/// charge run the same code" is a correctness requirement, not tidiness.
/// <para>
/// Order of operations is fixed: <b>discount first, then tax on what remains</b>. Taxing the
/// pre-discount amount would over-collect, and in most jurisdictions tax is owed on the
/// consideration actually paid.
/// </para>
/// </remarks>
public static class OrderPricingCalculator
{
    /// <summary>
    /// Minor units per major unit. Assumes a 2-decimal currency (₹1 = 100 paise, $1 = 100 cents),
    /// which is wrong for JPY (0 decimals) and a handful of others — the same assumption Inventory's
    /// own price conversion already makes. Tracked as T11; fixing it means resolving the ISO 4217
    /// exponent per currency in one shared place, not diverging here.
    /// </summary>
    private const decimal MinorUnitsPerMajor = 100m;

    /// <summary>
    /// Prices an order end to end.
    /// </summary>
    /// <param name="lines">The order's lines, priced per unit and in total.</param>
    /// <param name="terms">
    /// The promo code's terms, or <see langword="null"/> when no code was applied. Validity and
    /// redemption caps are checked before this point — reaching here means the code is usable.
    /// </param>
    /// <param name="taxRatePercent">
    /// The event's tax rate as a percentage, or <see langword="null"/>/zero when untaxed.
    /// </param>
    /// <returns>The full breakdown.</returns>
    public static OrderPricing Calculate(
        IReadOnlyList<OrderLineSpec> lines,
        PromoCodeTerms? terms,
        decimal? taxRatePercent)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var subtotalMinor = lines.Sum(line => line.PriceMinor);
        var discountMinor = terms is null ? 0L : CalculateDiscountMinor(lines, terms);

        // Cannot go negative: the discount is clamped to the eligible lines' subtotal, and the
        // eligible lines are a subset of all lines.
        var taxableMinor = subtotalMinor - discountMinor;

        var taxMinor = taxRatePercent is > 0m
            ? (long)Math.Round(taxableMinor * taxRatePercent.Value / 100m, MidpointRounding.AwayFromZero)
            : 0L;

        return new OrderPricing(subtotalMinor, discountMinor, taxMinor, taxableMinor + taxMinor);
    }

    /// <summary>
    /// Works out how much a promo code takes off, considering only the lines it applies to.
    /// </summary>
    /// <param name="lines">The order's lines.</param>
    /// <param name="terms">The code's terms.</param>
    /// <returns>The discount in minor currency units; never negative, never more than the eligible lines are worth.</returns>
    public static long CalculateDiscountMinor(IReadOnlyList<OrderLineSpec> lines, PromoCodeTerms terms)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(terms);

        var eligibleSubtotalMinor = lines
            .Where(line => AppliesTo(line, terms))
            .Sum(line => line.PriceMinor);

        if (eligibleSubtotalMinor <= 0)
        {
            // A code scoped to tiers this order doesn't contain is worth nothing. Not an error —
            // the caller decides whether to reject it or simply apply no discount.
            return 0;
        }

        var rawDiscount = terms.DiscountType switch
        {
            PromoDiscountType.Percentage =>
                Math.Round(eligibleSubtotalMinor * terms.DiscountValue / 100m, MidpointRounding.AwayFromZero),
            PromoDiscountType.FixedAmount =>
                Math.Round(terms.DiscountValue * MinorUnitsPerMajor, MidpointRounding.AwayFromZero),
            _ => 0m,
        };

        // Clamped so a fixed discount larger than the order can never make the total negative —
        // "₹500 off" on a ₹300 order is a free order, not a ₹200 refund.
        return Math.Clamp((long)rawDiscount, 0L, eligibleSubtotalMinor);
    }

    /// <summary>
    /// Whether a code discounts a given line: either the code names no tiers (applies to all), or
    /// the line's tier is one it names. Matched case-insensitively, since the tier is a
    /// human-entered name on both sides.
    /// </summary>
    private static bool AppliesTo(OrderLineSpec line, PromoCodeTerms terms) =>
        terms.PriceTiers.Count == 0
        || terms.PriceTiers.Contains(line.PriceTier, StringComparer.OrdinalIgnoreCase);
}
