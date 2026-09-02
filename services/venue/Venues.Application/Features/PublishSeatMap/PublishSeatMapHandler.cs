namespace Venues.Application.Features.PublishSeatMap;

/// <summary>
/// Handles <see cref="PublishSeatMapCommand"/> by validating the draft, freezing it, superseding
/// whatever was live, and enqueuing a <see cref="SeatMapPublished"/> integration event in the same
/// unit of work.
/// </summary>
/// <remarks>
/// Validation runs here rather than being left to the aggregate's own guard, so a failure comes
/// back as the full list of problems and a 409 — not as an exception carrying one sentence.
/// </remarks>
/// <param name="seatMaps">The seat-map repository.</param>
/// <param name="venues">The venue repository, for re-checking gate references.</param>
/// <param name="events">The integration-event publisher (transactional outbox).</param>
internal sealed class PublishSeatMapHandler(
    ISeatMapRepository seatMaps,
    IVenueRepository venues,
    IEventPublisher events)
    : IRequestHandler<PublishSeatMapCommand, PublishSeatMapResult>
{
    /// <inheritdoc />
    public async Task<PublishSeatMapResult> Handle(
        PublishSeatMapCommand request,
        CancellationToken cancellationToken)
    {
        var seatMap = await seatMaps.GetTrackedByIdAsync(request.SeatMapId, cancellationToken);
        if (seatMap is null || seatMap.TenantId != request.TenantId)
        {
            return new PublishSeatMapResult(PublishSeatMapOutcome.NotFound, null, null, []);
        }

        var draft = seatMap.Draft;
        if (draft is null)
        {
            return new PublishSeatMapResult(PublishSeatMapOutcome.NoOpenDraft, null, null, []);
        }

        var errors = draft.Validate().ToList();
        errors.AddRange(await FindGateErrorsAsync(seatMap.VenueId, draft, cancellationToken));

        if (errors.Count > 0)
        {
            return new PublishSeatMapResult(PublishSeatMapOutcome.Invalid, null, null, errors);
        }

        seatMap.PublishDraft(DateTimeOffset.UtcNow);

        events.Enqueue(new SeatMapPublished(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            seatMap.TenantId,
            seatMap.VenueId,
            seatMap.Id,
            draft.Id,
            draft.VersionNumber,
            draft.Capacity));

        await seatMaps.SaveChangesAsync(cancellationToken);

        return new PublishSeatMapResult(
            PublishSeatMapOutcome.Published,
            draft.VersionNumber,
            draft.Capacity,
            []);
    }

    // Re-checked at publish as well as at save: a gate can be deactivated between the two, and
    // freezing a map that routes a section through a gate nobody opens is not a state to reach.
    private async Task<IReadOnlyList<SeatMapValidationError>> FindGateErrorsAsync(
        Guid venueId,
        SeatMapVersion draft,
        CancellationToken cancellationToken)
    {
        var gateIds = draft.ReferencedGateIds();
        if (gateIds.Count == 0)
        {
            return [];
        }

        var venue = await venues.GetByIdAsync(venueId, cancellationToken);
        if (venue is null)
        {
            return [new SeatMapValidationError("venue_missing", "The venue this map belongs to no longer exists.")];
        }

        return gateIds
            .Where(id => !venue.HasActiveGate(id))
            .Select(id => new SeatMapValidationError(
                "unknown_gate",
                $"Gate '{id}' is not an active gate at this venue."))
            .ToList();
    }
}
