namespace Venues.Application.Features.CreateVenue;

/// <summary>
/// Command to create a venue. <see cref="TenantId"/> is set server-side from the validated JWT,
/// never from the request body (ADR-0011).
/// </summary>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">Venue name.</param>
/// <param name="VenueType">What kind of place this is, if stated.</param>
/// <param name="Address">Postal address and optional coordinates.</param>
/// <param name="TimeZoneId">IANA time-zone id for the venue, if known.</param>
public sealed record CreateVenueCommand(
    Guid TenantId,
    string Name,
    string? VenueType,
    VenueAddressInput Address,
    string? TimeZoneId) : IRequest<VenueResponse>;
