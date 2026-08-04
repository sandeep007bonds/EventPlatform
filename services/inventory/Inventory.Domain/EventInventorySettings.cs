namespace Inventory.Domain;

/// <summary>
/// Per-event settings Inventory needs but that don't belong to any one seat/allocation — today
/// just the enforced booking cutoff, learned from Catalog's <c>EventPublished</c>. Upserted
/// idempotently by provisioning, alongside <see cref="InventoryItem"/>/<see cref="GeneralAdmissionAllocation"/> rows.
/// </summary>
public sealed class EventInventorySettings
{
    // Parameterless ctor for EF Core materialization.
    private EventInventorySettings()
    {
    }

    private EventInventorySettings(Guid eventId, Guid tenantId, DateTimeOffset? bookingEndsAt, int? maxTicketsPerBuyer, DateTimeOffset? onSaleAt)
    {
        EventId = eventId;
        TenantId = tenantId;
        BookingEndsAt = bookingEndsAt;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        OnSaleAt = onSaleAt;
    }

    /// <summary>The event these settings belong to (primary key).</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Enforced booking cutoff (UTC), if set — <c>HoldService.PlaceHoldAsync</c> rejects new holds
    /// for this event once <see cref="DateTimeOffset.UtcNow"/> passes this time.
    /// </summary>
    public DateTimeOffset? BookingEndsAt { get; private set; }

    /// <summary>
    /// Maximum tickets a single buyer may hold for this event, if limited — <c>HoldService.PlaceHoldAsync</c>
    /// rejects a new hold once the buyer's active-plus-converted commitment would exceed this.
    /// </summary>
    public int? MaxTicketsPerBuyer { get; private set; }

    /// <summary>
    /// Enforced on-sale start (UTC), if set — <c>HoldService.PlaceHoldAsync</c> rejects new holds
    /// for this event until <see cref="DateTimeOffset.UtcNow"/> reaches this time.
    /// </summary>
    public DateTimeOffset? OnSaleAt { get; private set; }

    /// <summary>Creates the settings row for an event.</summary>
    /// <param name="eventId">The event.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC), if any.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit, if any.</param>
    /// <param name="onSaleAt">Enforced on-sale start (UTC), if any.</param>
    /// <returns>A new <see cref="EventInventorySettings"/>.</returns>
    public static EventInventorySettings Create(Guid eventId, Guid tenantId, DateTimeOffset? bookingEndsAt, int? maxTicketsPerBuyer, DateTimeOffset? onSaleAt) =>
        new(eventId, tenantId, bookingEndsAt, maxTicketsPerBuyer, onSaleAt);

    /// <summary>
    /// Updates the booking cutoff, per-buyer ticket limit, and on-sale start — called on
    /// redelivery of <c>EventPublished</c>, keeping this idempotent alongside the existing
    /// per-event provisioning guard. Unreachable today: <c>InventoryProvisioningService.ProvisionAsync</c>
    /// short-circuits on <c>ExistsForEventAsync</c> before this would ever run — kept symmetrical
    /// for when post-publish updates are supported.
    /// </summary>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC), if any.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit, if any.</param>
    /// <param name="onSaleAt">Enforced on-sale start (UTC), if any.</param>
    public void Update(DateTimeOffset? bookingEndsAt, int? maxTicketsPerBuyer, DateTimeOffset? onSaleAt)
    {
        BookingEndsAt = bookingEndsAt;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        OnSaleAt = onSaleAt;
    }
}
