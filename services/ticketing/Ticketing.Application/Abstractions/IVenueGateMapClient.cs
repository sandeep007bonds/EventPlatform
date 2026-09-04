namespace Ticketing.Application.Abstractions;

/// <summary>
/// Reads a performance's gate assignments from its pinned Venue seat-map version. Called exactly
/// once per performance, by <c>SessionScanContextProvisioningService</c> — never at scan time.
/// Implemented in the Infrastructure layer via Dapr service invocation.
/// </summary>
/// <remarks>
/// The version is requested by number rather than "whichever is published": a scanner has to agree
/// with the ticket in their hand, and the ticket was sold against a pinned version (ADR-0039).
/// </remarks>
public interface IVenueGateMapClient
{
    /// <summary>Gets the gate assignments for one seat-map version.</summary>
    /// <param name="seatMapId">The Venue seat-map id.</param>
    /// <param name="versionNumber">The pinned version number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The gate map.</returns>
    Task<VenueGateMap> GetGateMapAsync(Guid seatMapId, int versionNumber, CancellationToken cancellationToken);
}
