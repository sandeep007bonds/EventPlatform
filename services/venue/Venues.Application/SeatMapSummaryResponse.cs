namespace Venues.Application;

/// <summary>Enough of a seat map to choose between a venue's configurations.</summary>
/// <param name="Id">Seat-map id.</param>
/// <param name="VenueId">The venue this map configures.</param>
/// <param name="Name">Configuration name.</param>
/// <param name="PublishedVersionNumber">The version currently live, if any.</param>
/// <param name="HasOpenDraft">Whether a version is being edited.</param>
/// <param name="VersionCount">How many versions exist.</param>
public sealed record SeatMapSummaryResponse(
    Guid Id,
    Guid VenueId,
    string Name,
    int? PublishedVersionNumber,
    bool HasOpenDraft,
    int VersionCount);
