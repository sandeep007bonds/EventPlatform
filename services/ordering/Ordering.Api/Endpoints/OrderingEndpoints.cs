namespace Ordering.Api.Endpoints;

/// <summary>Maps the Ordering HTTP endpoints.</summary>
public static class OrderingEndpoints
{
    /// <summary>Maps the checkout and order endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/v1/checkout", CheckoutAsync).WithName("Checkout").WithTags("Checkout");
        app.MapGet("/v1/orders/{id:guid}", GetOrderAsync).WithName("GetOrder").WithTags("Orders");

        return app;
    }

    private static async Task<IResult> CheckoutAsync(
        CheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        ITenantContext tenant,
        ClaimsPrincipal principal,
        CheckoutService checkout,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.BadRequest(new { message = "The Idempotency-Key header is required." });
        }

        var result = await checkout.CheckoutAsync(
            tenant.TenantId.Value,
            userId.Value,
            request.HoldId,
            idempotencyKey,
            cancellationToken);

        return result.Outcome switch
        {
            CheckoutOutcome.Confirmed =>
                Results.Created($"/v1/orders/{result.OrderId}", new { orderId = result.OrderId }),
            CheckoutOutcome.HoldNotFound => Results.NotFound(new { message = "The hold does not exist." }),
            CheckoutOutcome.Forbidden => Results.Forbid(),
            CheckoutOutcome.HoldNotActive => Results.Conflict(new { message = "The hold is not active." }),
            CheckoutOutcome.HoldExpired => Results.Conflict(new { message = "The hold has expired." }),
            CheckoutOutcome.PaymentFailed =>
                Results.UnprocessableEntity(new { message = "Payment failed.", orderId = result.OrderId }),
            CheckoutOutcome.ConvertFailed =>
                Results.Conflict(new { message = "The seats could not be sold.", orderId = result.OrderId }),
            CheckoutOutcome.Failed =>
                Results.Conflict(new { message = "A prior checkout for this key failed.", orderId = result.OrderId }),
            _ => Results.Problem("Unexpected checkout outcome."),
        };
    }

    private static async Task<IResult> GetOrderAsync(
        Guid id,
        IOrderRepository orders,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return Results.NotFound();
        }

        var lines = order.Lines
            .Select(line => new OrderLineResponse(line.SeatId, line.PriceMinor))
            .ToList();

        var response = new OrderResponse(
            order.Id,
            order.Status.ToString(),
            order.TotalMinor,
            order.Currency,
            order.CatalogEventId,
            order.HoldId,
            lines);

        return Results.Ok(response);
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
