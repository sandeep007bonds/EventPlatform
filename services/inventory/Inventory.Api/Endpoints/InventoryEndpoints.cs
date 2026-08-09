namespace Inventory.Api.Endpoints;

/// <summary>Maps the Inventory HTTP endpoints, including the Dapr pub/sub subscription.</summary>
public static class InventoryEndpoints
{
    /// <summary>Maps the Inventory endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Dapr pub/sub: provision seat inventory when Catalog publishes an event.
        app.MapPost("/integration/catalog/event-published", OnEventPublishedAsync)
            .WithTopic("pubsub", nameof(EventPublished))
            .WithName("OnEventPublished")
            .ExcludeFromDescription();

        // Dapr pub/sub: an organizer manually paused/resumed sales for a published event.
        app.MapPost("/integration/catalog/event-sales-paused", OnEventSalesPausedAsync)
            .WithTopic("pubsub", nameof(EventSalesPaused))
            .WithName("OnEventSalesPaused")
            .ExcludeFromDescription();
        app.MapPost("/integration/catalog/event-sales-resumed", OnEventSalesResumedAsync)
            .WithTopic("pubsub", nameof(EventSalesResumed))
            .WithName("OnEventSalesResumed")
            .ExcludeFromDescription();

        app.MapGet("/v1/events/{eventId:guid}/inventory", GetInventoryCountAsync)
            .WithName("GetInventoryCount")
            .WithTags("Inventory");

        // Anonymous, same visibility posture as Catalog's public seatmap: buyers need to see which
        // seats are taken before picking one, and organizers need it to render the block/unblock UI.
        app.MapGet("/v1/events/{eventId:guid}/inventory/seats", GetInventorySeatsAsync)
            .WithName("GetInventorySeats")
            .WithTags("Inventory")
            .AllowAnonymous();

        // Same anonymous posture as the seat-status endpoint above — a buyer needs the real
        // allocation id (not Catalog's section id) to place a general-admission hold.
        app.MapGet("/v1/events/{eventId:guid}/inventory/general-admission", GetGeneralAdmissionAllocationsAsync)
            .WithName("GetGeneralAdmissionAllocations")
            .WithTags("Inventory")
            .AllowAnonymous();

        // Organizer seat blocking (e.g. a kill or a restricted view) — separate from the buyer-facing
        // hold path. No RBAC yet (tracked separately): any authenticated caller in the tenant can
        // block/unblock their own tenant's seats, same as other organizer-facing endpoints today.
        app.MapPost("/v1/events/{eventId:guid}/inventory/block", BlockSeatsAsync)
            .WithName("BlockSeats")
            .WithTags("Inventory");
        app.MapPost("/v1/events/{eventId:guid}/inventory/unblock", UnblockSeatsAsync)
            .WithName("UnblockSeats")
            .WithTags("Inventory");

        var holds = app.MapGroup("/v1/holds").WithTags("Holds");
        holds.MapPost("/", PlaceHoldAsync).WithName("PlaceHold");
        holds.MapGet("/{holdId:guid}", GetHoldAsync).WithName("GetHold");
        holds.MapDelete("/{holdId:guid}", ReleaseHoldAsync).WithName("ReleaseHold");

        // Internal (checkout saga, via Dapr service invocation): convert a hold to a sale, or
        // release it on compensation (no owner check — the saga acts as the system).
        holds.MapPost("/{holdId:guid}/convert", ConvertHoldAsync).WithName("ConvertHold").ExcludeFromDescription();
        holds.MapPost("/{holdId:guid}/release", SystemReleaseHoldAsync).WithName("SystemReleaseHold").ExcludeFromDescription();

        // Internal (checkout saga, via Dapr service invocation): extend a hold's expiry once payment
        // authentication begins. No request body — the extension duration is server-config-driven
        // (HoldOptions.PaymentExtensionTtl), never client-supplied.
        holds.MapPost("/{holdId:guid}/extend", ExtendHoldAsync).WithName("ExtendHold").ExcludeFromDescription();

        // Internal (Ordering's cancellation saga, via Dapr service invocation): release a converted
        // hold's sold seats/quantities back to available.
        holds.MapPost("/{holdId:guid}/cancel", CancelSoldAsync).WithName("CancelSold").ExcludeFromDescription();

