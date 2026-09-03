namespace Catalog.Application.Features.ListEventSessions;

/// <summary>
/// Handles <see cref="ListEventSessionsQuery"/>, returning <see langword="null"/> when the event
/// does not exist or is not visible to the caller.
/// </summary>
/// <param name="repository">The event repository.</param>
internal sealed class ListEventSessionsHandler(IEventRepository repository)
    : IRequestHandler<ListEventSessionsQuery, IReadOnlyList<EventSessionResponse>?>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EventSessionResponse>?> Handle(
        ListEventSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || !@event.IsVisibleTo(request.TenantId))
        {
            return null;
        }

        // A cancelled performance is still listed. A buyer holding a ticket for it needs to see
        // that it was called off far more than they need a tidy list.
        return @event.Sessions.ToResponses();
    }
}
