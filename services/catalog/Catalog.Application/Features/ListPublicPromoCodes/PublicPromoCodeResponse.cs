namespace Catalog.Application.Features.ListPublicPromoCodes;

/// <summary>
/// Read model for a promo code as a *buyer* sees it at checkout. Deliberately narrower than the
/// organizer's <c>PromoCodeResponse</c>: redemption caps and the active flag are operational
/// details a buyer has no use for, and publishing "only 3 left" would invite a rush on it.
/// </summary>
/// <param name="Code">The code to apply.</param>
/// <param name="Description">Short description of the offer, shown next to the code.</param>
/// <param name="DiscountType">Percentage or fixed amount.</param>
/// <param name="DiscountValue">Percentage in (0, 100], or a flat amount in major currency units.</param>
/// <param name="TicketTypeIds">Ticket types the code applies to. Empty means every type.</param>
public sealed record PublicPromoCodeResponse(
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    IReadOnlyList<Guid> TicketTypeIds);
