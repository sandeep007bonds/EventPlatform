namespace Ticketing.Tests.Issuing;

// Issuing runs off OrderConfirmed, which is delivered at least once. Getting the count wrong or
// running twice means a buyer with too few tickets to get their party in, or a venue admitting more
// people than it sold seats to.
public sealed class TicketIssuingServiceTests
{
    private readonly ITicketRepository tickets = Substitute.For<ITicketRepository>();
    private readonly IEventPublisher events = Substitute.For<IEventPublisher>();

    [Fact]
    public async Task EachReservedSeat_GetsItsOwnTicket()
    {
        var seats = Enumerable.Range(0, 3).Select(_ => Guid.CreateVersion7()).ToList();

        var result = await IssueAsync(seats.Select(seat => new OrderLineSummary(seat, null, 1)).ToList());

        result.Issued.ShouldBeTrue();
        result.TicketCount.ShouldBe(3);
        CapturedTickets().Select(ticket => ticket.SeatId).ShouldBe(seats, ignoreOrder: true);
    }

    // One general-admission line stands for N admissions, so the quantity has to expand into N
    // individually scannable tickets — four people cannot share one barcode at a turnstile.
    [Fact]
    public async Task AGeneralAdmissionLine_ExpandsIntoOneTicketPerAdmission()
    {
        var allocationId = Guid.CreateVersion7();

        var result = await IssueAsync([new OrderLineSummary(null, allocationId, 4)]);

        result.TicketCount.ShouldBe(4);
        var issued = CapturedTickets();
        issued.ShouldAllBe(ticket => ticket.GeneralAdmissionAllocationId == allocationId && ticket.SeatId == null);
        issued.Select(ticket => ticket.Token).Distinct().Count().ShouldBe(4);
    }

    [Fact]
    public async Task AMixedOrder_GetsATicketPerSeatPlusOnePerAdmission()
    {
        var result = await IssueAsync(
        [
            new OrderLineSummary(Guid.CreateVersion7(), null, 1),
            new OrderLineSummary(Guid.CreateVersion7(), null, 1),
            new OrderLineSummary(null, Guid.CreateVersion7(), 3),
        ]);

        result.TicketCount.ShouldBe(5);
    }

    // Tokens are what a QR code encodes; a collision would let one ticket scan as another.
    [Fact]
    public async Task EveryTicketGetsItsOwnUnguessableToken()
    {
        await IssueAsync([new OrderLineSummary(null, Guid.CreateVersion7(), 10)]);

        var tokens = CapturedTickets().Select(ticket => ticket.Token).ToList();
        tokens.Distinct().Count().ShouldBe(10);
        tokens.ShouldAllBe(token => token.Length == 32);
    }

    // OrderConfirmed is at-least-once, so a redelivery must not mint a second set of tickets.
    [Fact]
    public async Task AnOrderThatIsAlreadyTicketed_IsLeftAlone()
    {
        tickets.ExistsForOrderAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await IssueAsync([new OrderLineSummary(Guid.CreateVersion7(), null, 1)]);

        result.Issued.ShouldBeFalse();
        result.TicketCount.ShouldBe(0);
        tickets.DidNotReceive().AddRange(Arg.Any<IEnumerable<Ticket>>());
        events.DidNotReceive().Enqueue(Arg.Any<IntegrationEvent>());
        await tickets.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // One order-level event carrying every ticket, not one email per ticket — the buyer of four
    // general-admission tickets gets one message listing all four.
    [Fact]
    public async Task OneOrderLevelEventIsPublished_ListingEveryTicket()
    {
        await IssueAsync([new OrderLineSummary(null, Guid.CreateVersion7(), 4)], buyerEmail: "buyer@example.com");

        var published = events.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<OrderTicketsIssued>()
            .ToList();

        published.Count.ShouldBe(1);
        published[0].Tickets.Count.ShouldBe(4);
        published[0].BuyerEmail.ShouldBe("buyer@example.com");
    }

    // The per-ticket event stays alongside the order-level one; dropping it would silently break
    // any consumer that reacts to a single ticket.
    [Fact]
    public async Task APerTicketEventIsStillPublishedForEachTicket()
    {
        await IssueAsync([new OrderLineSummary(null, Guid.CreateVersion7(), 4)]);

        events.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<TicketIssued>()
            .Count()
            .ShouldBe(4);
    }

    // Nothing is persisted until the tickets and both kinds of event are in the same save, so a
    // crash mid-issue cannot leave tickets with no delivery event or the reverse.
    [Fact]
    public async Task TicketsAndTheirEventsAreCommittedTogether()
    {
        await IssueAsync([new OrderLineSummary(Guid.CreateVersion7(), null, 1)]);

        await tickets.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private IReadOnlyList<Ticket> CapturedTickets() =>
        tickets.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ITicketRepository.AddRange))
            .SelectMany(call => (IEnumerable<Ticket>)call.GetArguments()[0]!)
            .ToList();

    private async Task<IssueResult> IssueAsync(IReadOnlyList<OrderLineSummary> lines, string? buyerEmail = null) =>
        await new TicketIssuingService(tickets, events).IssueAsync(
            tenantId: Guid.CreateVersion7(),
            orderId: Guid.CreateVersion7(),
            catalogEventId: Guid.CreateVersion7(),
            userId: Guid.CreateVersion7(),
            lines,
            buyerEmail,
            CancellationToken.None);
}
