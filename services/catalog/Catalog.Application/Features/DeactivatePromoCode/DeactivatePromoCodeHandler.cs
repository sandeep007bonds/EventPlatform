namespace Catalog.Application.Features.DeactivatePromoCode;

/// <summary>Handles <see cref="DeactivatePromoCodeCommand"/> by retiring the code.</summary>
/// <param name="repository">The promo-code repository.</param>
internal sealed class DeactivatePromoCodeHandler(IPromoCodeRepository repository)
    : IRequestHandler<DeactivatePromoCodeCommand, DeactivatePromoCodeOutcome>
{
    /// <inheritdoc />
    public async Task<DeactivatePromoCodeOutcome> Handle(
        DeactivatePromoCodeCommand request,
        CancellationToken cancellationToken)
    {
        var promoCode = await repository.GetByIdAsync(request.PromoCodeId, cancellationToken);
        if (promoCode is null || promoCode.TenantId != request.TenantId)
        {
            return DeactivatePromoCodeOutcome.NotFound;
        }

        promoCode.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);

        return DeactivatePromoCodeOutcome.Deactivated;
    }
}
