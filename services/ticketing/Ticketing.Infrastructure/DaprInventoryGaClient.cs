namespace Ticketing.Infrastructure;

/// <summary>
/// Resolves every general-admission allocation's Venue admission-area id via Dapr service
/// invocation (app-id <c>inventory</c>), reusing Inventory's existing anonymous
/// general-admission-pools endpoint — no new Inventory code needed. Called once per performance by
/// <c>SessionScanContextProvisioningService</c>.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprInventoryGaClient(DaprClient daprClient) : IInventoryGaClient
{
    private const string InventoryAppId = "inventory";

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, Guid>> GetAllocationAdmissionAreasAsync(Guid eventSessionId, CancellationToken cancellationToken)
    {
        var allocations = await daprClient.InvokeMethodAsync<IReadOnlyList<InventoryGaAllocationDto>>(
            HttpMethod.Get,
            InventoryAppId,
            $"v1/sessions/{eventSessionId}/inventory/general-admission",
            cancellationToken);

        return allocations.ToDictionary(a => a.AllocationId, a => a.AdmissionAreaId);
    }
}
