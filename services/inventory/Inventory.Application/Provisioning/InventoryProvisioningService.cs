namespace Inventory.Application.Provisioning;

/// <summary>
/// Generates seat and general-admission inventory for a published event by reading its Catalog
/// seat map, and records the event's enforced booking cutoff. Idempotent: re-provisioning an event
/// that already has a settings row is a no-op, so at-least-once delivery of <c>EventPublished</c>
/// is safe.
/// </summary>
/// <param name="inventory">The inventory repository.</param>
/// <param name="seatMaps">The Catalog seat-map client.</param>
/// <param name="holdStore">The Redis fast gate, used to initialize general-admission capacity counters.</param>
public sealed class InventoryProvisioningService(
    IInventoryRepository inventory,
    ISeatMapClient seatMaps,
    IHoldStore holdStore)
{
    /// <summary>Provisions inventory for an event, unless it already exists.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventId">The published event.</param>
    /// <param name="bookingEndsAt">The event's enforced booking cutoff (UTC), if any.</param>
    /// <param name="maxTicketsPerBuyer">The event's per-buyer ticket limit, if any.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The provisioning result.</returns>
    public async Task<ProvisioningResult> ProvisionAsync(
        Guid tenantId,
        Guid eventId,
        DateTimeOffset? bookingEndsAt,
        int? maxTicketsPerBuyer,
        CancellationToken cancellationToken)
    {
        if (await inventory.ExistsForEventAsync(eventId, cancellationToken))
        {
            return new ProvisioningResult(Provisioned: false, SeatCount: 0, GeneralAdmissionAllocationCount: 0);
        }

        var seatMap = await seatMaps.GetSeatMapAsync(eventId, cancellationToken);

        var items = seatMap.Seats
            .Select(seat => InventoryItem.Create(
                tenantId,
                eventId,
                seat.SeatId,
                seat.PriceTier,
                ToMinor(seat.PriceAmount)))
            .ToList();
        inventory.AddRange(items);

        var allocations = seatMap.GeneralAdmissionSections
            .Select(section => GeneralAdmissionAllocation.Create(
                tenantId,
                eventId,
                section.SectionId,
                section.PriceTier,
                ToMinor(section.PriceAmount),
                section.Capacity))
            .ToList();
        inventory.AddGeneralAdmissionAllocations(allocations);

        inventory.AddEventInventorySettings(EventInventorySettings.Create(eventId, tenantId, bookingEndsAt, maxTicketsPerBuyer));

        await inventory.SaveChangesAsync(cancellationToken);

        foreach (var allocation in allocations)
        {
            await holdStore.InitializeGeneralAdmissionCapacityAsync(
                eventId,
                allocation.Id,
                allocation.TotalCapacity,
                cancellationToken);
        }

        return new ProvisioningResult(Provisioned: true, SeatCount: items.Count, GeneralAdmissionAllocationCount: allocations.Count);
    }

    // Assumes a 2-decimal currency (see tracker T-currency): refine per ISO 4217 exponent later.
    private static long ToMinor(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
