namespace Inventory.Application.Provisioning;

/// <summary>
/// Generates inventory for one published performance by reading its pinned Venue seat-map version
/// and joining it to the performance's allocation map.
/// </summary>
/// <remarks>
/// Idempotent: a performance that already has a settings row is a no-op, so at-least-once delivery
/// of <c>EventSessionPublished</c> is safe.
/// <para>
/// The join is by <b>code</b>. Venue says which seats exist and which block each is in; Catalog says
/// which ticket type each block sells as and at what price. Neither service knows both, and the
/// code is the only thing they agree on — which is why it is stable across renames by design.
/// </para>
/// </remarks>
/// <param name="inventory">The inventory repository.</param>
/// <param name="seatMaps">The Venue seat-map client.</param>
/// <param name="holdStore">The Redis fast gate, used to initialize general-admission capacity counters.</param>
public sealed class InventoryProvisioningService(
    IInventoryRepository inventory,
    ISeatMapClient seatMaps,
    IHoldStore holdStore)
{
    /// <summary>Provisions inventory for a performance, unless it already exists.</summary>
    /// <param name="request">Everything the published performance announced.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The provisioning result.</returns>
    public async Task<ProvisioningResult> ProvisionAsync(
        ProvisionSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await inventory.ExistsForSessionAsync(request.EventSessionId, cancellationToken))
        {
            return new ProvisioningResult(Provisioned: false, SeatCount: 0, GeneralAdmissionAllocationCount: 0);
        }

        var seatMap = await seatMaps.GetSeatMapAsync(request.SeatMapId, request.SeatMapVersionNumber, cancellationToken);
        var allocationsByCode = request.Allocations.ToDictionary(a => a.Code, StringComparer.OrdinalIgnoreCase);

        var items = new List<InventoryItem>();
        foreach (var seat in seatMap.Seats)
        {
            // A seat in a block with no allocation is skipped rather than guessed at. Usually that
            // is the performance excluding the block — the upper tier closed for a half-house show,
            // which Catalog simply leaves out of the payload. If instead the two services have
            // drifted, skipping is still the right answer: inventing a price would turn a
            // disagreement into a wrong sale.
            if (!allocationsByCode.TryGetValue(seat.SectionCode, out var allocation))
            {
                continue;
            }

            items.Add(InventoryItem.Create(
                request.TenantId,
                request.EventSessionId,
                request.CatalogEventId,
                seat.SeatId,
                allocation.TicketTypeId,
                allocation.PriceMinor,
                seat.IsSellable));
        }

        inventory.AddRange(items);

        var pools = new List<GeneralAdmissionAllocation>();
        foreach (var area in seatMap.AdmissionAreas)
        {
            if (!allocationsByCode.TryGetValue(area.Code, out var allocation))
            {
                continue;
            }

            // The performance may sell fewer than the area holds — 200 of a 400-capacity pit. Venue
            // still says 400, because that is the building; the smaller number is this night's
            // decision and Catalog carries it on the allocation.
            pools.Add(GeneralAdmissionAllocation.Create(
                request.TenantId,
                request.EventSessionId,
                request.CatalogEventId,
                area.AdmissionAreaId,
                allocation.TicketTypeId,
                allocation.PriceMinor,
                allocation.CapacityOverride ?? area.Capacity));
        }

        inventory.AddGeneralAdmissionAllocations(pools);

        inventory.AddSessionInventorySettings(SessionInventorySettings.Create(
            request.EventSessionId,
            request.CatalogEventId,
            request.TenantId,
            request.BookingEndsAt,
            request.MaxTicketsPerBuyer,
            request.OnSaleAt,
            request.RequiresQueue));

        await inventory.SaveChangesAsync(cancellationToken);

        foreach (var pool in pools)
        {
            await holdStore.InitializeGeneralAdmissionCapacityAsync(
                request.EventSessionId,
                pool.Id,
                pool.TotalCapacity,
                cancellationToken);
        }

        return new ProvisioningResult(
            Provisioned: true,
            SeatCount: items.Count,
            GeneralAdmissionAllocationCount: pools.Count);
    }
}
