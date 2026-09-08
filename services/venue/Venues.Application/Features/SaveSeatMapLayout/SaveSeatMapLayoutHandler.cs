namespace Venues.Application.Features.SaveSeatMapLayout;

/// <summary>
/// Handles <see cref="SaveSeatMapLayoutCommand"/>.
/// </summary>
/// <remarks>
/// A draft is allowed to be <i>incomplete</i> — half a stadium is a normal state to save and come
/// back to — so this does not run the publish validation. It rejects only what cannot be stored at
/// all: an element pointing at a section the layout does not contain, and a gate that is not this
/// venue's. Gates are checked here rather than at publish because the person who typed the wrong
/// gate is still looking at the screen.
/// </remarks>
/// <param name="seatMaps">The seat-map repository.</param>
/// <param name="venues">The venue repository, for validating gate references.</param>
internal sealed class SaveSeatMapLayoutHandler(ISeatMapRepository seatMaps, IVenueRepository venues)
    : IRequestHandler<SaveSeatMapLayoutCommand, SaveSeatMapLayoutResult>
{
    /// <inheritdoc />
    public async Task<SaveSeatMapLayoutResult> Handle(
        SaveSeatMapLayoutCommand request,
        CancellationToken cancellationToken)
    {
        var seatMap = await seatMaps.GetTrackedByIdAsync(request.SeatMapId, cancellationToken);
        if (seatMap is null || seatMap.TenantId != request.TenantId)
        {
            return new SaveSeatMapLayoutResult(SaveSeatMapLayoutOutcome.NotFound, null, null);
        }

        // Held rather than re-read: SaveDraftLayout mutates this same version in place, so the
        // reference stays valid and the compiler keeps its non-null state across the call.
        var draft = seatMap.Draft;
        if (draft is null)
        {
            return new SaveSeatMapLayoutResult(
                SaveSeatMapLayoutOutcome.NoOpenDraft,
                "This map has no open draft. Start a new version before editing it.",
                null);
        }

        var gateProblem = await FindGateProblemAsync(seatMap.VenueId, request.Layout, cancellationToken);
        if (gateProblem is not null)
        {
            return new SaveSeatMapLayoutResult(SaveSeatMapLayoutOutcome.UnknownGate, gateProblem, null);
        }

        try
        {
            seatMap.SaveDraftLayout(request.Layout);
        }
        catch (InvalidOperationException exception)
        {
            return new SaveSeatMapLayoutResult(SaveSeatMapLayoutOutcome.InvalidLayout, exception.Message, null);
        }

        // A layout is replaced wholesale, so this save deletes every row the draft had and writes
        // the new ones. Two saves of the same draft overlapping — Save clicked and then Publish
        // before the first returned — both delete the same rows, and the loser finds them already
        // gone. EF reports that as a concurrency conflict, and reporting it onward as a 500 blamed
        // the server for what is an ordinary lost race.
        if (!await seatMaps.TrySaveChangesAsync(cancellationToken))
        {
            // Held in a local because SA1118 forbids an argument that spans lines, and the message
            // does not fit on one.
            var message = "This draft was saved by another request a moment ago. Reopen the map "
                + "and re-apply your changes so you are editing what is actually stored.";

            return new SaveSeatMapLayoutResult(SaveSeatMapLayoutOutcome.ConcurrentEdit, message, null);
        }

        return new SaveSeatMapLayoutResult(SaveSeatMapLayoutOutcome.Saved, null, seatMap.ToResponse(draft));
    }

    private async Task<string?> FindGateProblemAsync(
        Guid venueId,
        SeatMapLayout layout,
        CancellationToken cancellationToken)
    {
        var gateIds = layout.Sections.Select(s => s.GateId)
            .Concat(layout.AdmissionAreas.Select(a => a.GateId))
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToHashSet();

        if (gateIds.Count == 0)
        {
            return null;
        }

        var venue = await venues.GetByIdAsync(venueId, cancellationToken);
        if (venue is null)
        {
            return "The venue this map belongs to no longer exists.";
        }

        var unknown = gateIds
            .Where(id => !venue.HasActiveGate(id))
            .Select(id => (Guid?)id)
            .FirstOrDefault();

        return unknown is null
            ? null
            : $"Gate '{unknown.Value}' is not an active gate at this venue.";
    }
}
