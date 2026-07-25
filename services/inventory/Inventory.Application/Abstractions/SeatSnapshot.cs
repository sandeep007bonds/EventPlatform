namespace Inventory.Application.Abstractions;

/// <summary>A seat as read from the Catalog seat map, used to provision inventory.</summary>
/// <param name="SeatId">The Catalog seat id (stable across services).</param>
/// <param name="PriceTier">Price tier name.</param>
/// <param name="PriceAmount">Seat price in the event's currency.</param>
public sealed record SeatSnapshot(Guid SeatId, string PriceTier, decimal PriceAmount);
