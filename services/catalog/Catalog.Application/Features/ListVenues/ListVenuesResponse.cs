namespace Catalog.Application.Features.ListVenues;

/// <summary>Paginated read model for a page of venues.</summary>
/// <param name="Venues">The venues on this page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="TotalCount">Total number of venues matching the filter, across all pages.</param>
public sealed record ListVenuesResponse(
    IReadOnlyList<VenueResponse> Venues,
    int Page,
    int PageSize,
    int TotalCount);
