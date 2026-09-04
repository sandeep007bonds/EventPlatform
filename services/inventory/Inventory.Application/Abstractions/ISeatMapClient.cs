namespace Inventory.Application.Abstractions;

/// <summary>
/// Reads a seat-map version from the Venue service (the cross-service hand-off). Implemented in the
/// Infrastructure layer via Dapr service invocation.
/// </summary>
/// <remarks>
/// Called once per performance, at provisioning time, and never on the selling path. The version is
/// requested by number rather than "whichever is published", because the performance pinned one —
/// resolving it again later could hand back a different map than the tickets were sold against.
/// </remarks>
public interface ISeatMapClient
{
    /// <summary>Gets one seat-map version — its seats and its admission areas.</summary>
    /// <param name="seatMapId">The Venue seat-map id.</param>
    /// <param name="versionNumber">The pinned version number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The version to provision inventory from.</returns>
    Task<SeatMapSnapshot> GetSeatMapAsync(Guid seatMapId, int versionNumber, CancellationToken cancellationToken);
}
