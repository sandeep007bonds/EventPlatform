namespace Ticketing.Domain;

/// <summary>
/// Per-event data a ticket scan needs but that isn't part of any one <see cref="Ticket"/> — the
/// check-in window, learned once from Catalog's <c>EventPublished</c> and never queried live at
/// scan time. Upserted idempotically by <c>EventScanContextProvisioningService</c>, mirroring
/// Inventory's <c>EventInventorySettings</c>.
/// </summary>
public sealed class EventScanContext
{
    // Parameterless ctor for EF Core materialization.
    private EventScanContext()
    {
    }

    private EventScanContext(Guid eventId, Guid tenantId, DateTimeOffset? doorsOpenAt, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        EventId = eventId;
        TenantId = tenantId;
        DoorsOpenAt = doorsOpenAt;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    /// <summary>The event these settings belong to (primary key).</summary>
    public Guid EventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Doors-open time (UTC), if set — the check-in window opens here, falling back to <see cref="StartsAt"/>.</summary>
    public DateTimeOffset? DoorsOpenAt { get; private set; }

    /// <summary>Scheduled start time (UTC) — see <see cref="DoorsOpenAt"/>.</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Scheduled end time (UTC) — a scan is rejected after this time.</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Creates the scan context row for an event.</summary>
    /// <param name="eventId">The event.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if any.</param>
    /// <param name="startsAt">Scheduled start time (UTC).</param>
    /// <param name="endsAt">Scheduled end time (UTC).</param>
    /// <returns>A new <see cref="EventScanContext"/>.</returns>
    public static EventScanContext Create(Guid eventId, Guid tenantId, DateTimeOffset? doorsOpenAt, DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        new(eventId, tenantId, doorsOpenAt, startsAt, endsAt);

    /// <summary>Whether <paramref name="now"/> falls within the check-in window.</summary>
    /// <param name="now">The current time (UTC).</param>
    /// <returns><see langword="true"/> if a scan at this instant is within the window.</returns>
    public bool IsWithinCheckInWindow(DateTimeOffset now) => now >= (DoorsOpenAt ?? StartsAt) && now <= EndsAt;
}
