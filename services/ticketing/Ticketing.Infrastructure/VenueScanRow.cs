namespace Ticketing.Infrastructure;

/// <summary>A row of seats, as far as Ticketing's gate map reads it.</summary>
/// <param name="Seats">The row's seats.</param>
internal sealed record VenueScanRow(IReadOnlyList<VenueScanSeat> Seats);
