namespace Catalog.Application.Features.ListEvents;

/// <summary>Paginated read model for a page of events.</summary>
/// <param name="Events">The events on this page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="TotalCount">Total number of events matching the filter, across all pages.</param>
public sealed record ListEventsResponse(
    IReadOnlyList<EventResponse> Events,
    int Page,
    int PageSize,
    int TotalCount);
