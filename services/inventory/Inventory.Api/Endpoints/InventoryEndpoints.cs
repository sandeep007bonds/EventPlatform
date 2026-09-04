namespace Inventory.Api.Endpoints;

/// <summary>Maps the Inventory HTTP endpoints, including the Dapr pub/sub subscription.</summary>
public static class InventoryEndpoints
{
    /// <summary>
    /// Where this service's undeliverable messages go — one topic per service, not per subscription
    /// (see <c>SubscribesTo</c>), so there is one drain rather than one per topic.
    /// </summary>
    private const string DeadLetterTopic = "deadletter-inventory";

    /// <summary>Maps the Inventory endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Dapr pub/sub: provision inventory when Catalog takes a performance on sale. One message
        // per performance, not per event — inventory is keyed by performance (ADR-0039).
        // AllowAnonymous deliberately: the Dapr sidecar delivers with no user token, and a
        // denied subscriber fails by going quiet rather than erroring.
        app.MapPost("/integration/catalog/event-session-published", OnEventSessionPublishedAsync)
            .SubscribesTo(nameof(EventSessionPublished), DeadLetterTopic)
            .WithName("OnEventSessionPublished")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // Dapr pub/sub: an organizer manually paused/resumed sales for a performance. Pausing a
        // whole event arrives as one of these per performance.
        app.MapPost("/integration/catalog/event-sales-paused", OnEventSalesPausedAsync)
            .SubscribesTo(nameof(EventSalesPaused), DeadLetterTopic)
            .WithName("OnEventSalesPaused")
            .AllowAnonymous()
            .ExcludeFromDescription();
        app.MapPost("/integration/catalog/event-sales-resumed", OnEventSalesResumedAsync)
            .SubscribesTo(nameof(EventSalesResumed), DeadLetterTopic)
            .WithName("OnEventSalesResumed")
            .AllowAnonymous()
            .ExcludeFromDescription();

        // Anonymous: remaining-capacity is public storefront data.
        app.MapGet("/v1/sessions/{eventSessionId:guid}/inventory", GetInventoryCountAsync)
            .WithName("GetInventoryCount")
            .AllowAnonymous()
            .WithTags("Inventory");

        // Anonymous, same visibility posture as Catalog's public seatmap: buyers need to see which
        // seats are taken before picking one, and organizers need it to render the block/unblock UI.
        app.MapGet("/v1/sessions/{eventSessionId:guid}/inventory/seats", GetInventorySeatsAsync)
            .WithName("GetInventorySeats")
            .WithTags("Inventory")
            .AllowAnonymous();

        // Same anonymous posture as the seat-status endpoint above — a buyer needs the real
        // allocation id (not Catalog's section id) to place a general-admission hold.
        app.MapGet("/v1/sessions/{eventSessionId:guid}/inventory/general-admission", GetGeneralAdmissionAllocationsAsync)
            .WithName("GetGeneralAdmissionAllocations")
            .WithTags("Inventory")
            .AllowAnonymous();

        // Organizer seat blocking (e.g. a kill or a restricted view) — separate from the buyer-facing
        // hold path. The policy establishes the role; the handler still checks the seats belong to
        // the caller's tenant.
        app.MapPost("/v1/sessions/{eventSessionId:guid}/inventory/block", BlockSeatsAsync)
            .RequireOrganizer()
            .WithName("BlockSeats")
            .WithTags("Inventory");
        app.MapPost("/v1/sessions/{eventSessionId:guid}/inventory/unblock", UnblockSeatsAsync)
            .RequireOrganizer()
            .WithName("UnblockSeats")
            .WithTags("Inventory");

        var holds = app.MapGroup("/v1/holds").WithTags("Holds");

        // Holding seats is the point at which a buyer must be identified (ADR-0016): browsing and
        // queueing are anonymous, this is not.
        holds.MapPost("/", PlaceHoldAsync).WithName("PlaceHold").RequireBuyer();
        holds.MapGet("/{holdId:guid}", GetHoldAsync).WithName("GetHold").RequireBuyer();

        // Internal twin of GetHold for the checkout saga, which invokes it over Dapr with no user
        // token and so cannot pass the buyer check above. Split rather than making GetHold
        // anonymous: that endpoint IS gateway-routed, and a hold reveals what someone is buying and
        // for how much. Not gateway-routed, same treatment as convert/release/extend/cancel below.
        holds.MapGet("/{holdId:guid}/snapshot", GetHoldSnapshotAsync)
            .WithName("GetHoldSnapshot")
            .AllowAnonymous()
            .ExcludeFromDescription();
        holds.MapDelete("/{holdId:guid}", ReleaseHoldAsync).WithName("ReleaseHold").RequireBuyer();

