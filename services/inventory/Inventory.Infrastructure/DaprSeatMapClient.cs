namespace Inventory.Infrastructure;

/// <summary>
/// Reads the Catalog seat map via Dapr service invocation (app-id <c>catalog</c>), keeping the
/// cross-service call behind the <see cref="ISeatMapClient"/> port. One request returns both
/// reserved seats and general-admission sections.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprSeatMapClient(DaprClient daprClient) : ISeatMapClient
{
    private const string CatalogAppId = "catalog";

    /// <inheritdoc />
    public async Task<SeatMapSnapshot> GetSeatMapAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var seatMap = await daprClient.InvokeMethodAsync<CatalogSeatMap>(
            HttpMethod.Get,
            CatalogAppId,
            $"v1/events/{eventId}/seatmap",
            cancellationToken);

        var seats = seatMap.Seats
            .Select(seat => new SeatSnapshot(seat.Id, seat.PriceTier, seat.PriceAmount))
            .ToList();

        var gaSections = seatMap.GeneralAdmissionSections
            .Select(section => new GeneralAdmissionSectionSnapshot(section.Id, section.PriceTier, section.PriceAmount, section.Capacity))
            .ToList();

        return new SeatMapSnapshot(seats, gaSections);
    }
}
