namespace Catalog.Application.Features.ListEventGroups;

/// <summary>Paginated read model for a page of event groups.</summary>
/// <param name="EventGroups">The event groups on this page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="TotalCount">Total number of event groups matching the filter, across all pages.</param>
public sealed record ListEventGroupsResponse(
    IReadOnlyList<EventGroupResponse> EventGroups,
    int Page,
    int PageSize,
    int TotalCount);
