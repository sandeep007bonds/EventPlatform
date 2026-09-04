namespace Communication.Api.Endpoints;

/// <summary>Maps the Communication HTTP endpoints, including the Dapr pub/sub subscriptions.</summary>
public static class NotificationsEndpoints
{
    /// <summary>
    /// Where this service's undeliverable messages go — one topic per service, not per subscription
    /// (see <c>SubscribesTo</c>), so there is one drain rather than one per topic.
    /// </summary>
    private const string DeadLetterTopic = "deadletter-communication";

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
            .SubscribesTo(nameof(OrderConfirmed), DeadLetterTopic)
            .WithName("OnOrderConfirmed")
            .AllowAnonymous()
            .ExcludeFromDescription();

        app.MapPost("/integration/ticketing/ticket-issued", OnTicketIssuedAsync)
            .SubscribesTo(nameof(TicketIssued), DeadLetterTopic)
            .WithName("OnTicketIssued")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // One combined ticket-delivery email per order, sent directly (the buyer email arrived on
        // the event) — see IntegrationEventNotificationHandler.HandleOrderTicketsIssuedAsync.
        app.MapPost("/integration/ticketing/order-tickets-issued", OnOrderTicketsIssuedAsync)
            .SubscribesTo(nameof(OrderTicketsIssued), DeadLetterTopic)
            .WithName("OnOrderTicketsIssued")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // The other half of a dead-letter topic. A topic nobody reads is just a quieter silence
        // than an infinite retry loop, so this records what could not be handled and says so
        // loudly. AllowAnonymous for the same reason as every subscriber: the sidecar delivers
        // with no user token.
        app.MapPost("/integration/dead-letter", OnDeadLetterAsync)
            .DrainsDeadLetters(DeadLetterTopic)
            .WithName("OnDeadLetterCommunication")
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> OnDeadLetterAsync(
        JsonNode? body,
        DeadLetterDrain drain,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        // Best-effort only. Dapr's delivery headers for a dead letter are not something to depend
        // on, so this is a hint; the envelope's own EventType is the topic the relay published to
        // and is what the drain actually falls back on.
        var topic = http.Request.Headers["Ce-Topic"].FirstOrDefault()
            ?? http.Request.Headers["topic"].FirstOrDefault();

        await drain.RecordAsync(topic, body, cancellationToken);

        // 200 regardless. A dead letter that fails to record would be retried and then dead-lettered
        // again, and there is nowhere further to send it — the log and the alert are the escalation.
        return Results.Ok();
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
            request.CausationId);

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
