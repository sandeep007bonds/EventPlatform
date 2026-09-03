namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints onto the application's use cases.</summary>
public static class CatalogEndpoints
{
    /// <summary>Maps the <c>/v1/events</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/events").WithTags("Events");

        // Storefront reads. Anonymous by design — browsing is the product's front door. The
        // handlers still apply Event.IsVisibleTo, so an anonymous caller sees non-draft events
        // only; a tenant additionally sees its own drafts.
        group.MapGet("/", ListEventsAsync).WithName("ListEvents").AllowAnonymous();
        group.MapGet("/{id:guid}", GetEventAsync).WithName("GetEvent").AllowAnonymous();

        // Routed under /by-slug/ rather than as a bare /{slug}, which would shadow every sibling
        // route on this group the moment one of them stopped being a :guid.
        group.MapGet("/by-slug/{slug}", GetEventBySlugAsync).WithName("GetEventBySlug").AllowAnonymous();

        // Organizer writes. The policy establishes only that the caller is an organizer at all —
        // each handler still checks that this event belongs to their tenant, and answers a
        // mismatch with an opaque 404.
        group.MapPost("/", CreateEventAsync).WithName("CreateEvent").RequireOrganizer();
        group.MapPost("/{id:guid}/publish", PublishEventAsync).WithName("PublishEvent").RequireOrganizer();
        group.MapPost("/{id:guid}/pause-sales", PauseSalesAsync).WithName("PauseSales").RequireOrganizer();
        group.MapPost("/{id:guid}/resume-sales", ResumeSalesAsync).WithName("ResumeSales").RequireOrganizer();

        // Renamed from /details when the dates and the venue moved to the performances that own
        // them: what is left here is the money and the selling rules for the whole run, and the
        // route now says so.
        group.MapPut("/{id:guid}/selling-rules", UpdateSellingRulesAsync).WithName("UpdateSellingRules").RequireOrganizer();

        // Split from the selling rules deliberately, and mapped as its own route rather than a mode
        // flag: the rules are Draft-only because they change what a ticket holder bought, and this
        // is editable for the life of the event because it does not. Two routes make that visible
        // in the route table instead of buried in a handler.
        group.MapPut("/{id:guid}/presentation", UpdateEventPresentationAsync).WithName("UpdateEventPresentation").RequireOrganizer();
        group.MapPut("/{id:guid}/slug", ChangeEventSlugAsync).WithName("ChangeEventSlug").RequireOrganizer();

