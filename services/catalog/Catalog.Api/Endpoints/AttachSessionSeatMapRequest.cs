namespace Catalog.Api.Endpoints;

/// <summary>Request body for pointing a performance at a Venue seat-map version.</summary>
/// <param name="SeatMapId">The Venue seat map.</param>
/// <param name="VersionNumber">
/// The version to pin, or <see langword="null"/> to pin whichever is published right now. Pinned
/// either way — resolving "the published one" at sale time would let a later reconfiguration move
/// the seats a sold ticket names.
/// </param>
public sealed record AttachSessionSeatMapRequest(Guid SeatMapId, int? VersionNumber = null);
