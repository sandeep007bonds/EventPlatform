namespace Ticketing.Application.Abstractions;

/// <summary>
/// Resolves every general-admission allocation's Venue admission-area id for a performance, from
/// the Inventory service. Called once per performance by
/// <c>SessionScanContextProvisioningService</c> — a ticket only ever carries Inventory's own
/// allocation id, not the admission-area id the gate restriction is keyed on. Implemented in the
/// Infrastructure layer via Dapr service invocation.
/// </summary>
public interface IInventoryGaClient
{
    /// <summary>Gets every general-admission allocation's admission-area id for a performance.</summary>
    /// <param name="eventSessionId">The performance id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Venue admission-area id, keyed by Inventory's own allocation id.</returns>
    Task<IReadOnlyDictionary<Guid, Guid>> GetAllocationAdmissionAreasAsync(
        Guid eventSessionId,
        CancellationToken cancellationToken);
}
