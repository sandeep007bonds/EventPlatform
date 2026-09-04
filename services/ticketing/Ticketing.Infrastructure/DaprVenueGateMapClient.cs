namespace Ticketing.Infrastructure;

/// <summary>
/// Reads a Venue seat-map version's gate assignments via Dapr service invocation (app-id
/// <c>venue</c>), keeping the cross-service call behind the <see cref="IVenueGateMapClient"/> port.
/// Called once per performance by <c>SessionScanContextProvisioningService</c>, mirroring
/// <c>Inventory.Infrastructure/DaprSeatMapClient.cs</c>'s pattern.
/// </summary>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprVenueGateMapClient(DaprClient daprClient) : IVenueGateMapClient
{
    private const string VenueAppId = "venue";

    /// <inheritdoc />
    public async Task<VenueGateMap> GetGateMapAsync(
        Guid seatMapId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var route = $"v1/seat-maps/{seatMapId}?version={versionNumber.ToString(CultureInfo.InvariantCulture)}";

        var map = await daprClient.InvokeMethodAsync<VenueScanSeatMap>(
            HttpMethod.Get,
            VenueAppId,
            route,
            cancellationToken);

        // Flattened from section to seat: the gate is a property of the section, but a scan knows
        // only the seat on the ticket, so the answer has to be reachable by seat in one lookup.
        var gateBySeatId = map.Version.Sections
            .Where(section => section.GateId is not null)
            .SelectMany(section => section.Rows
                .SelectMany(row => row.Seats)
                .Select(seat => (seat.Id, GateId: section.GateId!.Value)))
            .ToDictionary(pair => pair.Id, pair => pair.GateId);

        var gateByAreaId = map.Version.AdmissionAreas
            .Where(area => area.GateId is not null)
            .ToDictionary(area => area.Id, area => area.GateId!.Value);

        return new VenueGateMap(gateBySeatId, gateByAreaId);
    }
}
