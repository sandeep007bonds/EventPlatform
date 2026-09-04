namespace Inventory.Infrastructure;

/// <summary>A row of seats, as far as Inventory reads it.</summary>
/// <param name="Seats">The row's seats.</param>
internal sealed record VenueRow(IReadOnlyList<VenueSeat> Seats);
