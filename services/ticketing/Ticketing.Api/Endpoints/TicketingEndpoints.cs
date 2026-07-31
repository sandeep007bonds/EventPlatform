namespace Ticketing.Api.Endpoints;

/// <summary>Maps the Ticketing HTTP endpoints, including the Dapr pub/sub subscription.</summary>
public static class TicketingEndpoints
{
    /// <summary>Maps the Ticketing endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapTicketingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Dapr pub/sub: issue tickets when Ordering confirms an order.
        app.MapPost("/integration/ordering/order-confirmed", OnOrderConfirmedAsync)
            .WithTopic("pubsub", nameof(OrderConfirmed))
            .WithName("OnOrderConfirmed")
            .ExcludeFromDescription();

        app.MapGet("/v1/orders/{orderId:guid}/tickets", GetOrderTicketsAsync)
            .WithName("GetOrderTickets")
            .WithTags("Tickets");

        app.MapGet("/v1/tickets/{id:guid}", GetTicketAsync)
            .WithName("GetTicket")
            .WithTags("Tickets");

        return app;
    }

    private static async Task<IResult> OnOrderConfirmedAsync(
        OrderConfirmed @event,
        TicketIssuingService issuing,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var result = await issuing.IssueAsync(
            @event.TenantId,
            @event.OrderId,
            @event.CatalogEventId,
            @event.UserId,
            @event.Lines,
            cancellationToken);

        var logger = loggerFactory.CreateLogger("Ticketing.Issuing");
        if (result.Issued)
        {
            logger.LogInformation("Issued {TicketCount} ticket(s) for order {OrderId}.", result.TicketCount, @event.OrderId);
        }
        else
        {
            logger.LogInformation("Order {OrderId} already ticketed; skipped.", @event.OrderId);
        }

        // Ack so Dapr does not redeliver; issuance is idempotent if it does.
        return Results.Ok();
    }

    private static async Task<IResult> GetOrderTicketsAsync(
        Guid orderId,
        ITicketRepository repository,
        CancellationToken cancellationToken)
    {
        var tickets = await repository.GetByOrderAsync(orderId, cancellationToken);
        var response = tickets.Select(Map).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetTicketAsync(
        Guid id,
        ITicketRepository repository,
        CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(id, cancellationToken);
        return ticket is null ? Results.NotFound() : Results.Ok(Map(ticket));
    }

    private static TicketResponse Map(Ticket ticket) =>
        new(
            ticket.Id,
            ticket.OrderId,
            ticket.CatalogEventId,
            ticket.SeatId,
            ticket.GeneralAdmissionAllocationId,
            ticket.Token,
            ticket.Status.ToString(),
            ticket.IssuedAt);
}
