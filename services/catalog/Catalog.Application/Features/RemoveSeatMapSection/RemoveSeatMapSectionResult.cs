namespace Catalog.Application.Features.RemoveSeatMapSection;

/// <summary>Outcome of a <see cref="RemoveSeatMapSectionCommand"/>.</summary>
/// <param name="Outcome">What happened.</param>
public sealed record RemoveSeatMapSectionResult(RemoveSeatMapSectionOutcome Outcome);
