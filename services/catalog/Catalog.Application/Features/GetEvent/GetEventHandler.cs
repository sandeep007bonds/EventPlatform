namespace Catalog.Application.Features.GetEvent;

/// <summary>Handles <see cref="GetEventQuery"/>, mapping the aggregate to a read model.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class GetEventHandler(IEventRepository repository)
    : IRequestHandler<GetEventQuery, EventResponse?>
{
    /// <inheritdoc />
    public async Task<EventResponse?> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);

        return @event is null || !@event.IsVisibleTo(request.CallerTenantId)
            ? null
            : new EventResponse(
                @event.Id,
                @event.Title,
                @event.StartsAt,
                @event.Status.ToString(),
                @event.Currency);
    }
}
