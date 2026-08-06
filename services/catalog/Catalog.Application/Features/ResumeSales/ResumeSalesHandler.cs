namespace Catalog.Application.Features.ResumeSales;

/// <summary>
/// Handles <see cref="ResumeSalesCommand"/> by resuming a published event's paused sales and
/// enqueuing an <see cref="EventSalesResumed"/> integration event in the same unit of work.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class ResumeSalesHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<ResumeSalesCommand, ResumeSalesOutcome>
{
    /// <inheritdoc />
    public async Task<ResumeSalesOutcome> Handle(ResumeSalesCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return ResumeSalesOutcome.NotFound;
        }

        if (@event.Status != EventStatus.Published)
        {
            return ResumeSalesOutcome.NotPublished;
        }

        if (!@event.SalesPaused)
        {
            return ResumeSalesOutcome.NotPaused;
        }

        @event.ResumeSales();

        events.Enqueue(new EventSalesResumed(Guid.CreateVersion7(), DateTimeOffset.UtcNow, @event.TenantId, @event.Id));

        await repository.SaveChangesAsync(cancellationToken);
        return ResumeSalesOutcome.Resumed;
    }
}
