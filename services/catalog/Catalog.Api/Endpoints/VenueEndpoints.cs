namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints for venues onto the application's use cases.</summary>
public static class VenueEndpoints
{
    /// <summary>Maps the <c>/v1/venues</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapVenueEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/venues").WithTags("Venues");

        group.MapPost("/", CreateVenueAsync).WithName("CreateVenue");
        group.MapGet("/", ListVenuesAsync).WithName("ListVenues");
        group.MapGet("/{id:guid}", GetVenueAsync).WithName("GetVenue").AllowAnonymous();
        group.MapPut("/{id:guid}", UpdateVenueAsync).WithName("UpdateVenue");

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
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.Capacity);

        var id = await sender.Send(command, cancellationToken);
        return Results.Created($"/v1/venues/{id}", new { id });
    }

    private static async Task<IResult> ListVenuesAsync(
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new ListVenuesQuery(tenant.TenantId.Value, page, pageSize), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetVenueAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVenueQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateVenueAsync(
        Guid id,
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
            id,
            tenant.TenantId.Value,
            request.Name,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.Capacity);

        var outcome = await sender.Send(command, cancellationToken);
        return outcome switch
        {
            UpdateVenueOutcome.Updated => Results.NoContent(),
            UpdateVenueOutcome.NotFound => Results.NotFound(),
            _ => Results.Problem("Unexpected update-venue outcome."),
        };
    }
}
