namespace Ticketing.Infrastructure;

/// <summary>An admission area, as far as Ticketing's gate map reads it.</summary>
/// <param name="Id">The Venue admission-area id.</param>
/// <param name="GateId">The gate this area is entered through, or <see langword="null"/> for any.</param>
internal sealed record VenueScanArea(Guid Id, Guid? GateId);
