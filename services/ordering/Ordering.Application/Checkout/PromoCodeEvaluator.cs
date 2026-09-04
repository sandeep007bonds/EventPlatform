namespace Ordering.Application.Checkout;

/// <summary>
/// Decides whether a buyer may apply a promo code to a specific set of order lines.
/// </summary>
/// <remarks>
/// Shared by the <c>/v1/checkout/quote</c> preview and the checkout saga itself, so a buyer can
/// never be quoted a discount the charge then refuses (or vice versa). The saga re-runs it rather
/// than trusting the quote: the quote is advisory, and a code can expire or run out between the
/// two calls.
/// </remarks>
/// <param name="catalog">Reads the code's rules from Catalog.</param>
/// <param name="orders">Counts existing redemptions — Ordering owns the orders, so only it can.</param>
public sealed class PromoCodeEvaluator(ICatalogEventClient catalog, IOrderRepository orders)
{
    /// <summary>
    /// Evaluates a code against an order's lines and the buyer redeeming it.
    /// </summary>
    /// <param name="catalogEventId">The event being purchased.</param>
    /// <param name="code">The code the buyer typed.</param>
    /// <param name="userId">The buyer, for the per-buyer cap.</param>
    /// <param name="lines">The lines being purchased, for the tier-applicability check.</param>
    /// <param name="now">The instant to test validity at.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An accepted evaluation carrying the terms, or a rejection with a specific reason.</returns>
    public async Task<PromoCodeEvaluation> EvaluateAsync(
        Guid catalogEventId,
        string code,
        Guid userId,
        IReadOnlyList<OrderLineSpec> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var definition = await catalog.GetPromoCodeAsync(catalogEventId, code, cancellationToken);
        if (definition is null)
        {
            return PromoCodeEvaluation.Rejected(PromoCodeRejection.NotFound);
        }

        if (!definition.IsActive)
        {
            return PromoCodeEvaluation.Rejected(PromoCodeRejection.Inactive);
        }

        if (definition.ValidFrom is not null && now < definition.ValidFrom)
        {
            return PromoCodeEvaluation.Rejected(PromoCodeRejection.NotYetValid);
        }

        if (definition.ValidTo is not null && now > definition.ValidTo)
        {
            return PromoCodeEvaluation.Rejected(PromoCodeRejection.Expired);
        }

        var terms = new PromoCodeTerms(
            ParseDiscountType(definition.DiscountType),
            definition.DiscountValue,
            definition.TicketTypeIds);

        // Checked before the redemption counts so a code that is scoped to tiers this buyer isn't
        // even holding gets the accurate message, rather than a misleading "sold out".
        if (OrderPricingCalculator.CalculateDiscountMinor(lines, terms) <= 0)
        {
            return PromoCodeEvaluation.Rejected(PromoCodeRejection.NotApplicableToSelection);
        }

        // Two separate counts rather than one grouped query: the total cap is far more common than
        // the per-buyer one, and skipping the second query entirely when it isn't configured keeps
        // the usual path to a single count.
        if (definition.MaxRedemptions is { } maxRedemptions)
        {
            var used = await orders.CountPromoRedemptionsAsync(definition.Id, null, cancellationToken);
            if (used >= maxRedemptions)
            {
                return PromoCodeEvaluation.Rejected(PromoCodeRejection.RedemptionLimitReached);
            }
        }

        if (definition.MaxRedemptionsPerBuyer is { } maxPerBuyer)
        {
            var usedByBuyer = await orders.CountPromoRedemptionsAsync(definition.Id, userId, cancellationToken);
            if (usedByBuyer >= maxPerBuyer)
            {
                return PromoCodeEvaluation.Rejected(PromoCodeRejection.BuyerLimitReached);
            }
        }

        return PromoCodeEvaluation.Accepted(terms, definition.Id, definition.Code);
    }

    /// <summary>
    /// Maps Catalog's serialised discount type onto Ordering's own enum. An unrecognised value
    /// falls back to a fixed amount, which is the safer default: a percentage misread as an amount
    /// discounts pennies, while an amount misread as a percentage could discount the whole order.
    /// </summary>
    private static PromoDiscountType ParseDiscountType(string discountType) =>
        string.Equals(discountType, nameof(PromoDiscountType.Percentage), StringComparison.OrdinalIgnoreCase)
            ? PromoDiscountType.Percentage
            : PromoDiscountType.FixedAmount;
}
