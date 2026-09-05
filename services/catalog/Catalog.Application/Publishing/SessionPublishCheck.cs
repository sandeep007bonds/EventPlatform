namespace Catalog.Application.Publishing;

/// <summary>
/// The pre-flight a performance has to pass before it can be sold, and the message it produces.
/// </summary>
/// <remarks>
/// Shared by publishing a whole event and publishing one late-added performance, because they ask
/// exactly the same questions and a second copy would be the one that drifted. It is the last point
/// at which a mistake is cheap: Inventory provisions from the message and has no way to ask a
/// follow-up, so a block with no allocation would quietly become capacity nobody can buy.
/// </remarks>
public static class SessionPublishCheck
{
    /// <summary>Runs the pre-flight for one performance.</summary>
    /// <param name="session">The performance.</param>
    /// <param name="ticketTypes">The ticket-type repository.</param>
    /// <param name="venue">The Venue service client.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The readiness, carrying either a problem or the priced allocation list.</returns>
    public static async Task<SessionPublishReadiness> RunAsync(
        EventSession session,
        ITicketTypeRepository ticketTypes,
        IVenueClient venue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(ticketTypes);
        ArgumentNullException.ThrowIfNull(venue);

        var label = session.Name ?? session.StartsAt.ToString("u", CultureInfo.InvariantCulture);

        if (session.SeatMapId is not { } seatMapId || session.SeatMapVersionId is null)
        {
            return SessionPublishReadiness.Blocked(
                $"'{label}' has no seat map. Attach a published seat-map version before selling it.");
        }

        var version = await venue.GetSeatMapVersionAsync(seatMapId, session.SeatMapVersionNumber, cancellationToken);
        if (version is null)
        {
            return SessionPublishReadiness.Blocked($"'{label}' points at a seat map the venue no longer has.");
        }

        if (!version.IsPublished || version.SeatMapVersionId != session.SeatMapVersionId)
        {
            return SessionPublishReadiness.Blocked(
                $"'{label}' is pinned to a seat-map version that is no longer the published one. Re-attach the map.");
        }

        var allocatedCodes = session.Allocations.Select(a => a.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Every block must be sold as something. An unallocated section is not "free capacity" —
        // it is capacity Inventory will never hear about, so the map renders with a hole in it and
        // nobody can tell the hole from a sold-out block.
        var unallocated = version.BlockCodes.Where(code => !allocatedCodes.Contains(code)).Order(StringComparer.Ordinal).ToList();
        if (unallocated.Count > 0)
        {
            return SessionPublishReadiness.Blocked(
                $"'{label}' has blocks with no ticket type: {string.Join(", ", unallocated)}.");
        }

        var types = await ticketTypes.ListForEventAsync(session.EventId, cancellationToken);
        var pricesById = types.Where(t => t.IsActive).ToDictionary(t => t.Id, t => t.PriceMinor);

        var priced = new List<SessionAllocationPayload>();
        foreach (var allocation in session.Allocations)
        {
            if (!pricesById.TryGetValue(allocation.TicketTypeId, out var priceMinor))
            {
                return SessionPublishReadiness.Blocked(
                    $"'{label}' allocates block '{allocation.Code}' to a ticket type that is inactive or no longer exists.");
            }

            priced.Add(new SessionAllocationPayload(allocation.Code, allocation.TicketTypeId, priceMinor));
        }

        return SessionPublishReadiness.Ready(version.Capacity, priced);
    }
}