        // Internal (checkout saga, via Dapr service invocation): convert a hold to a sale, or
        // release it on compensation (no owner check — the saga acts as the system).
        holds.MapPost("/{holdId:guid}/convert", ConvertHoldAsync).WithName("ConvertHold").AllowAnonymous().ExcludeFromDescription();
        holds.MapPost("/{holdId:guid}/release", SystemReleaseHoldAsync).WithName("SystemReleaseHold").AllowAnonymous().ExcludeFromDescription();

        // Internal (checkout saga, via Dapr service invocation): extend a hold's expiry once payment
        // authentication begins. No request body — the extension duration is server-config-driven
        // (HoldOptions.PaymentExtensionTtl), never client-supplied.
        holds.MapPost("/{holdId:guid}/extend", ExtendHoldAsync).WithName("ExtendHold").AllowAnonymous().ExcludeFromDescription();

        // Internal (Ordering's cancellation saga, via Dapr service invocation): release a converted
        // hold's sold seats/quantities back to available.
        holds.MapPost("/{holdId:guid}/cancel", CancelSoldAsync).WithName("CancelSold").AllowAnonymous().ExcludeFromDescription();

        // The other half of a dead-letter topic. A topic nobody reads is just a quieter silence
        // than an infinite retry loop, so this records what could not be handled and says so
        // loudly. AllowAnonymous for the same reason as every subscriber: the sidecar delivers
        // with no user token.
        app.MapPost("/integration/dead-letter", OnDeadLetterAsync)
            .DrainsDeadLetters(DeadLetterTopic)
            .WithName("OnDeadLetterInventory")
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

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

