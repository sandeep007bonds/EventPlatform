namespace Catalog.Application.Features.GetEvent;

/// <summary>Handles <see cref="GetEventQuery"/>, mapping the aggregate to a read model.</summary>
/// <param name="repository">The event repository.</param>
/// <param name="eventGroupRepository">The event-group repository, used to resolve contact/social fallbacks.</param>
internal sealed class GetEventHandler(IEventRepository repository, IEventGroupRepository eventGroupRepository)
    : IRequestHandler<GetEventQuery, EventResponse?>
{
    /// <inheritdoc />
    public async Task<EventResponse?> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (@event is null || !@event.IsVisibleTo(request.CallerTenantId))
        {
            return null;
        }

        var group = @event.EventGroupId is null
            ? null
            : await eventGroupRepository.GetByIdAsync(@event.EventGroupId.Value, cancellationToken);

        return EventResponseMapper.Map(@event, group);
    }
}
