namespace Ticketing.Application.Abstractions;

/// <summary>
/// Reads an event's check-in window and per-section entry-gate assignments from the Catalog
/// service, live at scan time (a low-frequency, latency-tolerant admin action — not the hot hold
/// path, so a synchronous cross-service read is the right trade-off here). Implemented in the
/// Infrastructure layer via Dapr service invocation.
/// </summary>
public interface ICatalogEventClient
{
    /// <summary>Gets the event's check-in window and section-to-gate mapping from Catalog.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The scan context.</returns>
    Task<CatalogScanContext> GetScanContextAsync(Guid eventId, CancellationToken cancellationToken);
}