    private static async Task<IResult> SystemReleaseHoldAsync(
        Guid holdId,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        await holds.SystemReleaseAsync(holdId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ExtendHoldAsync(
        Guid holdId,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        var expiresAt = await holds.ExtendHoldAsync(holdId, cancellationToken);
        return expiresAt is null ? Results.NotFound() : Results.Ok(new { expiresAt = expiresAt.Value });
    }

    private static async Task<IResult> GetHoldAsync(
        Guid holdId,
        ClaimsPrincipal principal,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        var view = await holds.GetHoldViewAsync(holdId, cancellationToken);
        if (view is null)
        {
            return Results.NotFound();
        }

        // A hold shows which seats someone picked and what they cost. Opaque not-found on a
        // mismatch, so a guessed hold id never confirms whose it is.
        var userId = GetUserId(principal);
        return userId is not null && view.UserId == userId.Value
            ? Results.Ok(view)
            : Results.NotFound();
    }

    /// <remarks>
    /// The saga's read path. Identical projection, no owner check — the caller is the checkout
    /// workflow acting as the system, which is also why it carries no user token to check against.
    /// </remarks>
    private static async Task<IResult> GetHoldSnapshotAsync(
        Guid holdId,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        var view = await holds.GetHoldViewAsync(holdId, cancellationToken);
        return view is null ? Results.NotFound() : Results.Ok(view);
    }

    private static async Task<IResult> ConvertHoldAsync(
        Guid holdId,
        ConvertHoldRequest request,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        var outcome = await holds.ConvertToSoldAsync(holdId, request.OrderId, cancellationToken);
        return outcome switch
        {
            ConvertHoldOutcome.Converted => Results.NoContent(),
            ConvertHoldOutcome.NotFound => Results.NotFound(),
            ConvertHoldOutcome.NotActive => Results.Conflict(new { message = "The hold is not active." }),
            ConvertHoldOutcome.Expired => Results.Conflict(new { message = "The hold has expired." }),
            ConvertHoldOutcome.Conflict =>
                Results.Conflict(new { message = "The hold could not be converted due to a concurrent change." }),
            _ => Results.Problem("Unexpected convert outcome."),
        };
    }

    private static async Task<IResult> CancelSoldAsync(
        Guid holdId,
        CancelSoldRequest request,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        var outcome = await holds.CancelSoldAsync(holdId, request.OrderId, cancellationToken);
        return outcome switch
        {
            CancelSoldOutcome.Cancelled => Results.NoContent(),
            CancelSoldOutcome.NotFound => Results.NotFound(),
            CancelSoldOutcome.NotConverted =>
                Results.Conflict(new { message = "The hold was not sold for this order." }),
            CancelSoldOutcome.Conflict =>
                Results.Conflict(new { message = "The hold could not be cancelled due to a concurrent change." }),
            _ => Results.Problem("Unexpected cancel outcome."),
        };
    }

    private static async Task<IResult> PlaceHoldAsync(
        PlaceHoldRequest request,
        ClaimsPrincipal principal,
        HoldService holds,
        HoldOptions options,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var seatIds = request.SeatIds ?? [];
        var gaSelections = request.GeneralAdmissionSelections ?? [];

        if (seatIds.Count == 0 && gaSelections.Count == 0)
        {
            return Results.BadRequest(new { message = "At least one seat or general-admission quantity is required." });
        }

        if (seatIds.Count > options.MaxSeatsPerHold)
        {
            return Results.BadRequest(new { message = $"A hold may contain at most {options.MaxSeatsPerHold} seats." });
        }

        var totalGaQuantity = gaSelections.Sum(selection => selection.Quantity);
        if (totalGaQuantity > options.MaxGeneralAdmissionQuantityPerHold)
        {
            return Results.BadRequest(new
            {
                message = $"A hold may contain at most {options.MaxGeneralAdmissionQuantityPerHold} general-admission admissions.",
            });
        }

        var result = await holds.PlaceHoldAsync(
            userId.Value,
            request.EventSessionId,
            seatIds,
            gaSelections.Select(selection => (selection.AllocationId, selection.Quantity)).ToList(),
            request.QueueAdmissionToken,
            cancellationToken);

        return result.Outcome switch
        {
            PlaceHoldOutcome.Held =>
                Results.Created($"/v1/holds/{result.HoldId}", new { holdId = result.HoldId, expiresAt = result.ExpiresAt }),
            PlaceHoldOutcome.SessionNotFound =>
                Results.NotFound(new { message = "This performance has not been provisioned yet." }),
            PlaceHoldOutcome.SeatNotFound =>
                Results.NotFound(new { message = "One or more seats do not exist for this performance." }),
            PlaceHoldOutcome.AllocationNotFound =>
                Results.NotFound(new { message = "One or more general-admission allocations do not exist for this performance." }),
            PlaceHoldOutcome.Conflict =>
                Results.Conflict(new
                {
                    message = "One or more seats or general-admission allocations are no longer available.",
                    seatId = result.ConflictSeatId,
                    allocationId = result.ConflictAllocationId,
                }),
            PlaceHoldOutcome.BookingWindowClosed =>
                Results.Conflict(new { message = "The booking window for this event has closed." }),
            PlaceHoldOutcome.BuyerLimitExceeded =>
                Results.Conflict(new { message = "This would exceed the maximum tickets allowed per buyer for this event." }),
            PlaceHoldOutcome.OnSaleNotStarted =>
                Results.Conflict(new { message = "Tickets are not on sale yet for this event." }),
            PlaceHoldOutcome.QueueAdmissionRequired =>
                Results.Conflict(new { message = "This event requires joining the queue first." }),
            PlaceHoldOutcome.SalesPaused =>
                Results.Conflict(new { message = "Sales are currently paused for this event." }),
            _ => Results.Problem("Unexpected hold outcome."),
        };
    }

    private static async Task<IResult> ReleaseHoldAsync(
        Guid holdId,
        ClaimsPrincipal principal,
        HoldService holds,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var outcome = await holds.ReleaseHoldAsync(userId.Value, holdId, cancellationToken);
        return outcome switch
        {
            ReleaseHoldOutcome.Released => Results.NoContent(),
            ReleaseHoldOutcome.NotFound => Results.NotFound(),
            ReleaseHoldOutcome.Forbidden => Results.Forbid(),
            ReleaseHoldOutcome.NotActive => Results.Conflict(new { message = "The hold is not active." }),
            ReleaseHoldOutcome.Conflict =>
                Results.Conflict(new { message = "The hold could not be released due to a concurrent change." }),
            _ => Results.Problem("Unexpected release outcome."),
        };
    }

    private static async Task<IResult> BlockSeatsAsync(
        Guid eventSessionId,
        BlockSeatsRequest request,
        ITenantContext tenant,
        SeatBlockingService blocking,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        if (request.SeatIds is null || request.SeatIds.Count == 0)
        {
            return Results.BadRequest(new { message = "At least one seat is required." });
        }

        var result = await blocking.BlockAsync(tenant.TenantId.Value, eventSessionId, request.SeatIds, request.Reason, cancellationToken);
        return result.Outcome switch
        {
            BlockSeatsOutcome.Blocked => Results.Ok(new { eventSessionId, seatIds = request.SeatIds, status = "Blocked" }),
            BlockSeatsOutcome.SeatNotFound =>
                Results.NotFound(new { message = "One or more seats do not exist for this event." }),
            BlockSeatsOutcome.Conflict =>
                Results.Conflict(new { message = "One or more seats are not available.", seatId = result.ConflictSeatId }),
            _ => Results.Problem("Unexpected block outcome."),
        };
    }

    private static async Task<IResult> UnblockSeatsAsync(
        Guid eventSessionId,
        UnblockSeatsRequest request,
        ITenantContext tenant,
        SeatBlockingService blocking,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        if (request.SeatIds is null || request.SeatIds.Count == 0)
        {
            return Results.BadRequest(new { message = "At least one seat is required." });
        }

        var outcome = await blocking.UnblockAsync(tenant.TenantId.Value, eventSessionId, request.SeatIds, cancellationToken);
        return outcome switch
        {
            UnblockSeatsOutcome.Unblocked => Results.Ok(new { eventSessionId, seatIds = request.SeatIds, status = "Available" }),
            UnblockSeatsOutcome.SeatNotFound =>
                Results.NotFound(new { message = "One or more seats do not exist for this event." }),
            UnblockSeatsOutcome.Conflict =>
                Results.Conflict(new { message = "One or more seats are not currently blocked." }),
            _ => Results.Problem("Unexpected unblock outcome."),
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

    private static async Task<IResult> OnEventSessionPublishedAsync(
        EventSessionPublished @event,
        InventoryProvisioningService provisioning,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var result = await provisioning.ProvisionAsync(
            new ProvisionSessionRequest(
                @event.TenantId,
                @event.EventSessionId,
                @event.CatalogEventId,
                @event.SeatMapId,
                @event.SeatMapVersionNumber,
                @event.BookingEndsAt,
                @event.OnSaleAt,
                @event.MaxTicketsPerBuyer,
                @event.RequiresQueue,
                @event.Allocations),
            cancellationToken);

        var logger = loggerFactory.CreateLogger("Inventory.Provisioning");
        if (result.Provisioned)
        {
            logger.LogInformation(
                "Provisioned {SeatCount} seats and {AllocationCount} general-admission pools for performance {EventSessionId}.",
                result.SeatCount,
                result.GeneralAdmissionAllocationCount,
                @event.EventSessionId);
        }
        else
        {
            logger.LogInformation("Performance {EventSessionId} already provisioned; skipped.", @event.EventSessionId);
        }

        // Ack so Dapr does not redeliver; provisioning is idempotent if it does.
        return Results.Ok();
    }

    private static async Task<IResult> OnEventSalesPausedAsync(
        EventSalesPaused @event,
        SessionSalesToggleService salesToggle,
        CancellationToken cancellationToken)
    {
        await salesToggle.SetSalesPausedAsync(@event.EventSessionId, salesPaused: true, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> OnEventSalesResumedAsync(
        EventSalesResumed @event,
        SessionSalesToggleService salesToggle,
        CancellationToken cancellationToken)
    {
        await salesToggle.SetSalesPausedAsync(@event.EventSessionId, salesPaused: false, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> GetInventoryCountAsync(
        Guid eventSessionId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var count = await repository.CountForSessionAsync(eventSessionId, cancellationToken);
        return Results.Ok(new { eventSessionId, seatCount = count });
    }

    private static async Task<IResult> GetInventorySeatsAsync(
        Guid eventSessionId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var items = await repository.ListForSessionAsync(eventSessionId, cancellationToken);
        var seats = items
            .Select(i => new InventorySeatResponse(i.SeatId, i.Status.ToString(), i.TicketTypeId, i.PriceMinor))
            .ToList();
        return Results.Ok(seats);
    }

    private static async Task<IResult> GetGeneralAdmissionAllocationsAsync(
        Guid eventSessionId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var allocations = await repository.ListGeneralAdmissionForSessionAsync(eventSessionId, cancellationToken);
        var response = allocations
            .Select(a => new GeneralAdmissionAllocationResponse(
                a.Id,
                a.AdmissionAreaId,
                a.TicketTypeId,
                a.PriceMinor,
                a.RemainingCapacity,
                a.TotalCapacity,
                a.HeldCount,
                a.SoldCount))
            .ToList();
        return Results.Ok(response);
    }
}
