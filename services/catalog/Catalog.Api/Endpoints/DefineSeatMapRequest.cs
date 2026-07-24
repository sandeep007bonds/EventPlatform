namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for defining an event's seat map. The tenant is taken from the caller's token,
/// never from this body (ADR-0011).
/// </summary>
/// <param name="Name">Seat-map name.</param>
/// <param name="Sections">The sections to generate seats from.</param>
public sealed record DefineSeatMapRequest(string Name, IReadOnlyList<DefineSeatMapSectionRequest> Sections);
