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

        // Provider callback: authenticated by signature, not a bearer token, so anonymous.
        group.MapPost("/webhooks/stripe", StripeWebhookAsync)
            .WithName("StripeWebhook")
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> StripeWebhookAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var gateway = httpContext.RequestServices.GetService<IPaymentWebhookGateway>();
        if (gateway is null)
        {
            // No webhook signing secret configured — there is nothing to verify against.
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        // The signature is computed over the exact bytes Stripe sent, so read the raw body.
        string payload;
        using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8))
        {
            payload = await reader.ReadToEndAsync(cancellationToken);
        }

        PaymentWebhookNotification? notification;
        try
        {
            notification = gateway.Verify(payload, httpContext.Request.Headers["Stripe-Signature"]);
        }
        catch (PaymentWebhookVerificationException)
        {
            return Results.BadRequest();
        }

        if (notification is not null)
        {
            var webhooks = httpContext.RequestServices.GetRequiredService<PaymentWebhookService>();
            await webhooks.ProcessAsync(notification, cancellationToken);
        }

        // Acknowledge (even for ignored event types) so the provider stops retrying.
        return Results.Ok();
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
