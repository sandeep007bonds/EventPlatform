namespace Catalog.Application.Features.ListEventGroups;

/// <summary>Handles <see cref="ListEventGroupsQuery"/>, mapping a page of event groups to read models.</summary>
/// <param name="repository">The event group repository.</param>
internal sealed class ListEventGroupsHandler(IEventGroupRepository repository)
    : IRequestHandler<ListEventGroupsQuery, ListEventGroupsResponse>
{
    /// <inheritdoc />
    public async Task<ListEventGroupsResponse> Handle(ListEventGroupsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.ListForTenantAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var groups = items.Select(g => new EventGroupResponse(g.Id, g.Title)).ToList();

        return new ListEventGroupsResponse(groups, request.Page, request.PageSize, totalCount);
    }
}
