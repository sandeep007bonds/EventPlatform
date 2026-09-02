namespace Venues.Application.Features.PublishSeatMap;

/// <summary>The result of publishing a seat-map draft.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="VersionNumber">The version that is now live, when one was published.</param>
/// <param name="Capacity">That version's sellable capacity.</param>
/// <param name="Errors">
/// Every reason the layout was rejected. Returned as a list because a stadium plan can fail thirty
/// ways at once and an editor that reveals them one at a time is unusable.
/// </param>
public sealed record PublishSeatMapResult(
    PublishSeatMapOutcome Outcome,
    int? VersionNumber,
    int? Capacity,
    IReadOnlyList<SeatMapValidationError> Errors);
