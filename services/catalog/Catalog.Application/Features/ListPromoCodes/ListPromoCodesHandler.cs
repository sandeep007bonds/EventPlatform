namespace Catalog.Application.Features.ListPromoCodes;

/// <summary>
/// Handles <see cref="ListPromoCodesQuery"/>. Returns <see langword="null"/> when the event does
/// not belong to the caller's tenant, which the endpoint maps to an opaque 404 — the same
/// never-reveal-another-tenant's-data pattern as <c>DefineSeatMap</c>.
/// </summary>
/// <param name="eventRepository">The event repository, to check tenant ownership.</param>
/// <param name="promoCodeRepository">The promo-code repository.</param>
internal sealed class ListPromoCodesHandler(
    IEventRepository eventRepository,
    IPromoCodeRepository promoCodeRepository)
    : IRequestHandler<ListPromoCodesQuery, IReadOnlyList<PromoCodeResponse>?>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PromoCodeResponse>?> Handle(
        ListPromoCodesQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return null;
        }

        var codes = await promoCodeRepository.ListForEventAsync(request.EventId, cancellationToken);

        return codes.Select(p => new PromoCodeResponse(
            p.Id,
            p.EventId,
            p.Code,
            p.Description,
            p.DiscountType,
            p.DiscountValue,
            p.ValidFrom,
            p.ValidTo,
            p.IsPublic,
            p.MaxRedemptions,
            p.MaxRedemptionsPerBuyer,
            p.IsActive,
            p.CreatedAt,
            p.Tiers.Select(t => t.PriceTier).ToList())).ToList();
    }
}
