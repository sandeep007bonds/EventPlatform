namespace Catalog.Application.Features.UpdateSellingRules;

/// <summary>
/// Handles <see cref="UpdateSellingRulesCommand"/> by setting a draft event's commercial terms and
/// enqueuing an <see cref="EventUpdated"/> integration event in the same unit of work.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class UpdateSellingRulesHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<UpdateSellingRulesCommand, UpdateSellingRulesResult>
{
    /// <inheritdoc />
    public async Task<UpdateSellingRulesResult> Handle(
        UpdateSellingRulesCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new UpdateSellingRulesResult(UpdateSellingRulesOutcome.NotFound, null);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return new UpdateSellingRulesResult(UpdateSellingRulesOutcome.NotDraft, null);
        }

        try
        {
            // The aggregate re-checks the on-sale time against every performance's booking cutoff,
            // which is the one rule that spans both levels: moving the on-sale later can close a
            // night's sales before they opened.
            @event.UpdateSellingRules(
                request.OnSaleAt,
                request.MaxTicketsPerBuyer,
                request.RequiresQueue,
                request.TaxRatePercent,
                request.TaxLabel,
                request.BookingFeePerTicketMinor);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return new UpdateSellingRulesResult(UpdateSellingRulesOutcome.Refused, exception.Message);
        }

        events.Enqueue(new EventUpdated(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id));

        await repository.SaveChangesAsync(cancellationToken);

        return new UpdateSellingRulesResult(UpdateSellingRulesOutcome.Updated, null);
    }
}
