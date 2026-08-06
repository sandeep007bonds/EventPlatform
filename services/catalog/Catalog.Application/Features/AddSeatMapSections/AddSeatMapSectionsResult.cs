namespace Catalog.Application.Features.AddSeatMapSections;

/// <summary>Outcome of an <see cref="AddSeatMapSectionsCommand"/>, with the seat-map id when relevant.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="SeatMapId">The seat-map id when sections were added; otherwise <see langword="null"/>.</param>
public sealed record AddSeatMapSectionsResult(AddSeatMapSectionsOutcome Outcome, Guid? SeatMapId);