        return app;
    }

    private static async Task<IResult> CreateEventAsync(
        CreateEventRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new CreateEventCommand(
            tenant.TenantId.Value,
            request.Title,
            request.Currency,
            request.StartsAt,
            request.EndsAt,
            request.DoorsOpenAt,
            request.BookingEndsAt,
            request.EventGroupId,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.OnSaleAt,
            request.TaxRatePercent,
            request.TaxLabel,
            request.BookingFeePerTicketMinor,
            request.Slug);

        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            CreateEventOutcome.Created => Results.Created($"/v1/events/{result.EventId}", new { id = result.EventId }),
            CreateEventOutcome.EventGroupNotFound => Results.NotFound(),
            CreateEventOutcome.OutsideEventGroupRange =>
                Results.Conflict(new { message = "The dates fall outside the tour's advertised range." }),
            CreateEventOutcome.OverlapsExistingLeg =>
                Results.Conflict(new { message = "Another leg of this tour is already running on those dates." }),
            _ => Results.Problem("Unexpected create-event outcome."),
        };
    }

    private static async Task<IResult> ListEventsAsync(
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken,
        EventStatus? status = null,
        int page = 1,
        int pageSize = 20,
        bool mine = false,
        Guid? eventGroupId = null)
    {
        if (mine && tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var query = new ListEventsQuery(tenant.TenantId, status, page, pageSize, mine, eventGroupId);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEventAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventQuery(id, tenant.TenantId), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetEventBySlugAsync(
        string slug,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventBySlugQuery(slug, tenant.TenantId), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> PublishEventAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new PublishEventCommand(id, tenant.TenantId.Value), cancellationToken);

        return result.Outcome switch
        {
            PublishEventOutcome.Published => Results.NoContent(),
            PublishEventOutcome.NotFound => Results.NotFound(),
            PublishEventOutcome.NotDraft =>
                Results.Conflict(new { message = "This event has already been published." }),

            // Every problem, not the first: an organizer fixing a three-night run needs to see all
            // three at once rather than a refresh apart.
            PublishEventOutcome.NoSellablePerformance =>
                Results.Conflict(new { message = "No performance is ready to sell.", problems = result.Problems }),
            _ => Results.Problem("Unexpected publish-event outcome."),
        };
    }

    private static Task<IResult> PauseSalesAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        ChangeEventSalesAsync(id, pause: true, tenant, sender, cancellationToken);

    private static Task<IResult> ResumeSalesAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        ChangeEventSalesAsync(id, pause: false, tenant, sender, cancellationToken);

    private static async Task<IResult> ChangeEventSalesAsync(
        Guid id,
        bool pause,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new ChangeEventSalesCommand(id, tenant.TenantId.Value, pause);
        var outcome = await sender.Send(command, cancellationToken);

        return outcome switch
        {
            ChangeEventSalesOutcome.Changed => Results.NoContent(),
            ChangeEventSalesOutcome.NotFound => Results.NotFound(),
            ChangeEventSalesOutcome.NotPublished =>
                Results.Conflict(new { message = "Only a published event's sales can be paused or resumed." }),
            _ => Results.Problem("Unexpected change-event-sales outcome."),
        };
    }

    private static async Task<IResult> UpdateSellingRulesAsync(
        Guid id,
        UpdateSellingRulesRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateSellingRulesCommand(
            id,
            tenant.TenantId.Value,
            request.OnSaleAt,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.TaxRatePercent,
            request.TaxLabel,
            request.BookingFeePerTicketMinor);

        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            UpdateSellingRulesOutcome.Updated => Results.NoContent(),
            UpdateSellingRulesOutcome.NotFound => Results.NotFound(),
            UpdateSellingRulesOutcome.NotDraft =>
                Results.Conflict(new { message = "A published event's selling rules cannot be changed." }),
            UpdateSellingRulesOutcome.Refused => Results.Conflict(new { message = result.Message }),
            _ => Results.Problem("Unexpected update-selling-rules outcome."),
        };
    }

    private static async Task<IResult> UpdateEventPresentationAsync(
        Guid id,
        UpdateEventPresentationRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateEventPresentationCommand(
            id,
            tenant.TenantId.Value,
            request.Title,
            request.Description,
            request.Category,
            request.AgeRestriction,
            request.BannerImageUrl,
            request.VideoUrl,
            request.ContactPhone,
            request.ContactMobile,
            request.ContactEmail,
            request.WebsiteUrl,
            (request.SocialLinks ?? []).Select(l => new SocialLinkInput(l.Platform, l.Url)).ToList());

        var outcome = await sender.Send(command, cancellationToken);
        return outcome switch
        {
            UpdateEventPresentationOutcome.Updated => Results.NoContent(),
            UpdateEventPresentationOutcome.NotFound => Results.NotFound(),
            _ => Results.Problem("Unexpected update-presentation outcome."),
        };
    }

    private static async Task<IResult> ChangeEventSlugAsync(
        Guid id,
        ChangeEventSlugRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var outcome = await sender.Send(new ChangeEventSlugCommand(id, tenant.TenantId.Value, request.Slug), cancellationToken);
        return outcome switch
        {
            ChangeEventSlugOutcome.Changed => Results.NoContent(),
            ChangeEventSlugOutcome.NotFound => Results.NotFound(),
            ChangeEventSlugOutcome.NotDraft =>
                Results.Conflict(new { message = "A published event's URL cannot be changed — it has already been shared." }),
            ChangeEventSlugOutcome.SlugTaken =>
                Results.Conflict(new { message = "Another event already uses that URL." }),
            _ => Results.Problem("Unexpected change-slug outcome."),
        };
    }
}
