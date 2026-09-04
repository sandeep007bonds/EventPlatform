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
/// Order of operations is fixed: <b>discount first, then the booking fee, then tax on both</b>.
/// Taxing the pre-discount amount would over-collect, and in most jurisdictions tax is owed on the
/// consideration actually paid. The booking fee is charged per ticket and is <b>not</b> discountable
/// — a promo code reduces what the tickets cost, not what the platform charges to sell them.
/// </para>
/// <para>
/// Tax is computed as two separate roundings — one on the discounted tickets, one on the fee —
/// rather than one rounding over their sum. That looks like a detail and is not: the booking fee is
/// non-refundable, so a cancellation has to return the ticket money and its tax while keeping the
/// fee and its tax. Only a tax split at source makes that an exact subtraction instead of one that
/// can be a minor unit out.
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
    /// <param name="bookingFeePerTicketMinor">
    /// The event's booking fee per ticket, in minor units. Zero when the event charges none.
    /// Negative values are treated as zero rather than credited.
    /// </param>
    /// <returns>The full breakdown.</returns>
    public static OrderPricing Calculate(
        IReadOnlyList<OrderLineSpec> lines,
        PromoCodeTerms? terms,
        decimal? taxRatePercent,
        long bookingFeePerTicketMinor)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var subtotalMinor = lines.Sum(line => line.PriceMinor);
        var discountMinor = terms is null ? 0L : CalculateDiscountMinor(lines, terms);

        // Cannot go negative: the discount is clamped to the eligible lines' subtotal, and the
        // eligible lines are a subset of all lines.
        var netMinor = subtotalMinor - discountMinor;

        // Per admission, not per line: a general-admission line of four is four tickets, and a fee
        // charged per line would quietly under-charge exactly the orders that are largest.
        var ticketCount = lines.Sum(line => (long)line.Quantity);
        var bookingFeeMinor = Math.Max(0L, bookingFeePerTicketMinor) * ticketCount;

        var taxOnTicketsMinor = TaxOnMinor(netMinor, taxRatePercent);
        var taxOnFeeMinor = TaxOnMinor(bookingFeeMinor, taxRatePercent);

        return new OrderPricing(
            subtotalMinor,
            discountMinor,
            bookingFeeMinor,
            taxOnTicketsMinor + taxOnFeeMinor,
            netMinor + bookingFeeMinor + taxOnTicketsMinor + taxOnFeeMinor,
            netMinor + taxOnTicketsMinor);
    }

    /// <summary>
    /// Tax on an amount at a rate, rounded half away from zero.
    /// </summary>
    /// <param name="baseMinor">The amount being taxed, in minor units.</param>
    /// <param name="taxRatePercent">The rate as a percentage, or <see langword="null"/>/zero for untaxed.</param>
    /// <returns>The tax in minor units; zero when the rate is null, zero or negative.</returns>
    public static long TaxOnMinor(long baseMinor, decimal? taxRatePercent) =>
        taxRatePercent is > 0m
            ? (long)Math.Round(baseMinor * taxRatePercent.Value / 100m, MidpointRounding.AwayFromZero)
            : 0L;

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
        terms.TicketTypeIds.Count == 0
        || terms.TicketTypeIds.Contains(line.TicketTypeId, StringComparer.OrdinalIgnoreCase);
}
