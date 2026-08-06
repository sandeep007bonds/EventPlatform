namespace Ticketing.Application.Voiding;

/// <summary>
/// Voids every ticket for an order — a buyer-initiated cancellation/refund. All-or-nothing: a
/// ticket already checked in (the buyer already used it) blocks the whole order from voiding,
/// mirroring <c>SeatBlockingService</c>'s "all-or-nothing across the requested seats" precedent.
/// </summary>
/// <param name="tickets">The ticket repository.</param>
public sealed class TicketVoidingService(ITicketRepository tickets)
{
    /// <summary>Voids every ticket for an order, unless any of them is already checked in.</summary>
    /// <param name="orderId">The order whose tickets should be voided.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The void result.</returns>
    public async Task<VoidTicketsOutcome> VoidByOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var orderTickets = await tickets.GetTrackedByOrderAsync(orderId, cancellationToken);
        if (orderTickets.Count == 0)
        {
            return VoidTicketsOutcome.NoTickets;
        }

        if (orderTickets.Any(ticket => ticket.Status == TicketStatus.CheckedIn))
        {
            return VoidTicketsOutcome.AlreadyCheckedIn;
        }

        foreach (var ticket in orderTickets)
        {
            ticket.Void();
        }

        await tickets.SaveChangesAsync(cancellationToken);
        return VoidTicketsOutcome.Voided;
    }
}
