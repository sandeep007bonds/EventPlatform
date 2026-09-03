namespace Catalog.Application.Features.PublishEvent;

/// <summary>
/// Handles <see cref="PublishEventCommand"/> by validating every performance against its venue's
/// seat map, publishing the event and each ready performance, and enqueuing the integration events
/// in the same unit of work.
/// </summary>
/// <remarks>
/// Publishing is all-or-nothing across performances: if any one of them is not sellable the whole
/// publish is refused, listing every problem. A partial publish would take an event live with one
/// of its advertised nights silently unbuyable, which is worse than not publishing at all.
/// </remarks>
/// <param name="repository">The event repository.</param>
/// <param name="ticketTypes">The ticket-type repository, for the prices carried on each message.</param>
/// <param name="venue">The Venue service client.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class PublishEventHandler(
    IEventRepository repository,
    ITicketTypeRepository ticketTypes,
    IVenueClient venue,
    IEventPublisher events)
    : IRequestHandler<PublishEventCommand, PublishEventResult>
{
    /// <inheritdoc />
    public async Task<PublishEventResult> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new PublishEventResult(PublishEventOutcome.NotFound, []);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return new PublishEventResult(PublishEventOutcome.NotDraft, []);
        }

        var problems = new List<string>();
        var readyBySession = new Dictionary<Guid, SessionPublishReadiness>();

        foreach (var session in @event.Sessions.Where(s => s.Status == EventSessionStatus.Draft))
        {
            var readiness = await SessionPublishCheck.RunAsync(session, ticketTypes, venue, cancellationToken);

            if (readiness.Problem is null)
            {
                readyBySession[session.Id] = readiness;
            }
            else
            {
                problems.Add(readiness.Problem);
            }
        }

        if (problems.Count > 0 || readyBySession.Count == 0)
        {
            return new PublishEventResult(PublishEventOutcome.NoSellablePerformance, problems);
        }

        var published = @event.Publish();

        events.Enqueue(new EventPublished(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id,
            @event.Title,
            @event.RequiresQueue,
            @event.OnSaleAt));

        foreach (var session in published)
        {
            events.Enqueue(readyBySession[session.Id].ToIntegrationEvent(@event, session));
        }

        await repository.SaveChangesAsync(cancellationToken);

        return new PublishEventResult(PublishEventOutcome.Published, []);
    }
}
