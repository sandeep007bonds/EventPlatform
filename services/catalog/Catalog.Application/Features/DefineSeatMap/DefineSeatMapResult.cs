namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>Outcome of a <see cref="DefineSeatMapCommand"/>, with the seat-map id when relevant.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="SeatMapId">The seat-map id when created or already defined; otherwise <see langword="null"/>.</param>
public sealed record DefineSeatMapResult(DefineSeatMapOutcome Outcome, Guid? SeatMapId);
