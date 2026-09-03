namespace Catalog.Infrastructure;

/// <summary>
/// The Venue service's <c>GET /v1/seat-maps/{id}</c> response, as far as Catalog reads it.
/// </summary>
/// <remarks>
/// A hand-written mirror of the other service's DTO rather than a shared type, and deliberately:
/// <c>building-blocks/contracts</c> is for integration <i>events</i>, and sharing a read model
/// would make every Venue response shape a compile-time dependency of Catalog. This binds the few
/// fields Catalog needs and ignores the rest, which is what lets Venue add fields freely.
/// </remarks>
/// <param name="Id">Seat-map id.</param>
/// <param name="VenueId">The venue the map configures.</param>
/// <param name="TenantId">The tenant that owns the venue.</param>
/// <param name="Version">The requested version.</param>
internal sealed record VenueSeatMapVersion(Guid Id, Guid VenueId, Guid TenantId, VenueSeatMapVersionDetail Version);
