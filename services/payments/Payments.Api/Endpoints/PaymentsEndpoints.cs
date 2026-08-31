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

        // AllowAnonymous deliberately: the checkout and cancellation sagas invoke these over Dapr
        // with no user token, and neither is gateway-routed. Money movement is authorized by the
        // saga having got this far, not by a caller claim.
        group.MapPost("/intents", CreateIntentAsync).WithName("CreateIntent").AllowAnonymous().ExcludeFromDescription();
        group.MapPost("/refund", RefundAsync).WithName("Refund").AllowAnonymous().ExcludeFromDescription();

        // Pull counterpart to the Stripe webhook: re-reads a payment's live state from the provider
        // and applies the same reconciliation. Lets the checkout saga learn an outcome by asking,
        // so it doesn't depend on the provider being able to call us back (ADR-0028).
        group.MapPost("/{orderId:guid}/sync", SyncPaymentAsync).WithName("SyncPayment").AllowAnonymous().ExcludeFromDescription();

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
        using (var reader = new StreamReader(httpContext.Request.Body))
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

    private static async Task<IResult> CreateIntentAsync(
        CreateIntentRequest request,
        PaymentService payments,
        CancellationToken cancellationToken)
    {
        var result = await payments.CreatePaymentIntentAsync(
            request.TenantId,
            request.OrderId,
            request.AmountMinor,
            request.Currency,
            request.IdempotencyKey,
            cancellationToken);

        // Always 200 — there is no synchronous-failure branch any more. A genuine hard failure at
        // intent-creation time (bad amount, PSP outage) is left to propagate as an unhandled
        // exception, same as it already implicitly was before this endpoint existed; the eventual
        // capture/decline outcome, once the buyer authenticates, arrives later via the webhook.
        return Results.Ok(new CreateIntentResponse(result.PaymentId, result.ProviderReference, result.ClientSecret, result.Captured));
    }

    private static async Task<IResult> SyncPaymentAsync(
        Guid orderId,
        PaymentSyncService sync,
        CancellationToken cancellationToken)
    {
        var result = await sync.SyncAsync(orderId, cancellationToken);
        return Results.Ok(new PaymentSyncResponse(result.ToString()));
    }

    private static async Task<IResult> RefundAsync(
        RefundRequest request,
        PaymentService payments,
        CancellationToken cancellationToken)
    {
        await payments.RefundAsync(request.OrderId, request.AmountMinor, cancellationToken);
        return Results.NoContent();
    }
}
