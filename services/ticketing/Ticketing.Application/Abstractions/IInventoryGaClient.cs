namespace Ticketing.Application.Abstractions;

/// <summary>
/// Resolves a general-admission ticket's Catalog section id from the Inventory service, live at
/// scan time — a ticket only ever carries Inventory's own allocation id, not Catalog's section id
/// the gate restriction is keyed on. Implemented in the Infrastructure layer via Dapr service
/// invocation.
/// </summary>
public interface IInventoryGaClient
{
    /// <summary>Gets the Catalog section id a general-admission allocation maps to.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="allocationId">Inventory's own allocation id (<see cref="Ticket.GeneralAdmissionAllocationId"/>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The Catalog section id, or <see langword="null"/> if the allocation doesn't exist.</returns>
    Task<Guid?> GetCatalogSectionIdAsync(Guid eventId, Guid allocationId, CancellationToken cancellationToken);
}
