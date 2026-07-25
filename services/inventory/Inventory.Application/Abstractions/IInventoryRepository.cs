namespace Inventory.Application.Abstractions;

/// <summary>
/// Persistence abstraction for <see cref="InventoryItem"/>. Implemented in the Infrastructure
/// layer so the Application layer stays free of EF Core.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>Registers new inventory items to be persisted.</summary>
    /// <param name="items">The items to add.</param>
    void AddRange(IEnumerable<InventoryItem> items);

    /// <summary>Returns whether any inventory already exists for the event (provisioning dedupe).</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the event already has inventory.</returns>
    Task<bool> ExistsForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Counts the inventory items for an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of inventory items.</returns>
    Task<int> CountForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
