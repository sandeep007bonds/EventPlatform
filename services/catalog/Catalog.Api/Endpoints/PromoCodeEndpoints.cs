namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints for an event's promo codes onto the application's use cases.</summary>
public static class PromoCodeEndpoints
{
    /// <summary>Maps the <c>/v1/events/{eventId}/promo-codes</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapPromoCodeEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events/{eventId:guid}/promo-codes").WithTags("Promo codes");

        group.MapPost("/", CreatePromoCodeAsync).WithName("CreatePromoCode").RequireOrganizer();
        group.MapGet("/", ListPromoCodesAsync).WithName("ListPromoCodes").RequireOrganizer();
        group.MapPost("/{id:guid}/deactivate", DeactivatePromoCodeAsync)
            .WithName("DeactivatePromoCode")
            .RequireOrganizer();

        // Anonymous: a buyer picking seats has not necessarily logged in yet, and these codes are
        // advertised by design. Returns only public, currently-redeemable codes, and a narrower
        // shape than the organizer's listing — no redemption caps.
        group.MapGet("/public", ListPublicPromoCodesAsync).WithName("ListPublicPromoCodes").AllowAnonymous();

        // Server-to-server only: Ordering reads a code's rules at checkout over Dapr service
        // invocation. Excluded from the public API docs and deliberately NOT routed through the
        // gateway — knowing a code's discount is harmless, but this is not a browser endpoint.
        group.MapGet("/by-code/{code}", GetPromoCodeByCodeAsync)
            .WithName("GetPromoCodeByCode")
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> CreatePromoCodeAsync(
        Guid eventId,
        CreatePromoCodeRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreatePromoCodeCommand(
            eventId,
            tenant.TenantId.Value,
            request.Code,
            request.Description,
            request.DiscountType,
            request.DiscountValue,
            request.ValidFrom,
            request.ValidTo,
            request.IsPublic,
            request.MaxRedemptions,
            request.MaxRedemptionsPerBuyer,
            request.PriceTiers ?? []);

        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            CreatePromoCodeOutcome.Created =>
                Results.Created($"/v1/events/{eventId}/promo-codes/{result.PromoCodeId}", new { id = result.PromoCodeId }),
            CreatePromoCodeOutcome.EventNotFound => Results.NotFound(),
            CreatePromoCodeOutcome.DuplicateCode =>
                Results.Conflict(new { message = "This event already has a promo code with that text." }),
            _ => Results.Problem("Unexpected create-promo-code outcome."),
        };
    }

    private static async Task<IResult> ListPromoCodesAsync(
        Guid eventId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new ListPromoCodesQuery(eventId, tenant.TenantId.Value), cancellationToken);

        // null means the event isn't this tenant's — opaque 404, never "exists but not yours".
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> DeactivatePromoCodeAsync(
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

        var outcome = await sender.Send(
            new DeactivatePromoCodeCommand(eventId, id, tenant.TenantId.Value),
            cancellationToken);

        return outcome switch
        {
            DeactivatePromoCodeOutcome.Deactivated => Results.NoContent(),
            DeactivatePromoCodeOutcome.NotFound => Results.NotFound(),
            _ => Results.Problem("Unexpected deactivate-promo-code outcome."),
        };
    }

    private static async Task<IResult> ListPublicPromoCodesAsync(
        Guid eventId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListPublicPromoCodesQuery(eventId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPromoCodeByCodeAsync(
        Guid eventId,
        string code,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPromoCodeByCodeQuery(eventId, code), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
}
