namespace Ticketing.Infrastructure;

/// <summary>
/// Reads an event's check-in window and section-to-gate mapping from Catalog via Dapr service
/// invocation (app-id <c>catalog</c>), keeping the cross-service calls behind the
/// <see cref="ICatalogEventClient"/> port. Two requests: the event itself (window bounds) and its
/// seat map (per-section gate ids).
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprCatalogEventClient(DaprClient daprClient) : ICatalogEventClient
{
    private const string CatalogAppId = "catalog";

    /// <inheritdoc />
    public async Task<CatalogScanContext> GetScanContextAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await daprClient.InvokeMethodAsync<CatalogEventDto>(
            HttpMethod.Get,
            CatalogAppId,
            $"v1/events/{eventId}",
            cancellationToken);

        var seatMap = await daprClient.InvokeMethodAsync<CatalogSeatMapForScan>(
            HttpMethod.Get,
            CatalogAppId,
            $"v1/events/{eventId}/seatmap",
            cancellationToken);

        var gateBySeatId = seatMap.Seats.ToDictionary(s => s.Id, s => s.EntryGateId);
        var gateByCatalogSectionId = seatMap.GeneralAdmissionSections.ToDictionary(s => s.Id, s => s.EntryGateId);

        return new CatalogScanContext(@event.DoorsOpenAt, @event.StartsAt, @event.EndsAt, gateBySeatId, gateByCatalogSectionId);
    }
}
