namespace Ticketing.Infrastructure;

/// <summary>The Venue seat-map response, as far as Ticketing's gate map reads it.</summary>
/// <param name="Version">The requested seat-map version.</param>
internal sealed record VenueScanSeatMap(VenueScanVersion Version);
