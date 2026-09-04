namespace Ticketing.Infrastructure;

/// <summary>A seat, as far as Ticketing's gate map reads it — just its identity.</summary>
/// <param name="Id">The Venue seat id.</param>
internal sealed record VenueScanSeat(Guid Id);
