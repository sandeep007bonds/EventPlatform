namespace Ticketing.Domain;

/// <summary>
/// Per-performance data a ticket scan needs but that isn't part of any one <see cref="Ticket"/> —
/// the check-in window, learned once from Catalog's <c>EventSessionPublished</c> and never queried
/// live at scan time. Upserted idempotently by <c>SessionScanContextProvisioningService</c>,
/// mirroring Inventory's <c>SessionInventorySettings</c>.
/// </summary>
/// <remarks>
/// Keyed by performance, not by event (ADR-0039): the window is a different pair of instants every
/// night, and a scanner at a three-night run must let tonight's ticket in and turn tomorrow's away.
/// </remarks>
public sealed class SessionScanContext
{
    // Parameterless ctor for EF Core materialization.
    private SessionScanContext()
    {
    }

    private SessionScanContext(
        Guid eventSessionId,
        Guid tenantId,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt)
    {
        EventSessionId = eventSessionId;
        TenantId = tenantId;
        DoorsOpenAt = doorsOpenAt;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    /// <summary>The performance these settings belong to (primary key).</summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Doors-open time (UTC), if set — the check-in window opens here, falling back to <see cref="StartsAt"/>.</summary>
    public DateTimeOffset? DoorsOpenAt { get; private set; }

    /// <summary>Scheduled start time (UTC) — see <see cref="DoorsOpenAt"/>.</summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>Scheduled end time (UTC) — a scan is rejected after this time.</summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Creates the scan context row for a performance.</summary>
    /// <param name="eventSessionId">The performance.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="doorsOpenAt">Doors-open time (UTC), if any.</param>
    /// <param name="startsAt">Scheduled start time (UTC).</param>
    /// <param name="endsAt">Scheduled end time (UTC).</param>
    /// <returns>A new <see cref="SessionScanContext"/>.</returns>
    public static SessionScanContext Create(
        Guid eventSessionId,
        Guid tenantId,
        DateTimeOffset? doorsOpenAt,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt) =>
        new(eventSessionId, tenantId, doorsOpenAt, startsAt, endsAt);

    /// <summary>Whether <paramref name="now"/> falls within the check-in window.</summary>
    /// <param name="now">The current time (UTC).</param>
    /// <returns><see langword="true"/> if a scan at this instant is within the window.</returns>
    public bool IsWithinCheckInWindow(DateTimeOffset now) => now >= (DoorsOpenAt ?? StartsAt) && now <= EndsAt;
}
