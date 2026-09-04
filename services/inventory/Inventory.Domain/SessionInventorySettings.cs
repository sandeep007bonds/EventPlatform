namespace Inventory.Domain;

/// <summary>
/// The per-performance settings Inventory needs but that belong to no single seat or pool — the
/// selling window, the buyer limit, the queue requirement and the manual pause. Learned from
/// Catalog's <c>EventSessionPublished</c> and upserted idempotently alongside the
/// <see cref="InventoryItem"/>/<see cref="GeneralAdmissionAllocation"/> rows.
/// </summary>
/// <remarks>
/// Keyed by performance, because that is what has a booking cutoff and can be paused on its own.
/// It still carries <see cref="CatalogEventId"/>: the on-sale time, the queue requirement and the
/// per-buyer limit are decisions about the whole run, and the limit in particular has to be counted
/// across every performance or a buyer takes the cap once per night (ADR-0039).
/// </remarks>
public sealed class SessionInventorySettings
{
    // Parameterless ctor for EF Core materialization.
    private SessionInventorySettings()
    {
    }

    private SessionInventorySettings(
        Guid eventSessionId,
        Guid catalogEventId,
        Guid tenantId,
        DateTimeOffset? bookingEndsAt,
        int? maxTicketsPerBuyer,
        DateTimeOffset? onSaleAt,
        bool requiresQueue)
    {
        EventSessionId = eventSessionId;
        CatalogEventId = catalogEventId;
        TenantId = tenantId;
        BookingEndsAt = bookingEndsAt;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        OnSaleAt = onSaleAt;
        RequiresQueue = requiresQueue;
    }

    /// <summary>The performance these settings belong to (primary key).</summary>
    public Guid EventSessionId { get; private set; }

    /// <summary>The event that performance belongs to.</summary>
    public Guid CatalogEventId { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Enforced booking cutoff (UTC), if set — <c>HoldService.PlaceHoldAsync</c> rejects new holds
    /// for this performance once <see cref="DateTimeOffset.UtcNow"/> passes it. Per performance,
    /// because "book until two hours before the show" is a different instant every night.
    /// </summary>
    public DateTimeOffset? BookingEndsAt { get; private set; }

    /// <summary>
    /// Maximum tickets a single buyer may hold for the <b>event</b>, if limited — counted across
    /// every performance of the run, summing their active and converted holds.
    /// </summary>
    public int? MaxTicketsPerBuyer { get; private set; }

    /// <summary>
    /// Enforced on-sale start (UTC), if set — an event-level decision, repeated on every
    /// performance so a hold needs one lookup rather than two.
    /// </summary>
    public DateTimeOffset? OnSaleAt { get; private set; }

    /// <summary>
    /// Whether a buyer must present a valid Queue-service admission token to place a hold. Verified
    /// locally (HMAC), never via a call to the Queue service (ADR-0026). An event-level decision:
    /// one waiting room gates one on-sale.
    /// </summary>
    public bool RequiresQueue { get; private set; }

    /// <summary>
    /// Whether an organizer has manually paused sales for this performance — learned from Catalog's
    /// <c>EventSalesPaused</c>/<c>EventSalesResumed</c>, not from provisioning (sales are never
    /// paused at publish time). New holds are rejected while this is <see langword="true"/>,
    /// without affecting already-placed holds or tickets.
    /// </summary>
    public bool SalesPaused { get; private set; }

    /// <summary>Creates the settings row for a performance.</summary>
    /// <param name="eventSessionId">The performance.</param>
    /// <param name="catalogEventId">The event it belongs to.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC), if any.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit across the run, if any.</param>
    /// <param name="onSaleAt">Enforced on-sale start (UTC), if any.</param>
    /// <param name="requiresQueue">Whether a Queue admission token is required at hold time.</param>
    /// <returns>A new <see cref="SessionInventorySettings"/>.</returns>
    public static SessionInventorySettings Create(
        Guid eventSessionId,
        Guid catalogEventId,
        Guid tenantId,
        DateTimeOffset? bookingEndsAt,
        int? maxTicketsPerBuyer,
        DateTimeOffset? onSaleAt,
        bool requiresQueue) =>
        new(eventSessionId, catalogEventId, tenantId, bookingEndsAt, maxTicketsPerBuyer, onSaleAt, requiresQueue);

    /// <summary>
    /// Updates the window, limit and queue requirement — called on redelivery of
    /// <c>EventSessionPublished</c>, keeping provisioning idempotent.
    /// </summary>
    /// <param name="bookingEndsAt">Enforced booking cutoff (UTC), if any.</param>
    /// <param name="maxTicketsPerBuyer">Per-buyer ticket limit across the run, if any.</param>
    /// <param name="onSaleAt">Enforced on-sale start (UTC), if any.</param>
    /// <param name="requiresQueue">Whether a Queue admission token is required at hold time.</param>
    public void Update(
        DateTimeOffset? bookingEndsAt,
        int? maxTicketsPerBuyer,
        DateTimeOffset? onSaleAt,
        bool requiresQueue)
    {
        BookingEndsAt = bookingEndsAt;
        MaxTicketsPerBuyer = maxTicketsPerBuyer;
        OnSaleAt = onSaleAt;
        RequiresQueue = requiresQueue;
    }

    /// <summary>
    /// Sets the manual sales-paused flag — called on <c>EventSalesPaused</c>/<c>EventSalesResumed</c>.
    /// </summary>
    /// <param name="salesPaused">The new paused state.</param>
    public void SetSalesPaused(bool salesPaused)
    {
        SalesPaused = salesPaused;
    }
}
