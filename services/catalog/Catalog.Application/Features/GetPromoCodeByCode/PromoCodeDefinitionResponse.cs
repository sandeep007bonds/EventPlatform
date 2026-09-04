namespace Catalog.Application.Features.GetPromoCodeByCode;

/// <summary>
/// The full rule set for one promo code, as the Ordering service reads it at checkout.
/// </summary>
/// <remarks>
/// Deliberately returns the raw facts — validity bounds, active flag, caps — rather than a
/// pre-computed "is it usable" verdict. Ordering needs to tell the buyer *why* a code was
/// rejected (expired vs. exhausted vs. wrong tier), and it is the only service that can count
/// redemptions, so a boolean from here would be both less useful and incomplete.
/// </remarks>
/// <param name="Id">Promo-code id — Ordering stores this on the order and counts redemptions by it.</param>
/// <param name="Code">The code, upper-invariant.</param>
/// <param name="DiscountType">Percentage or fixed amount.</param>
/// <param name="DiscountValue">Percentage in (0, 100], or a flat amount in major currency units.</param>
/// <param name="ValidFrom">Earliest redeemable instant, if bounded.</param>
/// <param name="ValidTo">Latest redeemable instant, if bounded.</param>
/// <param name="IsActive">Whether the organizer has retired the code.</param>
/// <param name="MaxRedemptions">Total redemption cap, if any.</param>
/// <param name="MaxRedemptionsPerBuyer">Per-buyer redemption cap, if any.</param>
/// <param name="TicketTypeIds">Ticket types the code is restricted to. Empty means every type.</param>
public sealed record PromoCodeDefinitionResponse(
    Guid Id,
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int? MaxRedemptions,
    int? MaxRedemptionsPerBuyer,
    IReadOnlyList<Guid> TicketTypeIds);
