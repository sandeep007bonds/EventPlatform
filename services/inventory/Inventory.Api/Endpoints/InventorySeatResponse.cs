namespace Inventory.Api.Endpoints;

/// <summary>A single seat's current availability status.</summary>
/// <param name="SeatId">The Catalog seat id this inventory item maps to.</param>
/// <param name="Status">The current status name (<c>Available</c>, <c>Held</c>, <c>Sold</c>, <c>Blocked</c>).</param>
public sealed record InventorySeatResponse(Guid SeatId, string Status);
