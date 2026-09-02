namespace Venues.Application.Features.UpdateVenue;

/// <summary>
/// Command to update a venue's descriptive detail. Allowed at any status: correcting an address
/// changes nothing anybody bought.
/// </summary>
/// <param name="VenueId">The venue to update.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">Venue name.</param>
/// <param name="VenueType">What kind of place this is, if stated.</param>
/// <param name="Address">Postal address and optional coordinates.</param>
/// <param name="TimeZoneId">IANA time-zone id for the venue, if known.</param>
public sealed record UpdateVenueCommand(
    Guid VenueId,
    Guid TenantId,
    string Name,
    string? VenueType,
    VenueAddressInput Address,
    string? TimeZoneId) : IRequest<VenueResponse?>;
