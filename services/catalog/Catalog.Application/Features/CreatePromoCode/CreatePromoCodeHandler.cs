namespace Catalog.Application.Features.CreatePromoCode;

/// <summary>Handles <see cref="CreatePromoCodeCommand"/> by creating and persisting a promo code.</summary>
/// <param name="eventRepository">The event repository, to check tenant ownership.</param>
/// <param name="promoCodeRepository">The promo-code repository.</param>
internal sealed class CreatePromoCodeHandler(
    IEventRepository eventRepository,
    IPromoCodeRepository promoCodeRepository)
    : IRequestHandler<CreatePromoCodeCommand, CreatePromoCodeResult>
{
    /// <inheritdoc />
    public async Task<CreatePromoCodeResult> Handle(CreatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            // Opaque not-found on a tenant mismatch — never reveal that another tenant's event
            // exists. Same pattern as PublishEvent.
            return new CreatePromoCodeResult(CreatePromoCodeOutcome.EventNotFound, null);
        }

        // Pre-check for a friendly 409. The unique index over the event-and-code pair remains the
        // real guarantee — this only turns the common case into a clear message rather than a raw
        // constraint violation.
        var existing = await promoCodeRepository.GetByCodeAsync(request.EventId, request.Code, cancellationToken);
        if (existing is not null)
        {
            return new CreatePromoCodeResult(CreatePromoCodeOutcome.DuplicateCode, null);
        }

        var promoCode = PromoCode.Create(
            request.EventId,
            request.TenantId,
            request.Code,
            request.Description,
            request.DiscountType,
            request.DiscountValue,
            request.ValidFrom,
            request.ValidTo,
            request.IsPublic,
            request.MaxRedemptions,
            request.MaxRedemptionsPerBuyer,
            request.TicketTypeIds);

        promoCodeRepository.Add(promoCode);
        await promoCodeRepository.SaveChangesAsync(cancellationToken);

        return new CreatePromoCodeResult(CreatePromoCodeOutcome.Created, promoCode.Id);
    }
}
