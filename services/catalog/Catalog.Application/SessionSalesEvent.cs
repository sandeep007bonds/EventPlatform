namespace Catalog.Application;

/// <summary>
/// Builds the paused/resumed integration event for one performance.
/// </summary>
/// <remarks>
/// Shared by the event-wide switch and the per-performance one so the two cannot construct the
/// message differently — they are the same announcement, reached by two routes.
/// </remarks>
public static class SessionSalesEvent
{
    /// <summary>Builds the event announcing that a performance's sales stopped or restarted.</summary>
    /// <param name="paused"><see langword="true"/> for paused, <see langword="false"/> for resumed.</param>
    /// <param name="event">The event the performance belongs to.</param>
    /// <param name="session">The performance.</param>
    /// <returns>The integration event to enqueue on the outbox.</returns>
    public static IntegrationEvent For(bool paused, Event @event, EventSession session)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(session);

        return paused
            ? new EventSalesPaused(Guid.CreateVersion7(), DateTimeOffset.UtcNow, @event.TenantId, @event.Id, session.Id)
            : new EventSalesResumed(Guid.CreateVersion7(), DateTimeOffset.UtcNow, @event.TenantId, @event.Id, session.Id);
    }
}
