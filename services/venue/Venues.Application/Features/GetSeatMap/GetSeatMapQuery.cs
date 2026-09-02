namespace Venues.Application.Features.GetSeatMap;

/// <summary>Query for one seat map with a single version's full layout.</summary>
/// <param name="SeatMapId">The seat-map id.</param>
/// <param name="VersionNumber">
/// The version to load, or <see langword="null"/> for whichever is published. Asking for a specific
/// version is how a ticket sold two configurations ago still resolves to the seat it names.
/// </param>
/// <param name="TenantId">
/// The calling tenant, or <see langword="null"/> for an anonymous caller.
/// </param>
public sealed record GetSeatMapQuery(Guid SeatMapId, int? VersionNumber, Guid? TenantId)
    : IRequest<SeatMapResponse?>;
