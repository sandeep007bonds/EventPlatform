namespace Catalog.Application.Features.PauseSales;

/// <summary>
/// Handles <see cref="PauseSalesCommand"/> by pausing a published event's sales and enqueuing an
/// <see cref="EventSalesPaused"/> integration event in the same unit of work.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class PauseSalesHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<PauseSalesCommand, PauseSalesOutcome>
{
    /// <inheritdoc />
    public async Task<PauseSalesOutcome> Handle(PauseSalesCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return PauseSalesOutcome.NotFound;
        }

        if (@event.Status != EventStatus.Published)
        {
            return PauseSalesOutcome.NotPublished;
        }

        if (@event.SalesPaused)
        {
            return PauseSalesOutcome.AlreadyPaused;
        }

        @event.PauseSales();

        events.Enqueue(new EventSalesPaused(Guid.CreateVersion7(), DateTimeOffset.UtcNow, @event.TenantId, @event.Id));

        await repository.SaveChangesAsync(cancellationToken);
        return PauseSalesOutcome.Paused;
    }
}
