namespace Catalog.Application.Features.DeactivatePromoCode;

/// <summary>
/// Command to retire a promo code. There is no edit — a code that has been advertised should not
/// silently change what it is worth, so the only lifecycle change is deactivation.
/// </summary>
/// <param name="EventId">
/// The event the code belongs to. Checked, not decorative: without it a caller could retire a code
/// through any event's URL, so the path would claim one thing and the effect would be another.
/// </param>
/// <param name="PromoCodeId">The code to retire.</param>
/// <param name="TenantId">The caller's tenant id; must own the code.</param>
public sealed record DeactivatePromoCodeCommand(Guid EventId, Guid PromoCodeId, Guid TenantId)
    : IRequest<DeactivatePromoCodeOutcome>;
