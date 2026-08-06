namespace Ticketing.Application.Abstractions;

/// <summary>
/// Reads an event's seat-map entry-gate assignments from the Catalog service. Called exactly
/// once per event, by <c>EventScanContextProvisioningService</c> — never at scan time. Implemented
/// in the Infrastructure layer via Dapr service invocation.
/// </summary>
public interface ICatalogEventClient
{
    /// <summary>Gets the event's seat-map entry-gate assignments from Catalog.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The gate map.</returns>
    Task<CatalogGateMap> GetGateMapAsync(Guid eventId, CancellationToken cancellationToken);
}
