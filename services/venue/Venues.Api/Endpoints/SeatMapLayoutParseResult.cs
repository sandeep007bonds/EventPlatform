namespace Venues.Api.Endpoints;

/// <summary>The outcome of turning a submitted layout into the domain's own shape.</summary>
/// <param name="Layout">The parsed layout, when every enum name was recognised.</param>
/// <param name="Error">What was not recognised, otherwise.</param>
public sealed record SeatMapLayoutParseResult(SeatMapLayout? Layout, string? Error);
