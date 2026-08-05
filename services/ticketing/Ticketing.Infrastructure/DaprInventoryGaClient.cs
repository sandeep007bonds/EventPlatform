namespace Ticketing.Infrastructure;

/// <summary>
/// Resolves a general-admission allocation's Catalog section id via Dapr service invocation
/// (app-id <c>inventory</c>), reusing Inventory's existing anonymous general-admission-allocations
/// endpoint — no new Inventory code needed.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprInventoryGaClient(DaprClient daprClient) : IInventoryGaClient
{
    private const string InventoryAppId = "inventory";

    /// <inheritdoc />
    public async Task<Guid?> GetCatalogSectionIdAsync(Guid eventId, Guid allocationId, CancellationToken cancellationToken)
    {
        var allocations = await daprClient.InvokeMethodAsync<IReadOnlyList<InventoryGaAllocationDto>>(
            HttpMethod.Get,
            InventoryAppId,
            $"v1/events/{eventId}/inventory/general-admission",
            cancellationToken);

        return allocations.FirstOrDefault(a => a.AllocationId == allocationId)?.CatalogSectionId;
    }
}
