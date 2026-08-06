namespace Catalog.Api.Endpoints;

/// <summary>Request body for adding more sections to a draft event's existing seat map.</summary>
/// <param name="Sections">The sections to add.</param>
public sealed record AddSeatMapSectionsRequest(IReadOnlyList<DefineSeatMapSectionRequest> Sections);
