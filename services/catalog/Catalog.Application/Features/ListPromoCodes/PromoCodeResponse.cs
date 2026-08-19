namespace Catalog.Application.Features.ListPromoCodes;

/// <summary>
/// Read model for a promo code, as the owning organizer sees it — including the redemption caps
/// and inactive codes, neither of which a buyer is shown.
/// </summary>
/// <param name="Id">Promo-code id.</param>
/// <param name="EventId">The event the code discounts.</param>
/// <param name="Code">The code buyers type, upper-invariant.</param>
/// <param name="Description">Organizer-facing note.</param>
/// <param name="DiscountType">Percentage or fixed amount.</param>
/// <param name="DiscountValue">Percentage in (0, 100], or a flat amount in major currency units.</param>
/// <param name="ValidFrom">Earliest redeemable instant, if bounded.</param>
/// <param name="ValidTo">Latest redeemable instant, if bounded.</param>
/// <param name="IsPublic">Whether buyers see this code listed at checkout.</param>
/// <param name="MaxRedemptions">Total redemption cap, if any.</param>
/// <param name="MaxRedemptionsPerBuyer">Per-buyer redemption cap, if any.</param>
/// <param name="IsActive">Whether the code has been deactivated.</param>
/// <param name="CreatedAt">When the code was created.</param>
/// <param name="PriceTiers">Tiers the code is restricted to. Empty means every tier.</param>
public sealed record PromoCodeResponse(
    Guid Id,
    Guid EventId,
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsPublic,
    int? MaxRedemptions,
    int? MaxRedemptionsPerBuyer,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> PriceTiers);
