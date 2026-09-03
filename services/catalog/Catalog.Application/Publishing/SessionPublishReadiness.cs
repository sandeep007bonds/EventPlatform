namespace Catalog.Application.Publishing;

/// <summary>
/// Whether a performance can be sold, and — when it can — everything the message announcing it
/// needs.
/// </summary>
/// <param name="Problem">
/// Why it cannot be sold, in words an organizer can act on; <see langword="null"/> when it can.
/// </param>
/// <param name="Capacity">Sellable capacity of the pinned seat-map version.</param>
/// <param name="Allocations">Each block, its ticket type, and that type's price at this moment.</param>
public sealed record SessionPublishReadiness(
    string? Problem,
    int Capacity,
    IReadOnlyList<SessionAllocationPayload> Allocations)
{
    /// <summary>The performance cannot be sold.</summary>
    /// <param name="problem">Why.</param>
    /// <returns>A blocked readiness.</returns>
    public static SessionPublishReadiness Problem(string problem) => new(problem, 0, []);

    /// <summary>The performance is ready to sell.</summary>
    /// <param name="capacity">Sellable capacity of the pinned seat-map version.</param>
    /// <param name="allocations">Each block, its ticket type, and that type's price.</param>
    /// <returns>A ready readiness.</returns>
    public static SessionPublishReadiness Ready(int capacity, IReadOnlyList<SessionAllocationPayload> allocations) =>
        new(null, capacity, allocations);

    /// <summary>
    /// Builds the integration event that tells the rest of the platform this performance is on sale.
    /// </summary>
    /// <remarks>
    /// The allocation list travels inline. It is tens of rows even for a stadium — one per block,
    /// not one per seat — so carrying it saves Inventory a call back to Catalog on every
    /// provisioning run. The seats themselves are read from Venue, which is where they live.
    /// </remarks>
    /// <param name="event">The event the performance belongs to.</param>
    /// <param name="session">The performance.</param>
    /// <returns>The integration event to enqueue on the outbox.</returns>
    public EventSessionPublished ToIntegrationEvent(Event @event, EventSession session)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(session);

        return new EventSessionPublished(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            @event.TenantId,
            @event.Id,
            session.Id,
            session.VenueId!.Value,
            session.SeatMapId!.Value,
            session.SeatMapVersionId!.Value,
            session.SeatMapVersionNumber!.Value,
            Capacity,
            session.StartsAt,
            session.EndsAt,
            session.DoorsOpenAt,
            session.BookingEndsAt,
            @event.OnSaleAt,
            @event.MaxTicketsPerBuyer,
            @event.RequiresQueue,
            @event.Currency,
            Allocations.Select(a => new SessionAllocationContract(a.Code, a.TicketTypeId, a.PriceMinor)).ToList());
    }
}
