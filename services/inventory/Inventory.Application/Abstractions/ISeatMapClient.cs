namespace Inventory.Application.Abstractions;

/// <summary>
/// Reads an event's seat map from the Catalog service (the cross-service hand-off). Implemented in
/// the Infrastructure layer via Dapr service invocation.
/// </summary>
public interface ISeatMapClient
{
    /// <summary>Gets the seats for an event from Catalog.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The seats to provision inventory for.</returns>
    Task<IReadOnlyList<SeatSnapshot>> GetSeatsAsync(Guid eventId, CancellationToken cancellationToken);
}
