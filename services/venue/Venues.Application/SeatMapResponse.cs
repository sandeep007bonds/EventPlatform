namespace Venues.Application;

/// <summary>A seat map with one of its versions loaded.</summary>
/// <param name="Id">Seat-map id.</param>
/// <param name="VenueId">The venue this map configures.</param>
/// <param name="TenantId">Owning tenant (organizer).</param>
/// <param name="Name">Configuration name.</param>
/// <param name="PublishedVersionNumber">The version currently live, if any.</param>
/// <param name="Version">The requested version, in full.</param>
public sealed record SeatMapResponse(
    Guid Id,
    Guid VenueId,
    Guid TenantId,
    string Name,
    int? PublishedVersionNumber,
    SeatMapVersionResponse Version);
