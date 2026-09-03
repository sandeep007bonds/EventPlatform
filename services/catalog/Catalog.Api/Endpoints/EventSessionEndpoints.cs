namespace Catalog.Api.Endpoints;

/// <summary>Maps the HTTP endpoints for an event's performances onto the application's use cases.</summary>
public static class EventSessionEndpoints
{
    /// <summary>Maps the <c>/v1/events/{eventId}/sessions</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapEventSessionEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events/{eventId:guid}/sessions").WithTags("Performances");

        // Anonymous, like the event itself: a buyer choosing which night to attend needs the list
        // before they have done anything. The handler applies the same Event.IsVisibleTo rule, so
        // a draft event's performances stay invisible.
        group.MapGet("/", ListAsync).WithName("ListEventSessions").AllowAnonymous();

        group.MapPost("/", AddAsync).WithName("AddEventSession").RequireOrganizer();
        group.MapPut("/{eventSessionId:guid}", UpdateAsync).WithName("UpdateEventSession").RequireOrganizer();
        group.MapDelete("/{eventSessionId:guid}", RemoveAsync).WithName("RemoveEventSession").RequireOrganizer();
        group.MapPut("/{eventSessionId:guid}/seat-map", AttachSeatMapAsync).WithName("AttachSessionSeatMap").RequireOrganizer();
        group.MapPut("/{eventSessionId:guid}/allocations", SetAllocationsAsync).WithName("SetSessionAllocations").RequireOrganizer();
        group.MapPost("/{eventSessionId:guid}/publish", PublishAsync).WithName("PublishEventSession").RequireOrganizer();
        group.MapPost("/{eventSessionId:guid}/cancel", CancelAsync).WithName("CancelEventSession").RequireOrganizer();
        group.MapPost("/{eventSessionId:guid}/pause-sales", PauseAsync).WithName("PauseSessionSales").RequireOrganizer();
        group.MapPost("/{eventSessionId:guid}/resume-sales", ResumeAsync).WithName("ResumeSessionSales").RequireOrganizer();

        return app;
    }

    private static async Task<IResult> ListAsync(
        Guid eventId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var sessions = await sender.Send(new ListEventSessionsQuery(eventId, tenant.TenantId), cancellationToken);
        return sessions is null ? Results.NotFound() : Results.Ok(sessions);
    }

    private static async Task<IResult> AddAsync(
        Guid eventId,
        EventSessionRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new AddEventSessionCommand(
            eventId,
            tenant.TenantId.Value,
            request.Name,
            request.StartsAt,
            request.EndsAt,
            request.DoorsOpenAt,
            request.BookingEndsAt);

        var result = await sender.Send(command, cancellationToken);

        return result.Outcome == SessionCommandOutcome.Succeeded && result.Session is { } session
            ? Results.Created($"/v1/events/{eventId}/sessions/{session.Id}", session)
            : ToResult(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid eventId,
        Guid eventSessionId,
        EventSessionRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateEventSessionCommand(
            eventId,
            eventSessionId,
            tenant.TenantId.Value,
            request.Name,
            request.StartsAt,
            request.EndsAt,
            request.DoorsOpenAt,
            request.BookingEndsAt);

        return ToResult(await sender.Send(command, cancellationToken));
    }

    private static async Task<IResult> RemoveAsync(
        Guid eventId,
        Guid eventSessionId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new RemoveEventSessionCommand(eventId, eventSessionId, tenant.TenantId.Value);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome == SessionCommandOutcome.Succeeded
            ? Results.NoContent()
            : ToResult(result);
    }

    private static async Task<IResult> AttachSeatMapAsync(
        Guid eventId,
        Guid eventSessionId,
        AttachSessionSeatMapRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new AttachSessionSeatMapCommand(
            eventId,
            eventSessionId,
            tenant.TenantId.Value,
            request.SeatMapId,
            request.VersionNumber);

        return ToResult(await sender.Send(command, cancellationToken));
    }

    private static async Task<IResult> SetAllocationsAsync(
        Guid eventId,
        Guid eventSessionId,
        SetSessionAllocationsRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new SetSessionAllocationsCommand(
            eventId,
            eventSessionId,
            tenant.TenantId.Value,
            (request.Allocations ?? []).Select(a => new SessionAllocationInput(a.Code, a.TicketTypeId)).ToList());

        return ToResult(await sender.Send(command, cancellationToken));
    }

    private static async Task<IResult> PublishAsync(
        Guid eventId,
        Guid eventSessionId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new PublishEventSessionCommand(eventId, eventSessionId, tenant.TenantId.Value);
        return ToResult(await sender.Send(command, cancellationToken));
    }

    private static async Task<IResult> CancelAsync(
        Guid eventId,
        Guid eventSessionId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CancelEventSessionCommand(eventId, eventSessionId, tenant.TenantId.Value);
        return ToResult(await sender.Send(command, cancellationToken));
    }

    private static Task<IResult> PauseAsync(
        Guid eventId,
        Guid eventSessionId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        ChangeSalesAsync(eventId, eventSessionId, pause: true, tenant, sender, cancellationToken);

    private static Task<IResult> ResumeAsync(
        Guid eventId,
        Guid eventSessionId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        ChangeSalesAsync(eventId, eventSessionId, pause: false, tenant, sender, cancellationToken);

    private static async Task<IResult> ChangeSalesAsync(
        Guid eventId,
        Guid eventSessionId,
        bool pause,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new ChangeSessionSalesCommand(eventId, eventSessionId, tenant.TenantId.Value, pause);
        return ToResult(await sender.Send(command, cancellationToken));
    }

    // One mapping for nine commands. They answer the same three questions, so nine copies of this
    // switch would only be nine chances for one of them to return the wrong status.
    private static IResult ToResult(SessionCommandResult result) => result.Outcome switch
    {
        SessionCommandOutcome.Succeeded => Results.Ok(result.Session),
        SessionCommandOutcome.NotFound => Results.NotFound(),
        SessionCommandOutcome.Refused => Results.Conflict(new { message = result.Message }),
        _ => Results.Problem("Unexpected performance-command outcome."),
    };
}
