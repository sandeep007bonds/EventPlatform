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
        group.MapGet("/{id:guid}", GetEventAsync).WithName("GetEvent");
        group.MapPost("/{id:guid}/publish", PublishEventAsync).WithName("PublishEvent");

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

    private static async Task<IResult> GetEventAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> PublishEventAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var published = await sender.Send(new PublishEventCommand(id), cancellationToken);
        return published ? Results.NoContent() : Results.NotFound();
    }
}
