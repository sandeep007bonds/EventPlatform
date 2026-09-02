namespace Venues.Application.Features.StartSeatMapDraft;

/// <summary>Handles <see cref="StartSeatMapDraftCommand"/>.</summary>
/// <param name="repository">The seat-map repository.</param>
internal sealed class StartSeatMapDraftHandler(ISeatMapRepository repository)
    : IRequestHandler<StartSeatMapDraftCommand, StartSeatMapDraftResult>
{
    /// <inheritdoc />
    public async Task<StartSeatMapDraftResult> Handle(
        StartSeatMapDraftCommand request,
        CancellationToken cancellationToken)
    {
        var seatMap = await repository.GetTrackedByIdAsync(request.SeatMapId, cancellationToken);
        if (seatMap is null || seatMap.TenantId != request.TenantId)
        {
            return new StartSeatMapDraftResult(StartSeatMapDraftOutcome.NotFound, null);
        }

        if (seatMap.Draft is not null)
        {
            return new StartSeatMapDraftResult(StartSeatMapDraftOutcome.DraftAlreadyOpen, null);
        }

        var draft = seatMap.StartNewDraft();
        await repository.SaveChangesAsync(cancellationToken);

        return new StartSeatMapDraftResult(StartSeatMapDraftOutcome.Started, seatMap.ToResponse(draft));
    }
}
