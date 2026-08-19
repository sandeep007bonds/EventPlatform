namespace Ordering.Application.Abstractions;

/// <summary>
/// A promo code's full rule set, as read from Catalog. Ordering decides whether it is usable —
/// Catalog returns the facts, not a verdict, because only Ordering can count redemptions and only
/// Ordering knows which lines the buyer is actually holding.
/// </summary>
/// <param name="Id">The code's Catalog id; stamped on the order and used to count redemptions.</param>
/// <param name="Code">The code, upper-invariant.</param>
/// <param name="DiscountType">Percentage or fixed amount, as the string Catalog serialises.</param>
/// <param name="DiscountValue">Percentage in (0, 100], or a flat amount in major currency units.</param>
/// <param name="ValidFrom">Earliest redeemable instant, if bounded.</param>
/// <param name="ValidTo">Latest redeemable instant, if bounded.</param>
/// <param name="IsActive">Whether the organizer has retired the code.</param>
/// <param name="MaxRedemptions">Total redemption cap, if any.</param>
/// <param name="MaxRedemptionsPerBuyer">Per-buyer redemption cap, if any.</param>
/// <param name="PriceTiers">Tiers the code is restricted to. Empty means every tier.</param>
public sealed record PromoCodeDefinition(
    Guid Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int? MaxRedemptions,
    int? MaxRedemptionsPerBuyer,
    IReadOnlyList<string> PriceTiers);
