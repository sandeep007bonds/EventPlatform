namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>
/// Handles <see cref="DefineSeatMapCommand"/> by generating and persisting a seat map for a
/// draft event, one time only.
/// </summary>
/// <param name="eventRepository">The event repository.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
internal sealed class DefineSeatMapHandler(
    IEventRepository eventRepository,
    ISeatMapRepository seatMapRepository)
    : IRequestHandler<DefineSeatMapCommand, DefineSeatMapResult>
{
    /// <inheritdoc />
    public async Task<DefineSeatMapResult> Handle(DefineSeatMapCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new DefineSeatMapResult(DefineSeatMapOutcome.EventNotFound, null);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return new DefineSeatMapResult(DefineSeatMapOutcome.EventNotDraft, null);
        }

        var existing = await seatMapRepository.GetByEventIdAsync(request.EventId, cancellationToken);
        if (existing is not null)
        {
            return new DefineSeatMapResult(DefineSeatMapOutcome.AlreadyDefined, existing.Id);
        }

        var seatMap = SeatMap.Create(request.EventId, request.TenantId, request.Name);
        foreach (var section in request.Sections)
        {
            seatMap.AddSection(section.Name, section.PriceTier, section.PriceAmount, section.Rows, section.SeatsPerRow);
        }

        seatMapRepository.Add(seatMap);
        await seatMapRepository.SaveChangesAsync(cancellationToken);

        return new DefineSeatMapResult(DefineSeatMapOutcome.Created, seatMap.Id);
    }
}
