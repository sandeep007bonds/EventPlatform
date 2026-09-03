namespace Catalog.Application.Features.ChangeEventSales;

/// <summary>
/// Handles <see cref="ChangeEventSalesCommand"/> by switching every performance and enqueuing one
/// integration event per performance in the same unit of work.
/// </summary>
/// <remarks>
/// One message per performance rather than one for the event, because Inventory is keyed by
/// performance and has no way to expand "the event" into the nights it consists of. The aggregate
/// sets the flag on every session without the published-state guard, so a run with a draft
/// late-show in it pauses cleanly instead of throwing halfway through.
/// </remarks>
/// <param name="repository">The event repository.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class ChangeEventSalesHandler(IEventRepository repository, IEventPublisher events)
    : IRequestHandler<ChangeEventSalesCommand, ChangeEventSalesOutcome>
{
    /// <inheritdoc />
    public async Task<ChangeEventSalesOutcome> Handle(
        ChangeEventSalesCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return ChangeEventSalesOutcome.NotFound;
        }

        if (@event.Status != EventStatus.Published)
        {
            return ChangeEventSalesOutcome.NotPublished;
        }

        if (request.Pause)
        {
            @event.PauseSales();
        }
        else
        {
            @event.ResumeSales();
        }

        // Only performances that are actually on sale are announced. A draft one has no inventory
        // provisioned, so a consumer reacting to it would be pausing something that does not exist.
        foreach (var session in @event.Sessions.Where(s => s.Status == EventSessionStatus.Published))
        {
            events.Enqueue(SessionSalesEvent.For(request.Pause, @event, session));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return ChangeEventSalesOutcome.Changed;
    }
}
