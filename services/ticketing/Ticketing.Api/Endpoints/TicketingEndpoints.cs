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
            .AllowAnonymous()
            .ExcludeFromDescription();

        // Dapr pub/sub: warm the local scan cache when Catalog publishes an event — the check-in
        // window and every gate-restricted seat/allocation, resolved once so ScanTicketAsync never
        // needs a live cross-service call (ADR-0025).
        app.MapPost("/integration/catalog/event-session-published", OnEventSessionPublishedAsync)
            .WithTopic("pubsub", nameof(EventSessionPublished))
            .WithName("OnEventSessionPublished")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // Buyer reads their own tickets; the selling tenant reads its own. Both are legitimate, so
        // the role check is only "authenticated" — the handlers decide which records by ownership,
        // and each carries the token that admits someone at a gate, so those checks are the real
        // control here (ADR-0035).
        app.MapGet("/v1/orders/{orderId:guid}/tickets", GetOrderTicketsAsync)
            .WithName("GetOrderTickets")
            .WithTags("Tickets")
            .RequireAuthenticatedCaller();

        app.MapGet("/v1/tickets/{id:guid}", GetTicketAsync)
            .WithName("GetTicket")
            .WithTags("Tickets")
            .RequireAuthenticatedCaller();

        app.MapGet("/v1/tickets/{id:guid}/qrcode", GetTicketQrCodeAsync)
            .WithName("GetTicketQrCode")
            .WithTags("Tickets")
            .RequireAuthenticatedCaller();

        // Organizer-only: the whole event's tickets, and admitting someone at the door.
        app.MapGet("/v1/sessions/{eventSessionId:guid}/tickets", GetSessionTicketsAsync)
            .WithName("GetSessionTickets")
            .WithTags("Tickets")
            .RequireOrganizer();

        app.MapPost("/v1/tickets/scan", ScanTicketAsync)
            .WithName("ScanTicket")
            .WithTags("Tickets")
            .RequireOrganizer();

        // Internal (Ordering's cancellation saga, via Dapr service invocation): void every ticket
        // for an order. AllowAnonymous deliberately — Ordering invokes it with no user token, and
        // it is not gateway-routed, so the only reachable caller is another service in the mesh.
        // Same treatment as Payments' intents/refund.
        app.MapPost("/v1/orders/{orderId:guid}/tickets/void", VoidOrderTicketsAsync)
            .WithName("VoidOrderTickets")
            .AllowAnonymous()
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
            @event.EventSessionId,
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

    private static async Task<IResult> OnEventSessionPublishedAsync(
        EventSessionPublished @event,
        SessionScanContextProvisioningService provisioning,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var provisioned = await provisioning.ProvisionAsync(
            @event.TenantId,
            @event.EventSessionId,
            @event.SeatMapId,
            @event.SeatMapVersionNumber,
            @event.DoorsOpenAt,
            @event.StartsAt,
            @event.EndsAt,
            cancellationToken);

        var logger = loggerFactory.CreateLogger("Ticketing.ScanContext");
        if (provisioned)
        {
            logger.LogInformation("Warmed scan cache for performance {EventSessionId}.", @event.EventSessionId);
        }
        else
        {
            logger.LogInformation("Performance {EventSessionId} scan cache already warmed; skipped.", @event.EventSessionId);
        }

        // Ack so Dapr does not redeliver; provisioning is idempotent if it does.
        return Results.Ok();
    }

    private static async Task<IResult> GetOrderTicketsAsync(
        Guid orderId,
        ClaimsPrincipal principal,
        ITenantContext tenant,
        ITicketRepository repository,
        CancellationToken cancellationToken)
    {
        var tickets = await repository.GetByOrderAsync(orderId, cancellationToken);
        if (tickets.Count == 0)
        {
            // An order with no tickets and an order that isn't yours look the same from here, which
            // is the intent — neither confirms anything about an order id the caller guessed.
            return Results.Ok(Array.Empty<TicketResponse>());
        }

        // Every ticket in an order shares its buyer and its selling tenant, so one check covers the
        // whole set. Same opaque not-found as GetTicketQrCode on a mismatch.
        var userId = GetUserId(principal);
        var isOwner = userId is not null && tickets[0].UserId == userId;
        var isOwningTenant = tenant.TenantId is not null && tickets[0].TenantId == tenant.TenantId;
        if (!isOwner && !isOwningTenant)
        {
            return Results.NotFound();
        }

        return Results.Ok(tickets.Select(Map).ToList());
    }

    private static async Task<IResult> GetTicketAsync(
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

        // TicketResponse carries the scan Token — the thing that admits someone at the gate — so
        // this needs the same ownership check GetTicketQrCode already had. The QR *image* was
        // protected while the endpoint returning the same token as text was not.
        var userId = GetUserId(principal);
        var isOwner = userId is not null && ticket.UserId == userId;
        var isOwningTenant = tenant.TenantId is not null && ticket.TenantId == tenant.TenantId;
        if (!isOwner && !isOwningTenant)
        {
            return Results.NotFound();
        }

        return Results.Ok(Map(ticket));
    }

    private static async Task<IResult> GetSessionTicketsAsync(
        Guid eventSessionId,
        ITenantContext tenant,
        ITicketRepository repository,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var tickets = await repository.GetBySessionAsync(tenant.TenantId.Value, eventSessionId, cancellationToken);
        var response = tickets.Select(Map).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> ScanTicketAsync(
        ScanTicketRequest request,
        ITenantContext tenant,
        ITicketRepository repository,
        ISessionScanContextRepository scanContexts,
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

        // Same 404 shape as an unknown token — a wrong-performance scan shouldn't reveal the token
        // is valid for some other night. Matched on the performance, not the event: a three-night
        // run's Friday ticket must not open Saturday's doors.
        if (ticket.EventSessionId != request.EventSessionId)
        {
            return Results.NotFound(new { message = "This ticket is not for the selected performance." });
        }

        // Every read below is local to Ticketing's own database — no cross-service call — because
        // the scan cache was already warmed once, at publish time (ADR-0025).
        var scanContext = await scanContexts.GetContextAsync(request.EventSessionId, cancellationToken);
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
        // `sub` only: AuthenticationExtensions turns MapInboundClaims off, so the claim keeps the
        // name the issuer gave it. The ClaimTypes.NameIdentifier fallback that used to sit here was
        // a workaround for that mapping, and kept working while the role policies silently did not.
        var value = principal.FindFirstValue(EventPlatformClaims.Subject);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static TicketResponse Map(Ticket ticket) =>
        new(
            ticket.Id,
            ticket.OrderId,
            ticket.CatalogEventId,
            ticket.EventSessionId,
            ticket.SeatId,
            ticket.GeneralAdmissionAllocationId,
            ticket.Token,
            ticket.Status.ToString(),
            ticket.IssuedAt,
            ticket.CheckedInAt);
}
