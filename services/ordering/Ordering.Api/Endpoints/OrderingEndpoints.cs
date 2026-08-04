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
        app.MapGet("/v1/orders", ListOrdersAsync).WithName("ListOrders").WithTags("Orders");
        app.MapGet("/v1/orders/{id:guid}", GetOrderAsync).WithName("GetOrder").WithTags("Orders");

        return app;
    }

    private static async Task<IResult> CheckoutAsync(
        CheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        ClaimsPrincipal principal,
        DaprWorkflowClient workflowClient,
        IOrderRepository orders,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.BadRequest(new { message = "The Idempotency-Key header is required." });
        }

        if (string.IsNullOrWhiteSpace(request.BuyerEmail) || !MailAddress.TryCreate(request.BuyerEmail, out _))
        {
            return Results.BadRequest(new { message = "A valid BuyerEmail is required." });
        }

        // Idempotency: a prior attempt with this key (by this buyer) wins before starting a new
        // workflow. Scoped by buyer, not tenant — a checkout attempt is a buyer action, and the
        // buyer's own token may carry no tenant claim at all (ADR-0022).
        var existing = await orders.GetByIdempotencyKeyAsync(userId.Value, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.Status == OrderStatus.Confirmed
                ? Results.Created($"/v1/orders/{existing.Id}", new { orderId = existing.Id })
                : Results.Conflict(new { message = "A prior checkout for this key did not complete.", orderId = existing.Id });
        }

        var instanceId = Guid.CreateVersion7().ToString("N");
        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(CheckoutWorkflow),
            instanceId,
            new CheckoutWorkflowInput(userId.Value, request.HoldId, idempotencyKey, request.BuyerEmail));

        var state = await workflowClient.WaitForWorkflowCompletionAsync(
            instanceId,
            getInputsAndOutputs: true,
            cancellationToken);

        return MapCheckoutOutcome(state.ReadOutputAs<CheckoutWorkflowResult>());
    }

    private static IResult MapCheckoutOutcome(CheckoutWorkflowResult? result)
    {
        if (result is null || !Enum.TryParse<CheckoutOutcome>(result.Outcome, out var outcome))
        {
            return Results.Problem("Unexpected checkout outcome.");
        }

        return outcome switch
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
            CheckoutOutcome.Duplicate =>
                Results.Conflict(new { message = "A concurrent checkout for this key is being processed; retry the key or fetch the order.", orderId = result.OrderId }),
            _ => Results.Problem("Unexpected checkout outcome."),
        };
    }

    private static async Task<IResult> ListOrdersAsync(
        ITenantContext tenant,
        ClaimsPrincipal principal,
        IOrderRepository orders,
        CancellationToken cancellationToken,
        bool mine = false,
        bool forTenant = false,
        int page = 1,
        int pageSize = 20)
    {
        Guid? tenantId = null;
        Guid? userId = null;

        if (mine)
        {
            userId = GetUserId(principal);
            if (userId is null)
            {
                return Results.Unauthorized();
            }
        }
        else if (forTenant)
        {
            if (tenant.TenantId is null)
            {
                return Results.Unauthorized();
            }

            tenantId = tenant.TenantId;
        }
        else
        {
            return Results.BadRequest(new { message = "Specify mine=true or forTenant=true." });
        }

        var (items, totalCount) = await orders.ListAsync(tenantId, userId, page, pageSize, cancellationToken);
        var summaries = items
            .Select(o => new OrderSummaryResponse(o.Id, o.Status.ToString(), o.TotalMinor, o.Currency, o.CatalogEventId, o.CreatedAt))
            .ToList();

        return Results.Ok(new OrderListResponse(summaries, page, pageSize, totalCount));
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
            .Select(line => new OrderLineResponse(
                line.SeatId,
                line.GeneralAdmissionAllocationId,
                line.Quantity,
                line.UnitPriceMinor,
                line.PriceMinor))
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
