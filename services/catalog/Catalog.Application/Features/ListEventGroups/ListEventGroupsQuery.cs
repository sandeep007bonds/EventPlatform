namespace Catalog.Application.Features.ListEventGroups;

/// <summary>
/// Query to list the caller's own event groups (tours) — an organizer's "pick or create a tour"
/// picker, analogous to <c>ListEvents?mine=true</c>. There is no public directory in this pass.
/// </summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
public sealed record ListEventGroupsQuery(Guid TenantId, int Page, int PageSize) : IRequest<ListEventGroupsResponse>;
