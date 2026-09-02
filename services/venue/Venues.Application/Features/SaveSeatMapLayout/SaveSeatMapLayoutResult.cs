namespace Venues.Application.Features.SaveSeatMapLayout;

/// <summary>The result of saving a draft layout.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Message">Why it was rejected, when it was.</param>
/// <param name="SeatMap">The seat map with the saved draft loaded, when it was stored.</param>
public sealed record SaveSeatMapLayoutResult(
    SaveSeatMapLayoutOutcome Outcome,
    string? Message,
    SeatMapResponse? SeatMap);
