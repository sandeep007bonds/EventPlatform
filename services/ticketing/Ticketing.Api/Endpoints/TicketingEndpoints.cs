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

        app.MapGet("/v1/events/{eventId:guid}/tickets", GetEventTicketsAsync)
            .WithName("GetEventTickets")
            .WithTags("Tickets");

        app.MapPost("/v1/tickets/scan", ScanTicketAsync)
            .WithName("ScanTicket")
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
            @event.BuyerEmail,
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

    private static async Task<IResult> GetEventTicketsAsync(
        Guid eventId,
        ITenantContext tenant,
        ITicketRepository repository,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var tickets = await repository.GetByEventAsync(tenant.TenantId.Value, eventId, cancellationToken);
        var response = tickets.Select(Map).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> ScanTicketAsync(
        ScanTicketRequest request,
        ITenantContext tenant,
        ITicketRepository repository,
        ICatalogEventClient catalogEventClient,
        IInventoryGaClient inventoryGaClient,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var ticket = await repository.GetByTokenAsync(request.Token, cancellationToken);
        if (ticket is null || ticket.TenantId != tenant.TenantId)
        {
            return Results.NotFound(new { message = "No ticket matches that token." });
        }

        // Same 404 shape as an unknown token — a wrong-event scan shouldn't reveal the token is
        // valid for some other event.
        if (ticket.CatalogEventId != request.EventId)
        {
            return Results.NotFound(new { message = "This ticket is not for the selected event." });
        }

        var scanContext = await catalogEventClient.GetScanContextAsync(request.EventId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var opensAt = scanContext.DoorsOpenAt ?? scanContext.StartsAt;
        if (now < opensAt || now > scanContext.EndsAt)
        {
            return Results.Conflict(new { message = "Outside the event's check-in window." });
        }

        Guid? resolvedGateId;
        if (ticket.SeatId is { } seatId)
        {
            scanContext.EntryGateIdBySeatId.TryGetValue(seatId, out resolvedGateId);
        }
        else
        {
            var catalogSectionId = await inventoryGaClient.GetCatalogSectionIdAsync(
                request.EventId, ticket.GeneralAdmissionAllocationId!.Value, cancellationToken);
            resolvedGateId = catalogSectionId is { } sectionId && scanContext.EntryGateIdByCatalogSectionId.TryGetValue(sectionId, out var gateId)
                ? gateId
                : null;
        }

        if (resolvedGateId is not null && request.GateId is not null && resolvedGateId != request.GateId)
        {
            return Results.Conflict(new { message = "This ticket must enter through a different gate." });
        }

        try
        {
            ticket.CheckIn();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Results.Ok(Map(ticket));
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
            ticket.IssuedAt,
            ticket.CheckedInAt);
}
