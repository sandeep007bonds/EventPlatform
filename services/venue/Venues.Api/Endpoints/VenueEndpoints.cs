namespace Venues.Api.Endpoints;

/// <summary>Maps the Venue HTTP endpoints onto the application's use cases.</summary>
public static class VenueEndpoints
{
    /// <summary>Maps the <c>/v1/venues</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapVenueEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/venues").WithTags("Venues");

        // Every route here is organizer-only, including the reads. A venue is not public
        // information: which buildings an organizer is measuring up, and when, says more about an
        // unannounced tour than the tour does. Buyers never call this service — they see a seat map
        // through the event that points at it.
        group.MapPost("/", CreateVenueAsync).WithName("CreateVenue").RequireOrganizer();
        group.MapGet("/", ListVenuesAsync).WithName("ListVenues").RequireOrganizer();
        group.MapGet("/{venueId:guid}", GetVenueAsync).WithName("GetVenue").RequireOrganizer();
        group.MapPut("/{venueId:guid}", UpdateVenueAsync).WithName("UpdateVenue").RequireOrganizer();
        group.MapPost("/{venueId:guid}/activate", ActivateVenueAsync).WithName("ActivateVenue").RequireOrganizer();
        group.MapPost("/{venueId:guid}/archive", ArchiveVenueAsync).WithName("ArchiveVenue").RequireOrganizer();
        group.MapPost("/{venueId:guid}/gates", AddGateAsync).WithName("AddVenueGate").RequireOrganizer();
        group.MapPost("/{venueId:guid}/facilities", AddFacilityAsync).WithName("AddVenueFacility").RequireOrganizer();

        return app;
    }

    private static async Task<IResult> CreateVenueAsync(
        CreateVenueRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateVenueCommand(
            tenant.TenantId.Value,
            request.Name,
            request.VenueType,
            request.Address,
            request.TimeZoneId);

        var venue = await sender.Send(command, cancellationToken);

        return Results.Created($"/v1/venues/{venue.Id}", venue);
    }

    private static async Task<IResult> ListVenuesAsync(
        bool? includeArchived,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var query = new ListVenuesQuery(tenant.TenantId.Value, includeArchived ?? false);

        return Results.Ok(await sender.Send(query, cancellationToken));
    }

    private static async Task<IResult> GetVenueAsync(
        Guid venueId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var venue = await sender.Send(new GetVenueQuery(venueId, tenant.TenantId), cancellationToken);

        return venue is null ? Results.NotFound() : Results.Ok(venue);
    }

    private static async Task<IResult> UpdateVenueAsync(
        Guid venueId,
        UpdateVenueRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateVenueCommand(
            venueId,
            tenant.TenantId.Value,
            request.Name,
            request.VenueType,
            request.Address,
            request.TimeZoneId);

        var venue = await sender.Send(command, cancellationToken);

        return venue is null ? Results.NotFound() : Results.Ok(venue);
    }

    private static Task<IResult> ActivateVenueAsync(
        Guid venueId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(venueId, archive: false, tenant, sender, cancellationToken);

    private static Task<IResult> ArchiveVenueAsync(
        Guid venueId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(venueId, archive: true, tenant, sender, cancellationToken);

    private static async Task<IResult> ChangeStatusAsync(
        Guid venueId,
        bool archive,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new ChangeVenueStatusCommand(venueId, tenant.TenantId.Value, archive);
        var outcome = await sender.Send(command, cancellationToken);

        return outcome switch
        {
            ChangeVenueStatusOutcome.Changed => Results.NoContent(),
            ChangeVenueStatusOutcome.NotFound => Results.NotFound(),
            ChangeVenueStatusOutcome.AlreadyArchived => Results.Conflict(
                "An archived venue cannot be reactivated; create a new one."),
            _ => Results.Problem("Unexpected change-venue-status outcome."),
        };
    }

    private static async Task<IResult> AddGateAsync(
        Guid venueId,
        AddVenueGateRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new AddVenueGateCommand(venueId, tenant.TenantId.Value, request.Code, request.Name);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            AddVenueGateOutcome.Added =>
                Results.Created($"/v1/venues/{venueId}/gates/{result.GateId}", new { id = result.GateId }),
            AddVenueGateOutcome.VenueNotFound => Results.NotFound(),
            AddVenueGateOutcome.DuplicateCode => Results.Conflict(
                $"Gate code '{request.Code}' is already used at this venue."),
            _ => Results.Problem("Unexpected add-gate outcome."),
        };
    }

    private static async Task<IResult> AddFacilityAsync(
        Guid venueId,
        AddVenueFacilityRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new AddVenueFacilityCommand(
            venueId,
            tenant.TenantId.Value,
            request.Name,
            request.Description);

        var facilityId = await sender.Send(command, cancellationToken);

        return facilityId is null
            ? Results.NotFound()
            : Results.Created($"/v1/venues/{venueId}/facilities/{facilityId}", new { id = facilityId });
    }
}
