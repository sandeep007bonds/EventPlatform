namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints for event groups (tours) onto the application's use cases.</summary>
public static class EventGroupEndpoints
{
    /// <summary>Maps the <c>/v1/event-groups</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapEventGroupEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/event-groups").WithTags("Event groups");

        group.MapPost("/", CreateEventGroupAsync).WithName("CreateEventGroup");
        group.MapGet("/", ListEventGroupsAsync).WithName("ListEventGroups");
        group.MapGet("/{id:guid}", GetEventGroupAsync).WithName("GetEventGroup").AllowAnonymous();

        return app;
    }

    private static async Task<IResult> CreateEventGroupAsync(
        CreateEventGroupRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateEventGroupCommand(tenant.TenantId.Value, request.Title);

        var id = await sender.Send(command, cancellationToken);
        return Results.Created($"/v1/event-groups/{id}", new { id });
    }

    private static async Task<IResult> ListEventGroupsAsync(
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

        var query = new ListEventGroupsQuery(tenant.TenantId.Value, page, pageSize);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEventGroupAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventGroupQuery(id), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
