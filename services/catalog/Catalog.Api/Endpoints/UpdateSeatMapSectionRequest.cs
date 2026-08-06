namespace Catalog.Api.Endpoints;

/// <summary>Request body for replacing one existing section of a draft event's seat map.</summary>
/// <param name="CurrentSectionName">The existing section name to replace.</param>
/// <param name="Section">The new section definition.</param>
public sealed record UpdateSeatMapSectionRequest(
    string CurrentSectionName,
    DefineSeatMapSectionRequest Section);
