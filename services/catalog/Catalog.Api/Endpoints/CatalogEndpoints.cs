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
        group.MapGet("/{id:guid}/seatmap", GetSeatMapAsync).WithName("GetSeatMap").AllowAnonymous();

        // Organizer writes. The policy establishes only that the caller is an organizer at all —
        // each handler still checks that this event belongs to their tenant, and answers a
        // mismatch with an opaque 404.
        group.MapPost("/", CreateEventAsync).WithName("CreateEvent").RequireOrganizer();
        group.MapPost("/{id:guid}/publish", PublishEventAsync).WithName("PublishEvent").RequireOrganizer();
        group.MapPost("/{id:guid}/pause-sales", PauseSalesAsync).WithName("PauseSales").RequireOrganizer();
        group.MapPost("/{id:guid}/resume-sales", ResumeSalesAsync).WithName("ResumeSales").RequireOrganizer();
        group.MapPost("/{id:guid}/seatmap", DefineSeatMapAsync).WithName("DefineSeatMap").RequireOrganizer();
        group.MapPost("/{id:guid}/seatmap/sections", AddSeatMapSectionsAsync).WithName("AddSeatMapSections").RequireOrganizer();
        group.MapPut("/{id:guid}/seatmap/sections", UpdateSeatMapSectionAsync).WithName("UpdateSeatMapSection").RequireOrganizer();
        group.MapDelete("/{id:guid}/seatmap/sections/{sectionName}", RemoveSeatMapSectionAsync).WithName("RemoveSeatMapSection").RequireOrganizer();
        group.MapPut("/{id:guid}/details", UpdateEventDetailsAsync).WithName("UpdateEventDetails").RequireOrganizer();

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
            request.StartsAt,
            request.EndsAt,
            request.Currency,
            request.LocationName,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.Country,
            request.Latitude,
            request.Longitude,
            request.EventGroupId,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.TaxRatePercent,
            request.TaxLabel,
            request.BookingFeePerTicketMinor,
            request.TimeZoneId);

        var result = await sender.Send(command, cancellationToken);
        return result.Outcome switch
        {
            CreateEventOutcome.Created => Results.Created($"/v1/events/{result.EventId}", new { id = result.EventId }),
            CreateEventOutcome.EventGroupNotFound => Results.NotFound(new { message = "No matching tour exists." }),
            CreateEventOutcome.OutsideEventGroupRange =>
                Results.Conflict(new { message = "The event's dates fall outside the tour's advertised date range." }),
            CreateEventOutcome.OverlapsExistingLeg =>
                Results.Conflict(new { message = "The event's dates overlap another leg of the same tour." }),
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

        var outcome = await sender.Send(new PublishEventCommand(id, tenant.TenantId.Value), cancellationToken);
        return outcome switch
        {
            PublishEventOutcome.Published => Results.NoContent(),
            PublishEventOutcome.NotFound => Results.NotFound(),
            PublishEventOutcome.NoSeatMap => Results.Conflict(new { message = "Define a seat map before publishing the event." }),
            PublishEventOutcome.NotDraft => Results.Conflict(new { message = "Only a draft event can be published." }),
            _ => Results.Problem("Unexpected publish outcome."),
        };
    }

    private static async Task<IResult> PauseSalesAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var outcome = await sender.Send(new PauseSalesCommand(id, tenant.TenantId.Value), cancellationToken);
        return outcome switch
        {
            PauseSalesOutcome.Paused => Results.NoContent(),
            PauseSalesOutcome.NotFound => Results.NotFound(),
            PauseSalesOutcome.NotPublished => Results.Conflict(new { message = "Only a published event's sales can be paused." }),
            PauseSalesOutcome.AlreadyPaused => Results.Conflict(new { message = "Sales are already paused for this event." }),
            _ => Results.Problem("Unexpected pause-sales outcome."),
        };
    }

    private static async Task<IResult> ResumeSalesAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var outcome = await sender.Send(new ResumeSalesCommand(id, tenant.TenantId.Value), cancellationToken);
        return outcome switch
        {
            ResumeSalesOutcome.Resumed => Results.NoContent(),
            ResumeSalesOutcome.NotFound => Results.NotFound(),
            ResumeSalesOutcome.NotPublished => Results.Conflict(new { message = "Only a published event's sales can be resumed." }),
            ResumeSalesOutcome.NotPaused => Results.Conflict(new { message = "Sales are not paused for this event." }),
            _ => Results.Problem("Unexpected resume-sales outcome."),
        };
    }

    private static async Task<IResult> DefineSeatMapAsync(
        Guid id,
        DefineSeatMapRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var sections = request.Sections
            .Select(s => new SeatMapSectionInput(s.Name, s.PriceTier, s.PriceAmount, s.AllocationType, s.Rows, s.SeatsPerRow, s.Capacity, s.EntryGateId))
            .ToList();

        var command = new DefineSeatMapCommand(id, tenant.TenantId.Value, request.Name, sections);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            DefineSeatMapOutcome.Created =>
                Results.Created($"/v1/events/{id}/seatmap", new { seatMapId = result.SeatMapId }),
            DefineSeatMapOutcome.EventNotFound => Results.NotFound(),
            DefineSeatMapOutcome.EventNotDraft =>
                Results.Conflict(new { message = "The event is not a draft; its seat map can no longer be changed." }),
            DefineSeatMapOutcome.AlreadyDefined =>
                Results.Conflict(new { message = "A seat map already exists for this event." }),
            DefineSeatMapOutcome.EntryGateNotFound =>
                Results.BadRequest(new { message = "One or more sections reference an entry gate that doesn't exist for this event." }),
            _ => Results.Problem("Unexpected seat-map outcome."),
        };
    }

    private static async Task<IResult> AddSeatMapSectionsAsync(
        Guid id,
        AddSeatMapSectionsRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var sections = request.Sections
            .Select(s => new SeatMapSectionInput(s.Name, s.PriceTier, s.PriceAmount, s.AllocationType, s.Rows, s.SeatsPerRow, s.Capacity, s.EntryGateId))
            .ToList();

        var command = new AddSeatMapSectionsCommand(id, tenant.TenantId.Value, sections);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            AddSeatMapSectionsOutcome.Added =>
                Results.Ok(new { seatMapId = result.SeatMapId }),
            AddSeatMapSectionsOutcome.EventNotFound => Results.NotFound(),
            AddSeatMapSectionsOutcome.SeatMapNotFound =>
                Results.NotFound(new { message = "Define a seat map before adding more sections to it." }),
            AddSeatMapSectionsOutcome.EventNotDraft =>
                Results.Conflict(new { message = "The event is not a draft; its seat map can no longer be changed." }),
            AddSeatMapSectionsOutcome.DuplicateSectionName =>
                Results.Conflict(new { message = "A section with that name already exists in this seat map." }),
            AddSeatMapSectionsOutcome.EntryGateNotFound =>
                Results.BadRequest(new { message = "One or more sections reference an entry gate that doesn't exist for this event." }),
            _ => Results.Problem("Unexpected seat-map outcome."),
        };
    }

    private static async Task<IResult> UpdateSeatMapSectionAsync(
        Guid id,
        UpdateSeatMapSectionRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var section = new SeatMapSectionInput(
            request.Section.Name,
            request.Section.PriceTier,
            request.Section.PriceAmount,
            request.Section.AllocationType,
            request.Section.Rows,
            request.Section.SeatsPerRow,
            request.Section.Capacity,
            request.Section.EntryGateId);

        var command = new UpdateSeatMapSectionCommand(id, tenant.TenantId.Value, request.CurrentSectionName, section);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            UpdateSeatMapSectionOutcome.Updated =>
                Results.Ok(new { seatMapId = result.SeatMapId }),
            UpdateSeatMapSectionOutcome.EventNotFound => Results.NotFound(),
            UpdateSeatMapSectionOutcome.SeatMapNotFound =>
                Results.NotFound(new { message = "This event has no seat map yet." }),
            UpdateSeatMapSectionOutcome.SectionNotFound =>
                Results.NotFound(new { message = "No section with that name exists in this seat map." }),
            UpdateSeatMapSectionOutcome.EventNotDraft =>
                Results.Conflict(new { message = "The event is not a draft; its seat map can no longer be changed." }),
            UpdateSeatMapSectionOutcome.DuplicateSectionName =>
                Results.Conflict(new { message = "A different section with that name already exists in this seat map." }),
            UpdateSeatMapSectionOutcome.EntryGateNotFound =>
                Results.BadRequest(new { message = "The section references an entry gate that doesn't exist for this event." }),
            _ => Results.Problem("Unexpected seat-map outcome."),
        };
    }

    private static async Task<IResult> RemoveSeatMapSectionAsync(
        Guid id,
        string sectionName,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new RemoveSeatMapSectionCommand(id, tenant.TenantId.Value, sectionName);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            RemoveSeatMapSectionOutcome.Removed => Results.NoContent(),
            RemoveSeatMapSectionOutcome.EventNotFound => Results.NotFound(),
            RemoveSeatMapSectionOutcome.SeatMapNotFound =>
                Results.NotFound(new { message = "This event has no seat map yet." }),
            RemoveSeatMapSectionOutcome.SectionNotFound =>
                Results.NotFound(new { message = "No section with that name exists in this seat map." }),
            RemoveSeatMapSectionOutcome.EventNotDraft =>
                Results.Conflict(new { message = "The event is not a draft; its seat map can no longer be changed." }),
            _ => Results.Problem("Unexpected seat-map outcome."),
        };
    }

    private static async Task<IResult> GetSeatMapAsync(
        Guid id,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSeatMapQuery(id, tenant.TenantId), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> UpdateEventDetailsAsync(
        Guid id,
        UpdateEventDetailsRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var command = new UpdateEventDetailsCommand(
            id,
            tenant.TenantId.Value,
            request.Description,
            request.Category,
            request.EndsAt,
            request.DoorsOpenAt,
            request.OnSaleAt,
            request.BookingEndsAt,
            request.MaxTicketsPerBuyer,
            request.RequiresQueue,
            request.TaxRatePercent,
            request.TaxLabel,
            request.BookingFeePerTicketMinor,
            request.TimeZoneId,
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
            UpdateEventDetailsOutcome.Updated => Results.NoContent(),
            UpdateEventDetailsOutcome.NotFound => Results.NotFound(),
            UpdateEventDetailsOutcome.NotDraft =>
                Results.Conflict(new { message = "Only a draft event's details can be changed." }),
            UpdateEventDetailsOutcome.BookingCutoffAfterStart =>
                Results.Conflict(new { message = "The booking cutoff must not be later than the event's start time." }),
            UpdateEventDetailsOutcome.OutsideEventGroupRange =>
                Results.Conflict(new { message = "The event's dates fall outside the tour's advertised date range." }),
            UpdateEventDetailsOutcome.OverlapsExistingLeg =>
                Results.Conflict(new { message = "The event's dates overlap another leg of the same tour." }),
            _ => Results.Problem("Unexpected update-details outcome."),
        };
    }
}
