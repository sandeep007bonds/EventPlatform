namespace Ticketing.Application.Issuing;

/// <summary>
/// Issues tickets when an order is confirmed — one ticket per sold seat, and one ticket per
/// general-admission unit purchased. Idempotent: re-delivery of <c>OrderConfirmed</c> for an
/// already-ticketed order is a no-op.
/// </summary>
/// <param name="tickets">The ticket repository.</param>
/// <param name="events">The integration-event publisher (outbox).</param>
public sealed class TicketIssuingService(ITicketRepository tickets, IEventPublisher events)
{
    /// <summary>Issues tickets for a confirmed order, unless it is already ticketed.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="orderId">The confirmed order.</param>
    /// <param name="catalogEventId">The show/event.</param>
    /// <param name="eventSessionId">The performance the tickets admit to.</param>
    /// <param name="userId">The ticket holder.</param>
    /// <param name="lines">The purchased lines — reserved seats and/or general-admission quantities.</param>
    /// <param name="buyerEmail">The buyer's email, for ticket delivery — see <see cref="OrderTicketsIssued"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The issue result.</returns>
    public async Task<IssueResult> IssueAsync(
        Guid tenantId,
        Guid orderId,
        Guid catalogEventId,
        Guid eventSessionId,
        Guid userId,
        IReadOnlyList<OrderLineSummary> lines,
        string? buyerEmail,
        CancellationToken cancellationToken)
    {
        if (await tickets.ExistsForOrderAsync(orderId, cancellationToken))
        {
            return new IssueResult(Issued: false, TicketCount: 0);
        }

        var issued = new List<Ticket>();
        foreach (var line in lines)
        {
            for (var i = 0; i < line.Quantity; i++)
            {
                var ticket = Ticket.Create(
                    tenantId,
                    orderId,
                    catalogEventId,
                    eventSessionId,
                    line.SeatId,
                    line.GeneralAdmissionAllocationId,
                    userId,
                    NewToken());
                issued.Add(ticket);

                events.Enqueue(new TicketIssued(
                    Guid.CreateVersion7(),
                    DateTimeOffset.UtcNow,
                    tenantId,
                    ticket.Id,
                    orderId,
                    catalogEventId,
                    eventSessionId,
                    line.SeatId,
                    line.GeneralAdmissionAllocationId,
                    userId));
            }
        }

        tickets.AddRange(issued);

        events.Enqueue(new OrderTicketsIssued(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            tenantId,
            orderId,
            catalogEventId,
            eventSessionId,
            userId,
            buyerEmail,
            issued.Select(ticket => new IssuedTicketSummary(
                ticket.Id, ticket.SeatId, ticket.GeneralAdmissionAllocationId, ticket.Token)).ToList()));

        await tickets.SaveChangesAsync(cancellationToken);

        return new IssueResult(Issued: true, TicketCount: issued.Count);
    }

    private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
