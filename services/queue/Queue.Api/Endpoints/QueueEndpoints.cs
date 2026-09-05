namespace Queue.Api.Endpoints;

/// <summary>Maps the Queue service's HTTP endpoints and its <c>EventPublished</c> subscription.</summary>
public static class QueueEndpoints
{
    /// <summary>
    /// Where this service's undeliverable messages go — one topic per service, not per subscription
    /// (see <c>SubscribesTo</c>), so there is one drain rather than one per topic.
    /// </summary>
    private const string DeadLetterTopic = "deadletter-queue";

    /// <summary>Maps the <c>/v1/events/{eventId}/queue/*</c> endpoints and the Dapr subscription.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapQueueEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events/{eventId:guid}/queue").WithTags("Queue");

        // Anonymous — joining/polling the waiting room needs no login (ADR-0016's "browsing/queueing
        // is anonymous, the identity gate is at hold time" posture extends here unchanged).
        group.MapPost("/join", JoinAsync).WithName("JoinQueue").AllowAnonymous();
        group.MapGet("/status", StatusAsync).WithName("QueueStatus").AllowAnonymous();

        // Pacing is an organizer control, tunable while the event is live.
        group.MapGet("/settings", GetSettingsAsync).WithName("GetQueueSettings").RequireOrganizer();
        group.MapPut("/settings", UpdateSettingsAsync).WithName("UpdateQueueSettings").RequireOrganizer();

        // Dapr pub/sub: provision this event's queue settings when Catalog publishes it.
        // AllowAnonymous deliberately: the Dapr sidecar delivers with no user token, and a
        // denied subscriber fails by going quiet rather than erroring.
        app.MapPost("/integration/catalog/event-published", OnEventPublishedAsync)
            .SubscribesTo(nameof(EventPublished), DeadLetterTopic)
            .WithName("OnEventPublished")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // The other half of a dead-letter topic. A topic nobody reads is just a quieter silence
        // than an infinite retry loop, so this records what could not be handled and says so
        // loudly. AllowAnonymous for the same reason as every subscriber: the sidecar delivers
        // with no user token.
        app.MapPost("/integration/dead-letter", OnDeadLetterAsync)
            .DrainsDeadLetters(DeadLetterTopic)
            .WithName("OnDeadLetterQueue")
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

    private static async Task<IResult> JoinAsync(
        Guid eventId,
        JoinQueueRequest request,
        JoinQueueHandler handler,
        IJoinRateLimiter rateLimiter,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Budget is charged per session *created*, not per request — see IJoinRateLimiter. So the
        // check happens up front (cheap, read-only) and the charge only after the store confirms a
        // new session was actually minted.
        var clientKey = ClientKey(httpContext);
        var decision = await rateLimiter.CheckAsync(eventId, clientKey, cancellationToken);
        if (!decision.Allowed)
        {
            httpContext.Response.Headers.RetryAfter =
                decision.RetryAfterSeconds?.ToString(CultureInfo.InvariantCulture);

            return Results.Json(
                new { error = "too_many_joins", retryAfterSeconds = decision.RetryAfterSeconds },
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var sessionId = request.SessionId ?? Guid.CreateVersion7();
        var result = await handler.HandleAsync(eventId, sessionId, cancellationToken);

        if (result.CreatedNewSession)
        {
            await rateLimiter.RecordCreatedSessionAsync(eventId, clientKey, cancellationToken);
        }

        return Results.Ok(new QueueSessionResponse(sessionId, result.Admitted, result.AdmissionToken, result.Position, result.EstimatedWaitSeconds));
    }

    // The caller's address, once ForwardedHeaders has replaced the gateway's with the real client's
    // (see Program.cs). "unknown" only when there is no remote address at all — every such caller
    // then shares one bucket, which is the safe direction: it cannot be used to evade the limit.
    private static string ClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async Task<IResult> StatusAsync(
        Guid eventId,
        Guid sessionId,
        QueueStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(eventId, sessionId, cancellationToken);
        return Results.Ok(new QueueSessionResponse(sessionId, result.Admitted, result.AdmissionToken, result.Position, result.EstimatedWaitSeconds));
    }

    private static async Task<IResult> GetSettingsAsync(
        Guid eventId,
        ITenantContext tenant,
        IQueueSettingsRepository repository,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var settings = await repository.GetForTenantAsync(eventId, tenant.TenantId.Value, cancellationToken);
        if (settings is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(new QueueSettingsResponse(
            settings.EventId, settings.Enabled, settings.AdmissionRatePerInterval, settings.IntervalSeconds, settings.SessionTtlSeconds));
    }

    private static async Task<IResult> UpdateSettingsAsync(
        Guid eventId,
        UpdateQueueSettingsRequest request,
        ITenantContext tenant,
        IQueueSettingsRepository repository,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var settings = await repository.GetForTenantAsync(eventId, tenant.TenantId.Value, cancellationToken);
        if (settings is null)
        {
            return Results.NotFound();
        }

        try
        {
            settings.UpdateTuning(request.AdmissionRatePerInterval, request.IntervalSeconds, request.SessionTtlSeconds);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }

        await repository.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> OnEventPublishedAsync(
        EventPublished @event,
        QueueSettingsProvisioningService provisioning,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var provisioned = await provisioning.ProvisionAsync(
            @event.CatalogEventId, @event.TenantId, @event.RequiresQueue, cancellationToken);

        var logger = loggerFactory.CreateLogger("Queue.Api.Endpoints.QueueEndpoints");
        logger.LogInformation(
            "Queue settings {Action} for event {EventId} (RequiresQueue={RequiresQueue}).",
            provisioned ? "provisioned" : "already provisioned",
            @event.CatalogEventId,
            @event.RequiresQueue);

        return Results.Ok();
    }
}
