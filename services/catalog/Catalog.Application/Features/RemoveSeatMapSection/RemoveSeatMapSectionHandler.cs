namespace Catalog.Application.Features.RemoveSeatMapSection;

/// <summary>Handles <see cref="RemoveSeatMapSectionCommand"/> by deleting the named section entirely.</summary>
/// <param name="eventRepository">The event repository.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
internal sealed class RemoveSeatMapSectionHandler(
    IEventRepository eventRepository,
    ISeatMapRepository seatMapRepository)
    : IRequestHandler<RemoveSeatMapSectionCommand, RemoveSeatMapSectionResult>
{
    /// <inheritdoc />
    public async Task<RemoveSeatMapSectionResult> Handle(RemoveSeatMapSectionCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new RemoveSeatMapSectionResult(RemoveSeatMapSectionOutcome.EventNotFound);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return new RemoveSeatMapSectionResult(RemoveSeatMapSectionOutcome.EventNotDraft);
        }

        var seatMap = await seatMapRepository.GetTrackedByEventIdAsync(request.EventId, cancellationToken);
        if (seatMap is null)
        {
            return new RemoveSeatMapSectionResult(RemoveSeatMapSectionOutcome.SeatMapNotFound);
        }

        try
        {
            seatMap.RemoveSection(request.SectionName);
        }
        catch (InvalidOperationException)
        {
            return new RemoveSeatMapSectionResult(RemoveSeatMapSectionOutcome.SectionNotFound);
        }

        await seatMapRepository.SaveChangesAsync(cancellationToken);

        return new RemoveSeatMapSectionResult(RemoveSeatMapSectionOutcome.Removed);
    }
}
