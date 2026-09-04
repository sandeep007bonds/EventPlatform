namespace Ticketing.Application.Provisioning;

/// <summary>
/// Warms Ticketing's local scan cache for a published performance — the check-in window and every
/// gate-restricted seat and admission area — so <c>ScanTicketAsync</c> never needs a live
/// cross-service call. Runs once per performance, triggered by Catalog's
/// <c>EventSessionPublished</c>. Idempotent: re-provisioning a performance that already has a scan
/// context is a no-op, so at-least-once delivery is safe.
/// </summary>
/// <param name="scanContexts">The scan-context repository.</param>
/// <param name="venueGateMaps">The Venue gate-map client.</param>
/// <param name="inventoryGa">The Inventory general-admission-allocation client.</param>
public sealed class SessionScanContextProvisioningService(
    ISessionScanContextRepository scanContexts,
    IVenueGateMapClient venueGateMaps,
    IInventoryGaClient inventoryGa)
{
    // Inventory provisions general-admission allocations from the same EventPublished message,
    // asynchronously — this handler can race ahead of it. A few short retries covers the normal
    // case; if allocations are still missing after these, an event with no GA sections looks
    // identical to a race that never resolved, so this degrades safely either way (see below).
    private const int GaAllocationRetryAttempts = 5;
    private static readonly TimeSpan GaAllocationRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>Warms the scan cache for a performance, unless it's already warmed.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventSessionId">The published performance.</param>
    /// <param name="seatMapId">The Venue seat map the performance pinned.</param>
    /// <param name="seatMapVersionNumber">That version's number.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if any.</param>
    /// <param name="startsAt">Scheduled start time (UTC).</param>
    /// <param name="endsAt">Scheduled end time (UTC).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if this call provisioned the cache; <see langword="false"/> if it already existed.</returns>
    public async Task<bool> ProvisionAsync(
        Guid tenantId,
        Guid eventSessionId,
        Guid seatMapId,
        int seatMapVersionNumber,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CancellationToken cancellationToken)
    {
        if (await scanContexts.ExistsForSessionAsync(eventSessionId, cancellationToken))
        {
            return false;
        }

        scanContexts.AddContext(SessionScanContext.Create(eventSessionId, tenantId, doorsOpenAt, startsAt, endsAt));

        var gateMap = await venueGateMaps.GetGateMapAsync(seatMapId, seatMapVersionNumber, cancellationToken);

        var seatGates = gateMap.EntryGateIdBySeatId
            .Select(pair => SeatEntryGate.Create(pair.Key, eventSessionId, pair.Value))
            .ToList();
        scanContexts.AddSeatGates(seatGates);

        if (gateMap.EntryGateIdByAdmissionAreaId.Count > 0)
        {
            var allocationSections = await GetAllocationSectionsWithRetryAsync(eventSessionId, cancellationToken);

            var allocationGates = allocationSections
                .Where(pair => gateMap.EntryGateIdByAdmissionAreaId.ContainsKey(pair.Value))
                .Select(pair => GaAllocationGate.Create(pair.Key, eventSessionId, gateMap.EntryGateIdByAdmissionAreaId[pair.Value]))
                .ToList();
            scanContexts.AddGaAllocationGates(allocationGates);
        }

        await scanContexts.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyDictionary<Guid, Guid>> GetAllocationSectionsWithRetryAsync(Guid eventSessionId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= GaAllocationRetryAttempts; attempt++)
        {
            var allocationSections = await inventoryGa.GetAllocationAdmissionAreasAsync(eventSessionId, cancellationToken);
            if (allocationSections.Count > 0 || attempt == GaAllocationRetryAttempts)
            {
                return allocationSections;
            }

            await Task.Delay(GaAllocationRetryDelay, cancellationToken);
        }

        return new Dictionary<Guid, Guid>();
    }
}
