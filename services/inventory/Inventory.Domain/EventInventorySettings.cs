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

    private EventInventorySettings(Guid eventId, Guid tenantId, DateTimeOffset? bookingEndsAt)
    {
        EventId = eventId;
        TenantId = tenantId;
        BookingEndsAt = bookingEndsAt;
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

    /// <summary>Creates the settings row for an event.</summary>
    /// <param name="eventId">The event.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC), if any.</param>
    /// <returns>A new <see cref="EventInventorySettings"/>.</returns>
    public static EventInventorySettings Create(Guid eventId, Guid tenantId, DateTimeOffset? bookingEndsAt) =>
        new(eventId, tenantId, bookingEndsAt);

    /// <summary>
    /// Updates the booking cutoff — called on redelivery of <c>EventPublished</c>, keeping this
    /// idempotent alongside the existing per-event provisioning guard.
    /// </summary>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC), if any.</param>
    public void UpdateBookingCutoff(DateTimeOffset? bookingEndsAt) => BookingEndsAt = bookingEndsAt;
}
