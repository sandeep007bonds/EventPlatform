namespace Catalog.Application.Features.UpdateVenue;

/// <summary>
/// Command to update an existing venue's details. <see cref="TenantId"/> is set server-side
/// from the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="Id">The venue id to update.</param>
/// <param name="TenantId">The caller's tenant id; must own the venue.</param>
/// <param name="Name">Venue name.</param>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
/// <param name="Capacity">Nominal venue capacity, if known.</param>
public sealed record UpdateVenueCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    int? Capacity) : IRequest<UpdateVenueOutcome>;
