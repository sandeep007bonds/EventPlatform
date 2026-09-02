namespace Venues.Application;

/// <summary>Enough of a venue to pick one from a list.</summary>
/// <param name="Id">Venue id.</param>
/// <param name="Name">Venue name.</param>
/// <param name="VenueType">What kind of place this is, if stated.</param>
/// <param name="City">City, so two venues with the same name are still distinguishable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="GateCount">How many entry points the venue has.</param>
public sealed record VenueSummaryResponse(
    Guid Id,
    string Name,
    string? VenueType,
    string City,
    string Country,
    string Status,
    int GateCount);
