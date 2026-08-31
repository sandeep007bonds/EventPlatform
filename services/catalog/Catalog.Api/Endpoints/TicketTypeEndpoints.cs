namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints for an event's ticket types onto the application's use cases.</summary>
public static class TicketTypeEndpoints
{
    /// <summary>Maps the <c>/v1/events/{eventId}/ticket-types</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapTicketTypeEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events/{eventId:guid}/ticket-types").WithTags("Ticket types");

        // Organizer-only, and every handler additionally checks the event belongs to the caller's
        // tenant, answering an opaque 404 on a mismatch — same posture as promo codes and seat maps.
        //
        // Unlike the seat-map endpoints these are NOT restricted to a draft event: creating a type
        // on a published event is the point. Repricing one is the exception, refused after publish
        // by UpdateTicketTypeHandler.
        group.MapPost("/", CreateTicketTypeAsync).WithName("CreateTicketType").RequireOrganizer();
        group.MapGet("/", ListTicketTypesAsync).WithName("ListTicketTypes").RequireOrganizer();
        group.MapPut("/{id:guid}", UpdateTicketTypeAsync).WithName("UpdateTicketType").RequireOrganizer();
        group.MapPost("/{id:guid}/deactivate", DeactivateTicketTypeAsync)
            .WithName("DeactivateTicketType")
            .RequireOrganizer();

        return app;
    }

    private static async Task<IResult> CreateTicketTypeAsync(
        Guid eventId,
        CreateTicketTypeRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateTicketTypeCommand(
            eventId,
            tenant.TenantId.Value,
            request.Name,
            request.PriceMinor,
            request.Description,
            request.SalesStartsAt,
            request.SalesEndsAt,
            request.MaxPerBuyer,
            request.SortOrder);

        var result = await sender.Send(command, cancellationToken);
        return result.Outcome switch
        {
            CreateTicketTypeOutcome.Created => Results.Created(
                $"/v1/events/{eventId}/ticket-types/{result.TicketTypeId}",
                new { id = result.TicketTypeId }),
            CreateTicketTypeOutcome.EventNotFound => Results.NotFound(),
            CreateTicketTypeOutcome.DuplicateName =>
                Results.Conflict(new { message = "This event already has a ticket type with that name." }),
            _ => Results.Problem("Unexpected create-ticket-type outcome."),
        };
    }

    private static async Task<IResult> ListTicketTypesAsync(
        Guid eventId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new ListTicketTypesQuery(eventId, tenant.TenantId.Value), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateTicketTypeAsync(
        Guid eventId,
        Guid id,
        UpdateTicketTypeRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateTicketTypeCommand(
            eventId,
            id,
            tenant.TenantId.Value,
            request.Name,
            request.PriceMinor,
            request.Description,
            request.SalesStartsAt,
            request.SalesEndsAt,
            request.MaxPerBuyer,
            request.SortOrder);

        var outcome = await sender.Send(command, cancellationToken);
        return outcome switch
        {
            UpdateTicketTypeOutcome.Updated => Results.NoContent(),
            UpdateTicketTypeOutcome.NotFound => Results.NotFound(),
            UpdateTicketTypeOutcome.DuplicateName =>
                Results.Conflict(new { message = "This event already has a ticket type with that name." }),
            UpdateTicketTypeOutcome.PriceLockedAfterPublish => Results.Conflict(new
            {
                message = "A ticket type's price cannot be changed once its event is published.",
            }),
            _ => Results.Problem("Unexpected update-ticket-type outcome."),
        };
    }

    private static async Task<IResult> DeactivateTicketTypeAsync(
        Guid eventId,
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new DeactivateTicketTypeCommand(eventId, id, tenant.TenantId.Value);
        var outcome = await sender.Send(command, cancellationToken);
        return outcome switch
        {
            DeactivateTicketTypeOutcome.Deactivated => Results.NoContent(),
            DeactivateTicketTypeOutcome.NotFound => Results.NotFound(),
            _ => Results.Problem("Unexpected deactivate-ticket-type outcome."),
        };
    }
}
