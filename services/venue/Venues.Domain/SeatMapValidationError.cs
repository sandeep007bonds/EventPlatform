namespace Venues.Domain;

/// <summary>One reason a seat-map version cannot be published.</summary>
/// <remarks>
/// Returned as a list rather than thrown one at a time. Publishing a stadium plan can fail for
/// thirty reasons at once, and an editor that reveals them one refresh apart is unusable — the
/// person fixing it needs the whole list to work through.
/// </remarks>
/// <param name="Code">Stable machine-readable code (e.g. <c>duplicate_seat_number</c>).</param>
/// <param name="Message">Human-readable explanation, naming the section, row or area at fault.</param>
public sealed record SeatMapValidationError(string Code, string Message);
