namespace Catalog.Application;

/// <summary>Projects <see cref="EventSession"/> into the shape the API returns.</summary>
/// <remarks>
/// One place rather than one per handler: the session appears inside the event, in the session
/// list, and as the echo from every session mutation, and three copies of the projection would
/// drift the first time a field was added.
/// </remarks>
public static class EventSessionMapper
{
    /// <summary>Projects one performance.</summary>
    /// <param name="session">The performance.</param>
    /// <returns>The API representation.</returns>
    public static EventSessionResponse ToResponse(this EventSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new EventSessionResponse(
            session.Id,
            session.EventId,
            session.Name,
            session.StartsAt,
            session.EndsAt,
            session.DoorsOpenAt,
            session.BookingEndsAt,
            session.Status.ToString(),
            session.SalesPaused,
            session.VenueId,
            session.SeatMapId,
            session.SeatMapVersionId,
            session.SeatMapVersionNumber,
            session.Venue?.Name,
            session.Venue?.City,
            session.Venue?.Country,
            session.Venue?.TimeZoneId,
            session.Allocations
                .OrderBy(a => a.Code, StringComparer.OrdinalIgnoreCase)
                .Select(a => new SessionAllocationResponse(a.Code, a.TicketTypeId))
                .ToList());
    }

    /// <summary>Projects an event's performances, earliest first.</summary>
    /// <param name="sessions">The performances.</param>
    /// <returns>The API representations, in performance order.</returns>
    public static IReadOnlyList<EventSessionResponse> ToResponses(this IEnumerable<EventSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        return sessions.OrderBy(s => s.StartsAt).Select(ToResponse).ToList();
    }
}
