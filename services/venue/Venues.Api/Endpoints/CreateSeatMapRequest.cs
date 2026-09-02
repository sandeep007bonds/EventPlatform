namespace Venues.Api.Endpoints;

/// <summary>Request body for adding a seating configuration to a venue.</summary>
/// <param name="Name">Configuration name (e.g. <c>End stage</c>).</param>
public sealed record CreateSeatMapRequest(string Name);
