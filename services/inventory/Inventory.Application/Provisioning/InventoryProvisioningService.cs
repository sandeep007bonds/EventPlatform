namespace Inventory.Application.Provisioning;

/// <summary>
/// Generates seat inventory for a published event by reading its Catalog seat map. Idempotent:
/// re-provisioning an event that already has inventory is a no-op, so at-least-once delivery of
/// <c>EventPublished</c> is safe.
/// </summary>
/// <param name="inventory">The inventory repository.</param>
/// <param name="seatMaps">The Catalog seat-map client.</param>
public sealed class InventoryProvisioningService(
    IInventoryRepository inventory,
    ISeatMapClient seatMaps)
{
    /// <summary>Provisions inventory for an event, unless it already exists.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventId">The published event.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The provisioning result.</returns>
    public async Task<ProvisioningResult> ProvisionAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (await inventory.ExistsForEventAsync(eventId, cancellationToken))
        {
            return new ProvisioningResult(Provisioned: false, SeatCount: 0);
        }

        var seats = await seatMaps.GetSeatsAsync(eventId, cancellationToken);
        var items = seats
            .Select(seat => InventoryItem.Create(
                tenantId,
                eventId,
                seat.SeatId,
                seat.PriceTier,
                ToMinor(seat.PriceAmount)))
            .ToList();

        inventory.AddRange(items);
        await inventory.SaveChangesAsync(cancellationToken);

        return new ProvisioningResult(Provisioned: true, SeatCount: items.Count);
    }

    // Assumes a 2-decimal currency (see tracker T-currency): refine per ISO 4217 exponent later.
    private static long ToMinor(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
