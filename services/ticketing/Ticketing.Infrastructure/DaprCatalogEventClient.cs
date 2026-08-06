namespace Ticketing.Infrastructure;

/// <summary>
/// Reads Catalog's seat-map entry-gate assignments via Dapr service invocation (app-id
/// <c>catalog</c>), keeping the cross-service call behind the <see cref="ICatalogEventClient"/>
/// port. Called once per event by <c>EventScanContextProvisioningService</c>, mirroring
/// <c>Inventory.Infrastructure/DaprSeatMapClient.cs</c>'s pattern.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprCatalogEventClient(DaprClient daprClient) : ICatalogEventClient
{
    private const string CatalogAppId = "catalog";

    /// <inheritdoc />
    public async Task<CatalogGateMap> GetGateMapAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var seatMap = await daprClient.InvokeMethodAsync<CatalogSeatMapForScan>(
            HttpMethod.Get,
            CatalogAppId,
            $"v1/events/{eventId}/seatmap",
            cancellationToken);

        var gateBySeatId = seatMap.Seats
            .Where(s => s.EntryGateId is not null)
            .ToDictionary(s => s.Id, s => s.EntryGateId!.Value);

        var gateByCatalogSectionId = seatMap.GeneralAdmissionSections
            .Where(s => s.EntryGateId is not null)
            .ToDictionary(s => s.Id, s => s.EntryGateId!.Value);

        return new CatalogGateMap(gateBySeatId, gateByCatalogSectionId);
    }
}
