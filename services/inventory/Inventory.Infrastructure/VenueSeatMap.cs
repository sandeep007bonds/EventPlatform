namespace Inventory.Infrastructure;

/// <summary>
/// The Venue service's <c>GET /v1/seat-maps/{id}</c> response, as far as Inventory reads it.
/// </summary>
/// <remarks>
/// A hand-written mirror of the other service's DTO rather than a shared type:
/// <c>building-blocks/contracts</c> is for integration <i>events</i>, and sharing a read model
/// would make every Venue response shape a compile-time dependency of Inventory. This binds the
/// fields provisioning needs and ignores the rest.
/// </remarks>
/// <param name="Version">The requested seat-map version.</param>
internal sealed record VenueSeatMap(VenueSeatMapVersion Version);
