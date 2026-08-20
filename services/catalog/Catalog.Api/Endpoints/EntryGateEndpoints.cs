namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints for an event's entry gates onto the application's use cases.</summary>
public static class EntryGateEndpoints
{
    /// <summary>Maps the <c>/v1/events/{eventId}/entry-gates</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapEntryGateEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events/{eventId:guid}/entry-gates").WithTags("Entry gates");

        group.MapPost("/", CreateEntryGateAsync).WithName("CreateEntryGate").RequireOrganizer();

        // Anonymous — Ticketing resolves gate names/restrictions live at scan time via Dapr
        // service invocation, and a gate name alone reveals nothing sensitive.
        group.MapGet("/", ListEntryGatesAsync).WithName("ListEntryGates").AllowAnonymous();

        return app;
    }

    private static async Task<IResult> CreateEntryGateAsync(
        Guid eventId,
        CreateEntryGateRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateEntryGateCommand(eventId, tenant.TenantId.Value, request.Name);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            CreateEntryGateOutcome.Created =>
                Results.Created($"/v1/events/{eventId}/entry-gates/{result.EntryGateId}", new { id = result.EntryGateId }),
            CreateEntryGateOutcome.EventNotFound => Results.NotFound(),
            _ => Results.Problem("Unexpected create-entry-gate outcome."),
        };
    }

    private static async Task<IResult> ListEntryGatesAsync(
        Guid eventId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListEntryGatesQuery(eventId), cancellationToken);
        return Results.Ok(result);
    }
}
