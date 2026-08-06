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

        // Dapr pub/sub: warm the local scan cache when Catalog publishes an event — the check-in
        // window and every gate-restricted seat/allocation, resolved once so ScanTicketAsync never
        // needs a live cross-service call (ADR-0025).
        app.MapPost("/integration/catalog/event-published", OnEventPublishedAsync)
            .WithTopic("pubsub", nameof(EventPublished))
            .WithName("OnEventPublished")
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

        app.MapGet("/v1/tickets/{id:guid}/qrcode", GetTicketQrCodeAsync)
            .WithName("GetTicketQrCode")
            .WithTags("Tickets");

        // Internal (Ordering's cancellation saga, via Dapr service invocation): void every ticket
        // for an order — not gateway-routed, same treatment as Payments' charge/refund.
        app.MapPost("/v1/orders/{orderId:guid}/tickets/void", VoidOrderTicketsAsync)
            .WithName("VoidOrderTickets")
            .ExcludeFromDescription();

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

    private static async Task<IResult> OnEventPublishedAsync(
        EventPublished @event,
        EventScanContextProvisioningService provisioning,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var provisioned = await provisioning.ProvisionAsync(
            @event.TenantId,
            @event.CatalogEventId,
            @event.DoorsOpenAt,
            @event.StartsAt,
            @event.EndsAt,
            cancellationToken);

        var logger = loggerFactory.CreateLogger("Ticketing.ScanContext");
        if (provisioned)
        {
            logger.LogInformation("Warmed scan cache for event {EventId}.", @event.CatalogEventId);
        }
        else
        {
            logger.LogInformation("Event {EventId} scan cache already warmed; skipped.", @event.CatalogEventId);
        }

        // Ack so Dapr does not redeliver; provisioning is idempotent if it does.
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
        IEventScanContextRepository scanContexts,
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

        // Every read below is local to Ticketing's own database — no cross-service call — because
        // the scan cache was already warmed once, at publish time (ADR-0025).
        var scanContext = await scanContexts.GetContextAsync(request.EventId, cancellationToken);
        if (scanContext is not null && !scanContext.IsWithinCheckInWindow(DateTimeOffset.UtcNow))
        {
            return Results.Conflict(new { message = "Outside the event's check-in window." });
        }

        var resolvedGateId = ticket.SeatId is { } seatId
            ? await scanContexts.GetGateForSeatAsync(seatId, cancellationToken)
            : await scanContexts.GetGateForGaAllocationAsync(ticket.GeneralAdmissionAllocationId!.Value, cancellationToken);

        if (resolvedGateId is not null && request.GateId is not null && resolvedGateId != request.GateId)
        {
            return Results.Conflict(new { message = "This ticket must enter through a different gate." });
        }

        // A clearer message than the domain exception's raw "Ticket {id} is Void, not Issued." for
        // the specific, common case of a cancelled/refunded order — the gate scanner shouldn't see
        // an internal-sounding message for what is, to them, just "this ticket isn't valid."
        if (ticket.Status == TicketStatus.Void)
        {
            return Results.Conflict(new { message = "This ticket was cancelled and is no longer valid." });
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

    private static async Task<IResult> VoidOrderTicketsAsync(
        Guid orderId,
        TicketVoidingService voiding,
        CancellationToken cancellationToken)
    {
        var outcome = await voiding.VoidByOrderAsync(orderId, cancellationToken);
        return outcome switch
        {
            VoidTicketsOutcome.Voided => Results.NoContent(),
            VoidTicketsOutcome.NoTickets => Results.NotFound(),
            VoidTicketsOutcome.AlreadyCheckedIn =>
                Results.Conflict(new { message = "One or more tickets for this order have already been checked in." }),
            _ => Results.Problem("Unexpected void outcome."),
        };
    }

    private static async Task<IResult> GetTicketQrCodeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ITenantContext tenant,
        ITicketRepository repository,
        CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        // Opaque not-found on a mismatch — same "never reveal existence" pattern used elsewhere
        // (e.g. DefineSeatMap) — rather than a 403 that confirms the ticket exists.
        var userId = GetUserId(principal);
        var isOwner = userId is not null && ticket.UserId == userId;
        var isOwningTenant = tenant.TenantId is not null && ticket.TenantId == tenant.TenantId;
        if (!isOwner && !isOwningTenant)
        {
            return Results.NotFound();
        }

        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(ticket.Token, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(qrData).GetGraphic(20);

        return Results.File(png, "image/png");
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
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
