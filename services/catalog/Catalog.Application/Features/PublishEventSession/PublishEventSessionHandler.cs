namespace Catalog.Application.Features.PublishEventSession;

/// <summary>
/// Handles <see cref="PublishEventSessionCommand"/> by validating the performance against its
/// venue's seat map, publishing it, and enqueuing an <see cref="EventSessionPublished"/> in the
/// same unit of work.
/// </summary>
/// <remarks>
/// The validation is the point of this handler; the state change is one line. Inventory provisions
/// from the message this emits and cannot ask questions afterwards, so anything wrong with the
/// allocation map has to be caught here — an unallocated block would silently become capacity
/// nobody can buy.
/// </remarks>
/// <param name="repository">The event repository.</param>
/// <param name="ticketTypes">The ticket-type repository, for the prices carried on the message.</param>
/// <param name="venue">The Venue service client.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class PublishEventSessionHandler(
    IEventRepository repository,
    ITicketTypeRepository ticketTypes,
    IVenueClient venue,
    IEventPublisher events)
    : IRequestHandler<PublishEventSessionCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(
        PublishEventSessionCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return SessionCommandResult.NotFound();
        }

        var session = @event.FindSession(request.EventSessionId);
        if (session is null)
        {
            return SessionCommandResult.NotFound();
        }

        if (@event.Status != EventStatus.Published)
        {
            return SessionCommandResult.Refused(
                "Publish the event first — a performance cannot go on sale for an event that has not.");
        }

        var readiness = await SessionPublishCheck.RunAsync(session, ticketTypes, venue, cancellationToken);
        if (readiness.Problem is not null)
        {
            return SessionCommandResult.Refused(readiness.Problem);
        }

        try
        {
            session.Publish();
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }

        events.Enqueue(readiness.ToIntegrationEvent(@event, session));

        await repository.SaveChangesAsync(cancellationToken);

        return SessionCommandResult.Ok(session.ToResponse());
    }
}
