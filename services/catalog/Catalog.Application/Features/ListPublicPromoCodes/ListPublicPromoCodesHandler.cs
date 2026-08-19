namespace Catalog.Application.Features.ListPublicPromoCodes;

/// <summary>
/// Handles <see cref="ListPublicPromoCodesQuery"/>, filtering to codes that are public *and*
/// redeemable at this instant — an expired or not-yet-started code shown in the picker would only
/// produce a rejection when applied.
/// </summary>
/// <param name="repository">The promo-code repository.</param>
internal sealed class ListPublicPromoCodesHandler(IPromoCodeRepository repository)
    : IRequestHandler<ListPublicPromoCodesQuery, IReadOnlyList<PublicPromoCodeResponse>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicPromoCodeResponse>> Handle(
        ListPublicPromoCodesQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var codes = await repository.ListForEventAsync(request.EventId, cancellationToken);

        return codes
            .Where(p => p.IsPublic && p.IsRedeemableAt(now))
            .Select(p => new PublicPromoCodeResponse(
                p.Code,
                p.Description,
                p.DiscountType,
                p.DiscountValue,
                p.Tiers.Select(t => t.PriceTier).ToList()))
            .ToList();
    }
}
