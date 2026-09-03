namespace Catalog.Infrastructure;

/// <summary>
/// Reads seat-map versions from the Venue service via Dapr service invocation (app-id
/// <c>venue</c>), keeping the cross-service call behind the <see cref="IVenueClient"/> port.
/// </summary>
/// <remarks>
/// Two calls, not one: the seat map gives the codes and the capacity, and the venue gives the name
/// and city Catalog caches for display. They are separate because only the first is ever needed for
/// a decision — the second exists to fill a
/// <see cref="Catalog.Domain.VenueSnapshot"/> and its staleness is cosmetic.
/// <para>
/// Both are cold-path: attaching a seat map to a performance, and validating a publish. Nothing a
/// buyer does reaches this.
/// </para>
/// </remarks>
/// <param name="daprClient">The Dapr client.</param>
internal sealed class DaprVenueClient(DaprClient daprClient) : IVenueClient
{
    private const string VenueAppId = "venue";
    private const string PublishedStatus = "Published";

    /// <inheritdoc />
    public async Task<SeatMapVersionSnapshot?> GetSeatMapVersionAsync(
        Guid seatMapId,
        int? versionNumber,
        CancellationToken cancellationToken)
    {
        var route = versionNumber is null
            ? $"v1/seat-maps/{seatMapId}"
            : $"v1/seat-maps/{seatMapId}?version={versionNumber.Value.ToString(CultureInfo.InvariantCulture)}";

        VenueSeatMapVersion? map;
        try
        {
            map = await daprClient.InvokeMethodAsync<VenueSeatMapVersion>(
                HttpMethod.Get,
                VenueAppId,
                route,
                cancellationToken);
        }
        catch (InvocationException exception) when (exception.Response?.StatusCode == HttpStatusCode.NotFound)
        {
            // A missing map is an ordinary answer here — the organizer typed an id that is not
            // theirs, or asked for a version that does not exist — so it is null, not an exception
            // the caller has to catch to give a sensible message.
            return null;
        }

        if (map is null)
        {
            return null;
        }

        var venue = await GetVenueAsync(map.VenueId, cancellationToken);

        var codes = map.Version.Sections.Select(s => s.Code)
            .Concat(map.Version.AdmissionAreas.Select(a => a.Code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new SeatMapVersionSnapshot(
            map.Id,
            map.VenueId,
            map.TenantId,
            map.Version.Id,
            map.Version.VersionNumber,
            string.Equals(map.Version.Status, PublishedStatus, StringComparison.Ordinal),
            map.Version.Capacity,
            codes,
            venue?.Name ?? "Unknown venue",
            venue?.Address.City ?? string.Empty,
            venue?.Address.Country ?? string.Empty,
            venue?.TimeZoneId);
    }

    private async Task<VenueDetail?> GetVenueAsync(Guid venueId, CancellationToken cancellationToken)
    {
        try
        {
            return await daprClient.InvokeMethodAsync<VenueDetail>(
                HttpMethod.Get,
                VenueAppId,
                $"v1/venues/{venueId}",
                cancellationToken);
        }
        catch (InvocationException)
        {
            // The display snapshot is a nicety. Failing the whole attach because the venue's name
            // could not be fetched would block real work over a cosmetic field, so this degrades
            // to placeholders and the caller carries on with ids that are all correct.
            return null;
        }
    }
}
