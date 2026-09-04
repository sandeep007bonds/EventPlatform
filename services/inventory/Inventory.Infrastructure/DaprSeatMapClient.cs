namespace Inventory.Infrastructure;

/// <summary>
/// Reads a Venue seat-map version via Dapr service invocation (app-id <c>venue</c>), keeping the
/// cross-service call behind the <see cref="ISeatMapClient"/> port.
/// </summary>
/// <remarks>
/// The version is requested by number rather than "whichever is published", because the performance
/// pinned one — resolving it again could hand back a map the tickets were never sold against
/// (ADR-0039).
/// </remarks>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprSeatMapClient(DaprClient daprClient) : ISeatMapClient
{
    private const string VenueAppId = "venue";

    /// <inheritdoc />
    public async Task<SeatMapSnapshot> GetSeatMapAsync(
        Guid seatMapId,
        int versionNumber,
        CancellationToken cancellationToken)
    {
        var route = $"v1/seat-maps/{seatMapId}?version={versionNumber.ToString(CultureInfo.InvariantCulture)}";

        var map = await daprClient.InvokeMethodAsync<VenueSeatMap>(
            HttpMethod.Get,
            VenueAppId,
            route,
            cancellationToken);

        // Flattened section -> row -> seat, carrying the section code down onto each seat: the
        // allocation map binds by code, and provisioning needs the pair together to know what the
        // seat sells as.
        var seats = map.Version.Sections
            .SelectMany(section => section.Rows
                .SelectMany(row => row.Seats)
                .Select(seat => new SeatSnapshot(seat.Id, section.Code, seat.IsSellable)))
            .ToList();

        var areas = map.Version.AdmissionAreas
            .Select(area => new AdmissionAreaSnapshot(area.Id, area.Code, area.Capacity))
            .ToList();

        return new SeatMapSnapshot(seats, areas);
    }
}
