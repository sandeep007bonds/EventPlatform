namespace Ordering.Api.Endpoints;

/// <summary>Maps the Ordering HTTP endpoints.</summary>
public static class OrderingEndpoints
{
    /// <summary>
    /// Where this service's undeliverable messages go — one topic per service, not per subscription
    /// (see <c>SubscribesTo</c>), so there is one drain rather than one per topic.
    /// </summary>
    private const string DeadLetterTopic = "deadletter-ordering";

    // How long CheckoutAsync polls the Order row for a client secret before falling back to a full
    // blocking wait on the workflow's completion. Generous enough to cover the create-intent +
    // record + extend-hold activities under normal load without making every checkout feel slow.

    private static async Task<IResult> OnDeadLetterAsync(
        JsonNode? body,
        DeadLetterDrain drain,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        // Best-effort only. Dapr's delivery headers for a dead letter are not something to depend
        // on, so this is a hint; the envelope's own EventType is the topic the relay published to
        // and is what the drain actually falls back on.
        var topic = http.Request.Headers["Ce-Topic"].FirstOrDefault()
            ?? http.Request.Headers["topic"].FirstOrDefault();

        await drain.RecordAsync(topic, body, cancellationToken);

        // 200 regardless. A dead letter that fails to record would be retried and then dead-lettered
        // again, and there is nowhere further to send it — the log and the alert are the escalation.
        return Results.Ok();
    }

    private static readonly TimeSpan CheckoutPollBudget = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CheckoutPollInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Maps the checkout and order endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapOrderingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Buying is a buyer action: an organizer token has no business placing an order, and
        // checkout derives the tenant from the fetched hold rather than any caller claim (ADR-0022).
        app.MapPost("/v1/checkout", CheckoutAsync).WithName("Checkout").WithTags("Checkout").RequireBuyer();
        app.MapPost("/v1/checkout/quote", QuoteCheckoutAsync).WithName("QuoteCheckout").WithTags("Checkout").RequireBuyer();
        app.MapPost("/v1/orders/{id:guid}/cancel", CancelOrderAsync).WithName("CancelOrder").WithTags("Orders").RequireBuyer();
        app.MapPost("/v1/orders/{id:guid}/payment/sync", SyncOrderPaymentAsync)
            .WithName("SyncOrderPayment")
            .WithTags("Orders")
            .RequireBuyer();

        // Both roles reach these legitimately — a buyer sees their own orders (?mine=true), an
        // organizer sees their tenant's (?forTenant=true). Which records come back is decided in
        // the handler by ownership, not here.
        app.MapGet("/v1/orders", ListOrdersAsync).WithName("ListOrders").WithTags("Orders").RequireAuthenticatedCaller();
        app.MapGet("/v1/orders/{id:guid}", GetOrderAsync).WithName("GetOrder").WithTags("Orders").RequireAuthenticatedCaller();

        // Dapr pub/sub: resume a checkout saga waiting on payment authentication (ADR-0028). The
        // order id doubles as the saga's own Dapr instance id, so no lookup is needed.
        //
        // AllowAnonymous deliberately: the sidecar delivers these with no user token, so any
        // authorization requirement here silently stops every payment outcome from reaching its
        // saga — checkouts would hang rather than fail visibly. Not gateway-routed, so the only
        // caller is the sidecar on localhost.
        app.MapPost("/integration/payments/payment-captured", OnPaymentCapturedAsync)
            .SubscribesTo(nameof(PaymentCaptured), DeadLetterTopic)
            .WithName("OnPaymentCaptured")
            .AllowAnonymous()
            .ExcludeFromDescription();
        app.MapPost("/integration/payments/payment-failed", OnPaymentFailedAsync)
            .SubscribesTo(nameof(PaymentFailed), DeadLetterTopic)
            .WithName("OnPaymentFailed")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // The other half of a dead-letter topic. A topic nobody reads is just a quieter silence
        // than an infinite retry loop, so this records what could not be handled and says so
        // loudly. AllowAnonymous for the same reason as every subscriber: the sidecar delivers
        // with no user token.
        app.MapPost("/integration/dead-letter", OnDeadLetterAsync)
            .DrainsDeadLetters(DeadLetterTopic)
            .WithName("OnDeadLetterOrdering")
            .AllowAnonymous()
            .ExcludeFromDescription();

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

