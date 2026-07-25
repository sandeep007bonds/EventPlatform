namespace Payments.Api.Endpoints;

/// <summary>Maps the Payments HTTP endpoints (internal, called by the checkout saga).</summary>
public static class PaymentsEndpoints
{
    /// <summary>Maps the charge and refund endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/payments").WithTags("Payments");
        group.MapPost("/charge", ChargeAsync).WithName("Charge").ExcludeFromDescription();
        group.MapPost("/refund", RefundAsync).WithName("Refund").ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> ChargeAsync(
        ChargeRequest request,
        PaymentService payments,
        CancellationToken cancellationToken)
    {
        var result = await payments.ChargeAsync(
            request.TenantId,
            request.OrderId,
            request.AmountMinor,
            request.Currency,
            request.IdempotencyKey,
            cancellationToken);

        var response = new ChargeResponse(
            result.Outcome.ToString(),
            result.PaymentId,
            result.ProviderReference,
            result.FailureReason);

        return result.Outcome == ChargeOutcome.Captured
            ? Results.Ok(response)
            : Results.UnprocessableEntity(response);
    }

    private static async Task<IResult> RefundAsync(
        RefundRequest request,
        PaymentService payments,
        CancellationToken cancellationToken)
    {
        await payments.RefundAsync(request.OrderId, cancellationToken);
        return Results.NoContent();
    }
}
