namespace Venues.Application.Features.ListSeatMaps;

/// <summary>Query for a venue's seating configurations, without their layouts.</summary>
/// <param name="VenueId">The venue id.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
public sealed record ListSeatMapsQuery(Guid VenueId, Guid TenantId)
    : IRequest<IReadOnlyList<SeatMapSummaryResponse>>;
