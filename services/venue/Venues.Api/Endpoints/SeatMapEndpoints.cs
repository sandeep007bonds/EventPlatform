namespace Venues.Api.Endpoints;

/// <summary>Maps the seat-map HTTP endpoints onto the application's use cases.</summary>
public static class SeatMapEndpoints
{
    /// <summary>
    /// Maps the <c>/v1/venues/{venueId}/seat-maps</c> and <c>/v1/seat-maps/{seatMapId}</c> endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapSeatMapEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var venueScoped = app.MapGroup("/v1/venues/{venueId:guid}/seat-maps").WithTags("Seat maps");

        venueScoped.MapPost("/", CreateSeatMapAsync).WithName("CreateSeatMap").RequireOrganizer();
        venueScoped.MapGet("/", ListSeatMapsAsync).WithName("ListSeatMaps").RequireOrganizer();

        // Addressed by their own id once created: a map outlives the conversation that created it
        // and is referenced by events that do not know or care which venue route it arrived on.
        var map = app.MapGroup("/v1/seat-maps/{seatMapId:guid}").WithTags("Seat maps");

        // Anonymous: a buyer has to render the plan to choose a seat, and a ticket sold under an
        // older version has to keep resolving. The handler still refuses a *draft* to anyone but
        // the tenant that owns it.
        map.MapGet("/", GetSeatMapAsync).WithName("GetSeatMap").AllowAnonymous();

        map.MapPost("/versions", StartDraftAsync).WithName("StartSeatMapDraft").RequireOrganizer();
        map.MapPut("/draft/layout", SaveLayoutAsync).WithName("SaveSeatMapLayout").RequireOrganizer();
        map.MapPost("/publish", PublishSeatMapAsync).WithName("PublishSeatMap").RequireOrganizer();

        return app;
    }

    private static async Task<IResult> CreateSeatMapAsync(
        Guid venueId,
        CreateSeatMapRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateSeatMapCommand(venueId, tenant.TenantId.Value, request.Name);
        var seatMap = await sender.Send(command, cancellationToken);

        return seatMap is null
            ? Results.NotFound()
            : Results.Created($"/v1/seat-maps/{seatMap.Id}", seatMap);
    }

    private static async Task<IResult> ListSeatMapsAsync(
        Guid venueId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var query = new ListSeatMapsQuery(venueId, tenant.TenantId.Value);

        return Results.Ok(await sender.Send(query, cancellationToken));
    }

    private static async Task<IResult> GetSeatMapAsync(
        Guid seatMapId,
        int? version,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetSeatMapQuery(seatMapId, version, tenant.TenantId);
        var seatMap = await sender.Send(query, cancellationToken);

        return seatMap is null ? Results.NotFound() : Results.Ok(seatMap);
    }

    private static async Task<IResult> StartDraftAsync(
        Guid seatMapId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new StartSeatMapDraftCommand(seatMapId, tenant.TenantId.Value);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            StartSeatMapDraftOutcome.Started => Results.Ok(result.SeatMap),
            StartSeatMapDraftOutcome.NotFound => Results.NotFound(),
            StartSeatMapDraftOutcome.DraftAlreadyOpen => Results.Conflict(
                "This map already has an open draft. Publish or edit that one."),
            _ => Results.Problem("Unexpected start-draft outcome."),
        };
    }

    private static async Task<IResult> SaveLayoutAsync(
        Guid seatMapId,
        SaveSeatMapLayoutRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var parsed = SeatMapLayoutParser.Parse(request);
        if (parsed.Layout is null)
        {
            return Results.BadRequest(parsed.Error);
        }

        var command = new SaveSeatMapLayoutCommand(seatMapId, tenant.TenantId.Value, parsed.Layout);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            SaveSeatMapLayoutOutcome.Saved => Results.Ok(result.SeatMap),
            SaveSeatMapLayoutOutcome.NotFound => Results.NotFound(),
            SaveSeatMapLayoutOutcome.NoOpenDraft => Results.Conflict(result.Message),
            SaveSeatMapLayoutOutcome.InvalidLayout => Results.BadRequest(result.Message),
            SaveSeatMapLayoutOutcome.UnknownGate => Results.BadRequest(result.Message),
            _ => Results.Problem("Unexpected save-layout outcome."),
        };
    }

    private static async Task<IResult> PublishSeatMapAsync(
        Guid seatMapId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new PublishSeatMapCommand(seatMapId, tenant.TenantId.Value);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            PublishSeatMapOutcome.Published => Results.Ok(new
            {
                versionNumber = result.VersionNumber,
                capacity = result.Capacity,
            }),
            PublishSeatMapOutcome.NotFound => Results.NotFound(),
            PublishSeatMapOutcome.NoOpenDraft => Results.Conflict("This map has no open draft to publish."),

            // 409 with the whole list, not the first failure: the person fixing a stadium plan needs
            // every problem at once, and a 400 would suggest the request itself was malformed.
            PublishSeatMapOutcome.Invalid => Results.Conflict(new { errors = result.Errors }),
            _ => Results.Problem("Unexpected publish-seat-map outcome."),
        };
    }
}
