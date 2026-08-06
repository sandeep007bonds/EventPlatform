namespace Ticketing.Application.Abstractions;

/// <summary>
/// Resolves every general-admission allocation's Catalog section id for an event, from the
/// Inventory service. Called once per event by <c>EventScanContextProvisioningService</c> — a
/// ticket only ever carries Inventory's own allocation id, not Catalog's section id the gate
/// restriction is keyed on. Implemented in the Infrastructure layer via Dapr service invocation.
/// </summary>
public interface IInventoryGaClient
{
    /// <summary>Gets every general-admission allocation's Catalog section id for an event.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Catalog section id, keyed by Inventory's own allocation id.</returns>
    Task<IReadOnlyDictionary<Guid, Guid>> GetAllocationCatalogSectionsAsync(Guid eventId, CancellationToken cancellationToken);
}