        // The order id is minted here, before scheduling, and doubles as the workflow's own Dapr
        // instance id — this lets the payment webhook subscriber raise an event straight back to the
        // running saga with no lookup table (ADR-0028).
        var orderId = Guid.CreateVersion7();
        var instanceId = orderId.ToString("N");
        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(CheckoutWorkflow),
            instanceId,
            new CheckoutWorkflowInput(userId.Value, request.HoldId, idempotencyKey, request.BuyerEmail, orderId, request.PromoCode));

        var completionTask = workflowClient.WaitForWorkflowCompletionAsync(instanceId, getInputsAndOutputs: true, cancellationToken);
        var pollTask = PollForClientSecretAsync(orderId, orders, cancellationToken);

        var winner = await Task.WhenAny(completionTask, pollTask);
        if (winner == pollTask)
        {
            var polled = await pollTask;
            if (polled is not null)
            {
                return Results.Ok(new { orderId, clientSecret = polled.PaymentClientSecret });
            }

            // The poll budget elapsed with the saga still mid-flight (a genuinely slow intent-create
            // call, or — rarely — a concurrent double-submit whose real order has a different,
            // winning id our poll never finds) — degrade to a full blocking wait, same as before this
            // change existed.
        }

        var state = await completionTask;
        return MapCheckoutOutcome(state.ReadOutputAs<CheckoutWorkflowResult>());
    }

    // Prices a hold without creating anything, so the buyer can see what a promo code is worth
    // before committing. Deliberately returns 200 with a rejection reason rather than an error when
    // a code doesn't apply: the quote itself is still valid (just undiscounted), and the buyer needs
    // to see the real total next to the reason their code failed.
    private static async Task<IResult> QuoteCheckoutAsync(
        CheckoutQuoteRequest request,
        ClaimsPrincipal principal,
        IHoldClient holds,
        ICatalogEventClient catalog,
        PromoCodeEvaluator promoCodes,
        CheckoutOptions options,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var hold = await holds.GetHoldAsync(request.HoldId, cancellationToken);
        if (hold is null)
        {
            return Results.NotFound(new { message = "The hold does not exist." });
        }

        // Ownership, not just existence: quoting someone else's hold would leak what they are buying
        // and for how much.
        if (hold.UserId != userId.Value)
        {
            return Results.Forbid();
        }

        var lines = hold.Lines
            .Select(line => new OrderLineSpec(
                line.InventoryItemId,
                line.SeatId,
                line.GeneralAdmissionAllocationId,
                line.Quantity,
                line.TicketTypeId,
                line.UnitPriceMinor,
                line.PriceMinor))
            .ToList();

        var pricing = await catalog.GetEventPricingAsync(hold.CatalogEventId, cancellationToken)
                      ?? new EventPricing(options.DefaultCurrency, null, null, 0);

        PromoCodeTerms? terms = null;
        string? appliedCode = null;
        string? rejection = null;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var evaluation = await promoCodes.EvaluateAsync(
                hold.CatalogEventId,
                request.PromoCode,
                userId.Value,
                lines,
                DateTimeOffset.UtcNow,
                cancellationToken);

            if (evaluation.IsAccepted)
            {
                terms = evaluation.Terms;
                appliedCode = evaluation.Code;
            }
            else
            {
                rejection = evaluation.Rejection?.ToString();
            }
        }

        var quote = OrderPricingCalculator.Calculate(
            lines, terms, pricing.TaxRatePercent, pricing.BookingFeePerTicketMinor);

        return Results.Ok(new CheckoutQuoteResponse(
            quote.SubtotalMinor,
            quote.DiscountMinor,
            quote.BookingFeeMinor,
            quote.TaxMinor,
            quote.TotalMinor,
            pricing.Currency,
            pricing.TaxLabel,
            appliedCode,
            rejection));
    }

    // Polls the order row until it either has a payment client secret (payment pending — the
    // fast-return case) or reaches a terminal status with none (e.g. the simulated gateway's
    // instant-capture path), or the poll budget elapses (returns null either way).
    private static async Task<Order?> PollForClientSecretAsync(Guid orderId, IOrderRepository orders, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(CheckoutPollBudget);
        while (DateTimeOffset.UtcNow < deadline)
        {
            // Untracked: the saga's activities write the client secret from a *different* DbContext,
            // and a tracking read would keep handing back the first-loaded (null-secret) instance
            // from this scope's identity map for every subsequent poll.
            var order = await orders.GetSnapshotByIdAsync(orderId, cancellationToken);
            if (order is not null && (order.PaymentClientSecret is not null || order.Status is OrderStatus.Confirmed or OrderStatus.Failed))
            {
                return order;
            }

            await Task.Delay(CheckoutPollInterval, cancellationToken);
        }

        return null;
    }

    private static Task<IResult> OnPaymentCapturedAsync(
        PaymentCaptured @event,
        DaprWorkflowClient workflowClient,
        IOrderRepository orders,
        IPaymentClient payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
        RaisePaymentOutcomeAsync(@event.OrderId, captured: true, failureReason: null, workflowClient, orders, payments, loggerFactory, cancellationToken);

    private static Task<IResult> OnPaymentFailedAsync(
        PaymentFailed @event,
        DaprWorkflowClient workflowClient,
        IOrderRepository orders,
        IPaymentClient payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
        RaisePaymentOutcomeAsync(@event.OrderId, captured: false, failureReason: @event.Reason, workflowClient, orders, payments, loggerFactory, cancellationToken);

    // Shared by both webhook-driven subscribers: raise the outcome into the still-running checkout
    // saga (the common case), or — if the saga already finished, most likely because the extended
    // hold's deadline already fired — handle a late arrival. A late PaymentFailed needs no action
    // (the order is already Failed either way); a late PaymentCaptured means the buyer was actually
    // charged after we already released their seats, so it's refunded directly rather than left
    // silently orphaned.
    private static async Task<IResult> RaisePaymentOutcomeAsync(
        Guid orderId,
        bool captured,
        string? failureReason,
        DaprWorkflowClient workflowClient,
        IOrderRepository orders,
        IPaymentClient payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var instanceId = orderId.ToString("N");
        var state = await workflowClient.GetWorkflowStateAsync(instanceId);
        if (state.RuntimeStatus == WorkflowRuntimeStatus.Running)
        {
            await workflowClient.RaiseEventAsync(instanceId, "PaymentOutcome", new PaymentOutcomeSignal(captured, failureReason), cancellationToken);

            // Ack so Dapr does not redeliver.
            return Results.Ok();
        }

        if (captured)
        {
            var order = await orders.GetByIdAsync(orderId, cancellationToken);
            if (order is not null && order.Status == OrderStatus.Failed)
            {
                var logger = loggerFactory.CreateLogger("Ordering.PaymentOutcome");
                logger.LogWarning(
                    "Payment for order {OrderId} captured after its checkout saga already failed; refunding.",
                    orderId);

                // In full (null amount): the saga already failed, so this buyer has no tickets and
                // there is no completed sale to charge a booking fee for.
                await payments.RefundAsync(
                    orderId, $"late-capture-refund-{orderId:N}", amountMinor: null, cancellationToken);
            }
        }

        return Results.Ok();
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
            CheckoutOutcome.PaymentTimedOut =>
                Results.UnprocessableEntity(new { message = "Payment was not completed in time.", orderId = result.OrderId }),
            CheckoutOutcome.PromoCodeInvalid =>
                Results.Conflict(new { message = "That promo code is no longer valid for this purchase. Remove it and try again." }),
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
            .Select(o => new OrderSummaryResponse(
                o.Id,
                o.Status.ToString(),
                o.TotalMinor,
                o.Currency,
                o.CatalogEventId,
                o.EventSessionId,
                o.CreatedAt))
            .ToList();

        return Results.Ok(new OrderListResponse(summaries, page, pageSize, totalCount));
    }

    private static async Task<IResult> GetOrderAsync(
        Guid id,
        ClaimsPrincipal principal,
        ITenantContext tenant,
        IOrderRepository orders,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null && tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var order = await orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return Results.NotFound();
        }

        // Either the buyer who placed it, or an organizer whose tenant sold it. Anything else is
        // reported as not-found rather than forbidden, so this never confirms that someone else's
        // order exists — an order id is a bare GUID in a public URL, and "403 vs 404" would turn it
        // into an oracle.
        var isBuyer = userId is not null && order.UserId == userId.Value;
        var isSellingTenant = tenant.TenantId is not null && order.TenantId == tenant.TenantId.Value;
        if (!isBuyer && !isSellingTenant)
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
            order.SubtotalMinor,
            order.DiscountMinor,
            order.BookingFeeMinor,
            order.TaxMinor,
            order.TaxLabel,
            order.PromoCodeText,
            order.Currency,
            order.CatalogEventId,
            order.EventSessionId,
            order.HoldId,
            lines,
            isBuyer ? order.PaymentClientSecret : null);

        return Results.Ok(response);
    }

    /// <remarks>
    /// Called by the buyer's browser the moment Stripe's <c>confirmPayment</c> resolves. The browser
    /// is the first to know the payment succeeded — it holds the confirmed PaymentIntent — so rather
    /// than make the backend rediscover that by waiting for a webhook (which cannot reach a
    /// developer machine) or by polling on a timer, it simply tells us to look now. This only
    /// *triggers* reconciliation: Payments still re-reads the intent from Stripe itself, so a
    /// client claiming success it didn't have changes nothing. The webhook and the saga's own poll
    /// remain as backstops for a buyer who closes the tab mid-payment (ADR-0028).
    /// </remarks>
    private static async Task<IResult> SyncOrderPaymentAsync(
        Guid id,
        ClaimsPrincipal principal,
        DaprWorkflowClient workflowClient,
        IOrderRepository orders,
        IPaymentClient payments,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        // Ownership check: a buyer may only nudge their own order. A mismatch is reported as
        // not-found rather than forbidden, so this never confirms another buyer's order exists.
        var order = await orders.GetSnapshotByIdAsync(id, cancellationToken);
        if (order is null || order.UserId != userId.Value)
        {
            return Results.NotFound();
        }

        var status = await payments.SyncStatusAsync(id, cancellationToken);

        // Hand the outcome straight to the saga rather than letting it come back around via
        // Payments' outbox -> pub/sub -> our own subscriber. That chain is fine as an independent
        // route, but it must not be the *only* one this path depends on: reconciliation emits
        // PaymentCaptured only on the transition, so a payment already captured by an earlier call
        // (or by a webhook whose delivery was then lost) reports "Captured" here while emitting
        // nothing at all — leaving the saga parked forever with no further event coming.
        // Raising it directly is also several hops faster, and RaisePaymentOutcomeAsync already
        // no-ops safely when the saga is no longer running.
        if (string.Equals(status, "Captured", StringComparison.Ordinal))
        {
            await RaisePaymentOutcomeAsync(
                id, captured: true, failureReason: null, workflowClient, orders, payments, loggerFactory, cancellationToken);
        }
        else if (string.Equals(status, "Failed", StringComparison.Ordinal))
        {
            await RaisePaymentOutcomeAsync(
                id, captured: false, failureReason: "payment_failed", workflowClient, orders, payments, loggerFactory, cancellationToken);
        }

        return Results.Ok(new PaymentSyncStatusResponse(status));
    }

    private static async Task<IResult> CancelOrderAsync(
        Guid id,
        ClaimsPrincipal principal,
        DaprWorkflowClient workflowClient,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var instanceId = Guid.CreateVersion7().ToString("N");
        await workflowClient.ScheduleNewWorkflowAsync(
            nameof(CancelOrderWorkflow),
            instanceId,
            new CancelOrderWorkflowInput(id, userId.Value));

        var state = await workflowClient.WaitForWorkflowCompletionAsync(
            instanceId,
            getInputsAndOutputs: true,
            cancellationToken);

        return MapCancelOutcome(state.ReadOutputAs<CancelOrderWorkflowResult>());
    }

    private static IResult MapCancelOutcome(CancelOrderWorkflowResult? result)
    {
        if (result is null || !Enum.TryParse<CancelOrderOutcome>(result.Outcome, out var outcome))
        {
            return Results.Problem("Unexpected cancel outcome.");
        }

        return outcome switch
        {
            CancelOrderOutcome.Cancelled => Results.NoContent(),
            CancelOrderOutcome.OrderNotFound => Results.NotFound(new { message = "The order does not exist." }),
            CancelOrderOutcome.Forbidden => Results.Forbid(),
            CancelOrderOutcome.NotConfirmed =>
                Results.Conflict(new { message = "Only a confirmed order can be cancelled.", orderId = result.OrderId }),
            CancelOrderOutcome.TicketAlreadyCheckedIn =>
                Results.Conflict(new { message = "One or more tickets for this order have already been checked in.", orderId = result.OrderId }),
            CancelOrderOutcome.Failed =>
                Results.Conflict(new { message = "The order could not be cancelled.", orderId = result.OrderId }),
            _ => Results.Problem("Unexpected cancel outcome."),
        };
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        // `sub` only: AuthenticationExtensions turns MapInboundClaims off, so the claim keeps the
        // name the issuer gave it. The ClaimTypes.NameIdentifier fallback that used to sit here was
        // a workaround for that mapping, and kept working while the role policies silently did not.
        var value = principal.FindFirstValue(EventPlatformClaims.Subject);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
