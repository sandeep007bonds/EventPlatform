namespace Catalog.Application.Abstractions;

/// <summary>
/// What Catalog needs to know about a Venue seat-map version: enough to check a performance can be
/// sold with it, and nothing more.
/// </summary>
/// <remarks>
/// Deliberately shallow. Catalog never sees rows or seats — allocations bind to section and
/// admission-area <b>codes</b>, so the codes and the capacity are the whole of what it has to
/// reason about. Inventory is the service that reads the seats, and it reads them from Venue
/// directly.
/// </remarks>
/// <param name="SeatMapId">The seat map this is a version of.</param>
/// <param name="VenueId">The venue the map configures.</param>
/// <param name="TenantId">The tenant that owns the venue.</param>
/// <param name="SeatMapVersionId">This version's id.</param>
/// <param name="VersionNumber">This version's number.</param>
/// <param name="IsPublished">Whether the version is published, and therefore immutable.</param>
/// <param name="Capacity">Sellable seats plus admission-area capacity.</param>
/// <param name="BlockCodes">Every section and admission-area code in the version.</param>
/// <param name="VenueName">Venue name, for the display snapshot.</param>
/// <param name="City">City, for the display snapshot.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code, for the display snapshot.</param>
/// <param name="TimeZoneId">The venue's IANA time zone, for the display snapshot.</param>
public sealed record SeatMapVersionSnapshot(
    Guid SeatMapId,
    Guid VenueId,
    Guid TenantId,
    Guid SeatMapVersionId,
    int VersionNumber,
    bool IsPublished,
    int Capacity,
    IReadOnlySet<string> BlockCodes,
    string VenueName,
    string City,
    string Country,
    string? TimeZoneId);
