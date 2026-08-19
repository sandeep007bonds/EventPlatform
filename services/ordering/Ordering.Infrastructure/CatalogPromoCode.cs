namespace Ordering.Infrastructure;

/// <summary>
/// Wire shape of Catalog's <c>GET /v1/events/{id}/promo-codes/by-code/{code}</c> response.
/// Mirrors Catalog's <c>PromoCodeDefinitionResponse</c> field for field — there is no shared
/// contract assembly between the two services, so this is a hand-kept copy, and its property
/// names must match Catalog's JSON exactly.
/// </summary>
/// <param name="Id">The code's Catalog id.</param>
/// <param name="Code">The code, upper-invariant.</param>
/// <param name="DiscountType">Serialised as a string (<c>Percentage</c> / <c>FixedAmount</c>).</param>
/// <param name="DiscountValue">Percentage in (0, 100], or a flat amount in major currency units.</param>
/// <param name="ValidFrom">Earliest redeemable instant, if bounded.</param>
/// <param name="ValidTo">Latest redeemable instant, if bounded.</param>
/// <param name="IsActive">Whether the organizer has retired the code.</param>
/// <param name="MaxRedemptions">Total redemption cap, if any.</param>
/// <param name="MaxRedemptionsPerBuyer">Per-buyer redemption cap, if any.</param>
/// <param name="PriceTiers">Tiers the code is restricted to. Empty means every tier.</param>
internal sealed record CatalogPromoCode(
    Guid Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int? MaxRedemptions,
    int? MaxRedemptionsPerBuyer,
    IReadOnlyList<string>? PriceTiers);
