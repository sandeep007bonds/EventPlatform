namespace Venues.Application.Mapping;

/// <summary>Turns <see cref="Venue"/> aggregates into the shapes the API returns.</summary>
/// <remarks>
/// One place rather than one per handler: <c>GetVenue</c>, <c>CreateVenue</c> and every mutation
/// that echoes the venue back all owe the caller the same shape, and three copies of it would drift
/// the first time a field was added.
/// </remarks>
public static class VenueMapping
{
    /// <summary>Projects a venue in full.</summary>
    /// <param name="venue">The venue.</param>
    /// <returns>The API representation.</returns>
    public static VenueResponse ToResponse(this Venue venue)
    {
        ArgumentNullException.ThrowIfNull(venue);

        return new VenueResponse(
            venue.Id,
            venue.TenantId,
            venue.Name,
            venue.VenueType,
            venue.Status.ToString(),
            venue.TimeZoneId,
            new VenueAddressResponse(
                venue.Address.AddressLine1,
                venue.Address.AddressLine2,
                venue.Address.City,
                venue.Address.Region,
                venue.Address.PostalCode,
                venue.Address.Country,
                venue.Address.Latitude,
                venue.Address.Longitude),
            venue.Gates
                .OrderBy(g => g.Code, StringComparer.OrdinalIgnoreCase)
                .Select(g => new VenueGateResponse(g.Id, g.Code, g.Name, g.IsActive))
                .ToList(),
            venue.Facilities
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(f => new VenueFacilityResponse(f.Id, f.Name, f.Description))
                .ToList());
    }

    /// <summary>Projects a venue as a list entry.</summary>
    /// <param name="venue">The venue.</param>
    /// <returns>The summary representation.</returns>
    public static VenueSummaryResponse ToSummary(this Venue venue)
    {
        ArgumentNullException.ThrowIfNull(venue);

        return new VenueSummaryResponse(
            venue.Id,
            venue.Name,
            venue.VenueType,
            venue.Address.City,
            venue.Address.Country,
            venue.Status.ToString(),
            venue.Gates.Count);
    }
}
