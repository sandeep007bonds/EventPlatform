namespace Ordering.Domain;

/// <summary>
/// The subset of a promo code's rules that affects arithmetic. Ordering's own narrow view of
/// Catalog's <c>PromoCode</c> — validity bounds and redemption caps are checked elsewhere, before
/// these terms ever reach the calculator.
/// </summary>
/// <param name="DiscountType">Whether <paramref name="DiscountValue"/> is a percentage or a flat amount.</param>
/// <param name="DiscountValue">
/// A percentage in (0, 100] for <see cref="PromoDiscountType.Percentage"/>, or a flat amount in
/// **major** currency units for <see cref="PromoDiscountType.FixedAmount"/>.
/// </param>
/// <param name="PriceTiers">
/// Tiers the code applies to. **Empty means every tier** — the unrestricted case is the absence of
/// restrictions, so an organizer discounting a whole order never enumerates their tiers.
/// </param>
public sealed record PromoCodeTerms(
    PromoDiscountType DiscountType,
    decimal DiscountValue,
    IReadOnlyList<string> PriceTiers);
