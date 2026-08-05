namespace Ticketing.Infrastructure;

/// <summary>
/// Resolves every general-admission allocation's Catalog section id via Dapr service invocation
/// (app-id <c>inventory</c>), reusing Inventory's existing anonymous general-admission-allocations
/// endpoint — no new Inventory code needed. Called once per event by
/// <c>EventScanContextProvisioningService</c>.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprInventoryGaClient(DaprClient daprClient) : IInventoryGaClient
{
    private const string InventoryAppId = "inventory";

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid>> GetAllocationCatalogSectionsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var allocations = await daprClient.InvokeMethodAsync<IReadOnlyList<InventoryGaAllocationDto>>(
            HttpMethod.Get,
            InventoryAppId,
            $"v1/events/{eventId}/inventory/general-admission",
            cancellationToken);

        return allocations.ToDictionary(a => a.AllocationId, a => a.CatalogSectionId);
    }
}
