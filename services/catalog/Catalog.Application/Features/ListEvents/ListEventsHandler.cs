namespace Catalog.Application.Features.ListEvents;

/// <summary>Handles <see cref="ListEventsQuery"/>, mapping a page of events to read models.</summary>
/// <param name="repository">The event repository.</param>
/// <param name="eventGroupRepository">The event-group repository, used to resolve contact/social fallbacks.</param>
internal sealed class ListEventsHandler(IEventRepository repository, IEventGroupRepository eventGroupRepository)
    : IRequestHandler<ListEventsQuery, ListEventsResponse>
{
    /// <inheritdoc />
    public async Task<ListEventsResponse> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = request.Mine
            ? await repository.ListForTenantAsync(
                request.CallerTenantId!.Value,
                request.Status,
                request.EventGroupId,
                request.Page,
                request.PageSize,
                cancellationToken)
            : await repository.ListAsync(
                request.CallerTenantId,
                request.Status,
                request.EventGroupId,
                request.Page,
                request.PageSize,
                cancellationToken);

        // Batch-fetch the distinct owning groups for this page in one round trip, rather than one
        // query per event, to resolve each event's contact/social fallback.
        var groupIds = items.Where(e => e.EventGroupId is not null).Select(e => e.EventGroupId!.Value).Distinct().ToList();
        var groups = groupIds.Count > 0
            ? (await eventGroupRepository.GetByIdsAsync(groupIds, cancellationToken)).ToDictionary(g => g.Id)
            : new Dictionary<Guid, EventGroup>();

        var events = items
            .Select(e => EventResponseMapper.Map(e, e.EventGroupId is not null && groups.TryGetValue(e.EventGroupId.Value, out var group) ? group : null))
            .ToList();

        return new ListEventsResponse(events, request.Page, request.PageSize, totalCount);
    }
}
