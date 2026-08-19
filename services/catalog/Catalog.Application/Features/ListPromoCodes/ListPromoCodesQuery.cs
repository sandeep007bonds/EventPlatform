namespace Catalog.Application.Features.ListPromoCodes;

/// <summary>
/// Query to list every promo code for an event, active or not — the organizer's own view.
/// <see cref="TenantId"/> comes from the validated JWT and must own the event.
/// </summary>
/// <param name="EventId">The event id.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
public sealed record ListPromoCodesQuery(Guid EventId, Guid TenantId) : IRequest<IReadOnlyList<PromoCodeResponse>?>;
