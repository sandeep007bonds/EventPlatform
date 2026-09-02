namespace Venues.Application.Features.StartSeatMapDraft;

/// <summary>The result of opening a new draft version.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="SeatMap">The seat map with the new draft loaded, when one was opened.</param>
public sealed record StartSeatMapDraftResult(StartSeatMapDraftOutcome Outcome, SeatMapResponse? SeatMap);
