namespace Catalog.Application.Features.ListEvents;

/// <summary>Query to list events visible to the caller, optionally filtered by status.</summary>
/// <param name="CallerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
/// <param name="Status">An optional status filter.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="Mine">
/// When <see langword="true"/>, ignores the public visibility rule and returns only
/// <see cref="CallerTenantId"/>'s own events at any status (an organizer dashboard, not public
/// browsing). The caller must guarantee <see cref="CallerTenantId"/> is non-null when this is set —
/// the endpoint layer enforces that before dispatching.
/// </param>
public sealed record ListEventsQuery(Guid? CallerTenantId, EventStatus? Status, int Page, int PageSize, bool Mine = false)
    : IRequest<ListEventsResponse>;