        return app;
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
            request.EventId,
            seatIds,
            gaSelections.Select(selection => (selection.AllocationId, selection.Quantity)).ToList(),
            request.QueueAdmissionToken,
            cancellationToken);

        return result.Outcome switch
        {
            PlaceHoldOutcome.Held =>
                Results.Created($"/v1/holds/{result.HoldId}", new { holdId = result.HoldId, expiresAt = result.ExpiresAt }),
            PlaceHoldOutcome.EventNotFound =>
                Results.NotFound(new { message = "This event has not been provisioned yet." }),
            PlaceHoldOutcome.SeatNotFound =>
                Results.NotFound(new { message = "One or more seats do not exist for this event." }),
            PlaceHoldOutcome.AllocationNotFound =>
                Results.NotFound(new { message = "One or more general-admission allocations do not exist for this event." }),
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
        Guid eventId,
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

        var result = await blocking.BlockAsync(tenant.TenantId.Value, eventId, request.SeatIds, request.Reason, cancellationToken);
        return result.Outcome switch
        {
            BlockSeatsOutcome.Blocked => Results.Ok(new { eventId, seatIds = request.SeatIds, status = "Blocked" }),
            BlockSeatsOutcome.SeatNotFound =>
                Results.NotFound(new { message = "One or more seats do not exist for this event." }),
            BlockSeatsOutcome.Conflict =>
                Results.Conflict(new { message = "One or more seats are not available.", seatId = result.ConflictSeatId }),
            _ => Results.Problem("Unexpected block outcome."),
        };
    }

    private static async Task<IResult> UnblockSeatsAsync(
        Guid eventId,
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

        var outcome = await blocking.UnblockAsync(tenant.TenantId.Value, eventId, request.SeatIds, cancellationToken);
        return outcome switch
        {
            UnblockSeatsOutcome.Unblocked => Results.Ok(new { eventId, seatIds = request.SeatIds, status = "Available" }),
            UnblockSeatsOutcome.SeatNotFound =>
                Results.NotFound(new { message = "One or more seats do not exist for this event." }),
            UnblockSeatsOutcome.Conflict =>
                Results.Conflict(new { message = "One or more seats are not currently blocked." }),
            _ => Results.Problem("Unexpected unblock outcome."),
        };
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static async Task<IResult> OnEventPublishedAsync(
        EventPublished @event,
        InventoryProvisioningService provisioning,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var result = await provisioning.ProvisionAsync(
            @event.TenantId,
            @event.CatalogEventId,
            @event.BookingEndsAt,
            @event.MaxTicketsPerBuyer,
            @event.OnSaleAt,
            @event.RequiresQueue,
            cancellationToken);

        var logger = loggerFactory.CreateLogger("Inventory.Provisioning");
        if (result.Provisioned)
        {
            logger.LogInformation(
                "Provisioned {SeatCount} seats and {AllocationCount} general-admission allocations for event {EventId}.",
                result.SeatCount,
                result.GeneralAdmissionAllocationCount,
                @event.CatalogEventId);
        }
        else
        {
            logger.LogInformation("Event {EventId} already provisioned; skipped.", @event.CatalogEventId);
        }

        // Ack so Dapr does not redeliver; provisioning is idempotent if it does.
        return Results.Ok();
    }

    private static async Task<IResult> OnEventSalesPausedAsync(
        EventSalesPaused @event,
        EventSalesToggleService salesToggle,
        CancellationToken cancellationToken)
    {
        await salesToggle.SetSalesPausedAsync(@event.CatalogEventId, salesPaused: true, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> OnEventSalesResumedAsync(
        EventSalesResumed @event,
        EventSalesToggleService salesToggle,
        CancellationToken cancellationToken)
    {
        await salesToggle.SetSalesPausedAsync(@event.CatalogEventId, salesPaused: false, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> GetInventoryCountAsync(
        Guid eventId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var count = await repository.CountForEventAsync(eventId, cancellationToken);
        return Results.Ok(new { eventId, seatCount = count });
    }

    private static async Task<IResult> GetInventorySeatsAsync(
        Guid eventId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var items = await repository.ListForEventAsync(eventId, cancellationToken);
        var seats = items.Select(i => new InventorySeatResponse(i.SeatId, i.Status.ToString())).ToList();
        return Results.Ok(seats);
    }

    private static async Task<IResult> GetGeneralAdmissionAllocationsAsync(
        Guid eventId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var allocations = await repository.ListGeneralAdmissionForEventAsync(eventId, cancellationToken);
        var response = allocations
            .Select(a => new GeneralAdmissionAllocationResponse(a.Id, a.CatalogSectionId, a.RemainingCapacity, a.TotalCapacity, a.HeldCount, a.SoldCount))
            .ToList();
        return Results.Ok(response);
    }
}
