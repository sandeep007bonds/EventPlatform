namespace Catalog.Application.Features.GetPromoCodeByCode;

/// <summary>Handles <see cref="GetPromoCodeByCodeQuery"/>, mapping the code to its rule set.</summary>
/// <param name="repository">The promo-code repository.</param>
internal sealed class GetPromoCodeByCodeHandler(IPromoCodeRepository repository)
    : IRequestHandler<GetPromoCodeByCodeQuery, PromoCodeDefinitionResponse?>
{
    /// <inheritdoc />
    public async Task<PromoCodeDefinitionResponse?> Handle(
        GetPromoCodeByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByCodeAsync(request.EventId, request.Code, cancellationToken);

        return promoCode is null
            ? null
            : new PromoCodeDefinitionResponse(
                promoCode.Id,
                promoCode.Code,
                promoCode.DiscountType,
                promoCode.DiscountValue,
                promoCode.ValidFrom,
                promoCode.ValidTo,
                promoCode.IsActive,
                promoCode.MaxRedemptions,
                promoCode.MaxRedemptionsPerBuyer,
                promoCode.Tiers.Select(t => t.PriceTier).ToList());
    }
}
