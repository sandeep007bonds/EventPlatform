namespace Catalog.Application.Features.ListVenues;

/// <summary>
/// Query to list the caller's own venues — an organizer's reusable-venue picker, analogous to
/// <c>ListEvents?mine=true</c>. There is no public venue directory in this pass.
/// </summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
public sealed record ListVenuesQuery(Guid TenantId, int Page, int PageSize) : IRequest<ListVenuesResponse>;
