namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints onto the application's use cases.</summary>
public static class CatalogEndpoints
{
    /// <summary>Maps the <c>/v1/events</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events").WithTags("Events");

        group.MapPost("/", CreateEventAsync).WithName("CreateEvent");
        group.MapGet("/", ListEventsAsync).WithName("ListEvents").AllowAnonymous();
        group.MapGet("/{id:guid}", GetEventAsync).WithName("GetEvent").AllowAnonymous();
        group.MapPost("/{id:guid}/publish", PublishEventAsync).WithName("PublishEvent");
        group.MapPost("/{id:guid}/seatmap", DefineSeatMapAsync).WithName("DefineSeatMap");
        group.MapGet("/{id:guid}/seatmap", GetSeatMapAsync).WithName("GetSeatMap").AllowAnonymous();

        return app;
    }

    private static async Task<IResult> CreateEventAsync(
        CreateEventRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateEventCommand(
            tenant.TenantId.Value,
            request.VenueId,
            request.Title,
            request.StartsAt,
            request.Currency);

        var id = await sender.Send(command, cancellationToken);
        return Results.Created($"/v1/events/{id}", new { id });
    }

    private static async Task<IResult> ListEventsAsync(
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken,
        EventStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new ListEventsQuery(tenant.TenantId, status, page, pageSize), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEventAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventQuery(id, tenant.TenantId), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> PublishEventAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var outcome = await sender.Send(new PublishEventCommand(id), cancellationToken);
        return outcome switch
        {
            PublishEventOutcome.Published => Results.NoContent(),
            PublishEventOutcome.NotFound => Results.NotFound(),
            PublishEventOutcome.NoSeatMap => Results.Conflict(new { message = "Define a seat map before publishing the event." }),
            PublishEventOutcome.NotDraft => Results.Conflict(new { message = "Only a draft event can be published." }),
            _ => Results.Problem("Unexpected publish outcome."),
        };
    }

    private static async Task<IResult> DefineSeatMapAsync(
        Guid id,
        DefineSeatMapRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var sections = request.Sections
            .Select(s => new SeatMapSectionInput(s.Name, s.PriceTier, s.PriceAmount, s.Rows, s.SeatsPerRow))
            .ToList();

        var command = new DefineSeatMapCommand(id, tenant.TenantId.Value, request.Name, sections);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            DefineSeatMapOutcome.Created =>
                Results.Created($"/v1/events/{id}/seatmap", new { seatMapId = result.SeatMapId }),
            DefineSeatMapOutcome.EventNotFound => Results.NotFound(),
            DefineSeatMapOutcome.EventNotDraft =>
                Results.Conflict(new { message = "The event is not a draft; its seat map can no longer be changed." }),
            DefineSeatMapOutcome.AlreadyDefined =>
                Results.Conflict(new { message = "A seat map already exists for this event." }),
            _ => Results.Problem("Unexpected seat-map outcome."),
        };
    }

    private static async Task<IResult> GetSeatMapAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSeatMapQuery(id, tenant.TenantId), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
