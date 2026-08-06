namespace Catalog.Application.Features.UpdateSeatMapSection;

/// <summary>Outcome of an <see cref="UpdateSeatMapSectionCommand"/>, with the seat-map id when relevant.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="SeatMapId">The seat-map id when the section was replaced; otherwise <see langword="null"/>.</param>
public sealed record UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome Outcome, Guid? SeatMapId);
