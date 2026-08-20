namespace Communication.Api.Endpoints;

/// <summary>Maps the Communication HTTP endpoints, including the Dapr pub/sub subscriptions.</summary>
public static class NotificationsEndpoints
{
    /// <summary>Maps the Communication endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Internal, service-to-service only (Dapr invocation) — never gateway-routed.
        // AllowAnonymous deliberately: Identity invokes this to deliver OTPs with no user token,
        // by definition — the recipient is not logged in yet.
        app.MapPost("/v1/notifications/send", SendAsync)
            .WithName("SendNotification")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // Dapr pub/sub: wired for redelivery-safety, but real delivery is deferred — see
        // IntegrationEventNotificationHandler and services/communication/CLAUDE.md.
        app.MapPost("/integration/ordering/order-confirmed", OnOrderConfirmedAsync)
            .WithTopic("pubsub", nameof(OrderConfirmed))
            .WithName("OnOrderConfirmed")
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapPost("/integration/ticketing/ticket-issued", OnTicketIssuedAsync)
            .WithTopic("pubsub", nameof(TicketIssued))
            .WithName("OnTicketIssued")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // One combined ticket-delivery email per order, sent directly (the buyer email arrived on
        // the event) — see IntegrationEventNotificationHandler.HandleOrderTicketsIssuedAsync.
        app.MapPost("/integration/ticketing/order-tickets-issued", OnOrderTicketsIssuedAsync)
            .WithTopic("pubsub", nameof(OrderTicketsIssued))
            .WithName("OnOrderTicketsIssued")
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> SendAsync(
        SendNotificationRequest request,
        NotificationSendService sendService,
        CancellationToken cancellationToken)
    {
        var command = new SendNotificationCommand(
            request.TenantId,
            request.Channel,
            request.Recipient,
            request.TemplateKey,
            request.Placeholders,
            request.Body,
            request.CorrelationId);

        var errors = NotificationRequestValidator.Validate(command);
        if (errors.Count > 0)
        {
            return Results.BadRequest(new { errors });
        }

        var result = await sendService.SendAsync(command, cancellationToken);

        var response = new SendNotificationResponse(
            result.Succeeded,
            result.DeliveryLogId,
            result.Provider,
            result.ProviderReference,
            result.FailureReason);

        return Results.Ok(response);
    }

    private static async Task<IResult> OnOrderConfirmedAsync(
        OrderConfirmed @event,
        IntegrationEventNotificationHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleOrderConfirmedAsync(@event, cancellationToken);

        // Ack so Dapr does not redeliver; handling is idempotent if it does.
        return Results.Ok();
    }

    private static async Task<IResult> OnTicketIssuedAsync(
        TicketIssued @event,
        IntegrationEventNotificationHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleTicketIssuedAsync(@event, cancellationToken);

        return Results.Ok();
    }

    private static async Task<IResult> OnOrderTicketsIssuedAsync(
        OrderTicketsIssued @event,
        IntegrationEventNotificationHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleOrderTicketsIssuedAsync(@event, cancellationToken);

        return Results.Ok();
    }
}
