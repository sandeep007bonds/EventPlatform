namespace Venues.Application.Features.ListVenues;

/// <summary>Query for the calling tenant's venues.</summary>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="IncludeArchived">Whether to include archived venues.</param>
public sealed record ListVenuesQuery(Guid TenantId, bool IncludeArchived)
    : IRequest<IReadOnlyList<VenueSummaryResponse>>;
