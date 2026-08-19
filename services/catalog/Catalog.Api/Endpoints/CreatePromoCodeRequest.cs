namespace Catalog.Api.Endpoints;

/// <summary>Request body for creating a promo code. The tenant comes from the caller's token.</summary>
/// <param name="Code">The code buyers type. Stored upper-invariant; matching is case-insensitive.</param>
/// <param name="Description">Organizer-facing note on what the code is for.</param>
/// <param name="DiscountType">
/// <c>Percentage</c> or <c>FixedAmount</c> — decides how <paramref name="DiscountValue"/> is read.
/// </param>
/// <param name="DiscountValue">
/// A percentage in (0, 100] for <c>Percentage</c>, or a flat amount in **major** currency units
/// (e.g. 250 for ₹250) for <c>FixedAmount</c>.
/// </param>
/// <param name="ValidFrom">Earliest redeemable instant (UTC); omit for no lower bound.</param>
/// <param name="ValidTo">Latest redeemable instant (UTC); omit for no upper bound.</param>
/// <param name="IsPublic">Whether buyers see this code listed at checkout instead of typing it.</param>
/// <param name="MaxRedemptions">Total redemption cap; omit for unlimited.</param>
/// <param name="MaxRedemptionsPerBuyer">Per-buyer redemption cap; omit for unlimited.</param>
/// <param name="PriceTiers">
/// Price-tier names the code applies to, matching the seat map's section tiers. Omit or send an
/// empty list to discount every tier.
/// </param>
public sealed record CreatePromoCodeRequest(
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null,
    bool IsPublic = false,
    int? MaxRedemptions = null,
    int? MaxRedemptionsPerBuyer = null,
    IReadOnlyList<string>? PriceTiers = null);
