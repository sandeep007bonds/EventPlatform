namespace Catalog.Application.Features.ListEvents;

/// <summary>Query to list events visible to the caller, optionally filtered by status.</summary>
/// <param name="CallerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
/// <param name="Status">An optional status filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
public sealed record ListEventsQuery(Guid? CallerTenantId, EventStatus? Status, int Page, int PageSize)
    : IRequest<ListEventsResponse>;
