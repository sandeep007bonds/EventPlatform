namespace Inventory.Application.Abstractions;

/// <summary>
/// Reads an event's seat map from the Catalog service (the cross-service hand-off). Implemented in
/// the Infrastructure layer via Dapr service invocation.
/// </summary>
public interface ISeatMapClient
{
    /// <summary>Gets the full seat map for an event from Catalog — both reserved seats and general-admission sections.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The seat map to provision inventory from.</returns>
    Task<SeatMapSnapshot> GetSeatMapAsync(Guid eventId, CancellationToken cancellationToken);
}
