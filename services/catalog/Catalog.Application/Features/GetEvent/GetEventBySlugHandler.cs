namespace Catalog.Application.Features.GetEvent;

/// <summary>
/// Handles <see cref="GetEventBySlugQuery"/> — the same read model as <see cref="GetEventHandler"/>,
/// keyed by the shareable URL rather than the id.
/// </summary>
/// <param name="repository">The event repository.</param>
/// <param name="eventGroupRepository">The event-group repository, used to resolve contact/social fallbacks.</param>
internal sealed class GetEventBySlugHandler(IEventRepository repository, IEventGroupRepository eventGroupRepository)
    : IRequestHandler<GetEventBySlugQuery, EventResponse?>
{
    /// <inheritdoc />
    public async Task<EventResponse?> Handle(GetEventBySlugQuery request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetBySlugAsync(request.Slug, cancellationToken);
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
