namespace Ordering.Api.Endpoints;

/// <summary>Paginated read model for a page of orders.</summary>
/// <param name="Orders">The orders on this page.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="TotalCount">Total number of orders matching the filter, across all pages.</param>
public sealed record OrderListResponse(
    IReadOnlyList<OrderSummaryResponse> Orders,
    int Page,
    int PageSize,
    int TotalCount);
