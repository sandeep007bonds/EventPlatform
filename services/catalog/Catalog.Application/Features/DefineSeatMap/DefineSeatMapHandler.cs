namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>
/// Handles <see cref="DefineSeatMapCommand"/> by generating and persisting a seat map for a
/// draft event, one time only.
/// </summary>
/// <param name="eventRepository">The event repository.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
/// <param name="entryGateRepository">The entry-gate repository, to validate section gate references.</param>
/// <param name="ticketTypes">Resolves each section's tier name to the ticket type it is sold as.</param>
internal sealed class DefineSeatMapHandler(
    IEventRepository eventRepository,
    ISeatMapRepository seatMapRepository,
    IEntryGateRepository entryGateRepository,
    TicketTypeResolver ticketTypes)
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

        var requestedGateIds = request.Sections
            .Where(s => s.EntryGateId is not null)
            .Select(s => s.EntryGateId!.Value)
            .ToHashSet();
        if (requestedGateIds.Count > 0)
        {
            var eventGates = await entryGateRepository.ListForEventAsync(request.EventId, cancellationToken);
            var knownGateIds = eventGates.Select(g => g.Id).ToHashSet();
            if (requestedGateIds.Any(id => !knownGateIds.Contains(id)))
            {
                return new DefineSeatMapResult(DefineSeatMapOutcome.EntryGateNotFound, null);
            }
        }

        var seatMap = SeatMap.Create(request.EventId, request.TenantId, request.Name);
        foreach (var section in request.Sections)
        {
            var ticketType = await ticketTypes.ResolveAsync(
                request.EventId,
                request.TenantId,
                section.PriceTier,
                section.PriceAmount,
                cancellationToken);

            if (section.AllocationType == AllocationType.Reserved)
            {
                seatMap.AddReservedSection(
                    section.Name,
                    ticketType,
                    section.Rows!.Value,
                    section.SeatsPerRow!.Value,
                    section.EntryGateId);
            }
            else
            {
                seatMap.AddGeneralAdmissionSection(
                    section.Name,
                    ticketType,
                    section.Capacity!.Value,
                    section.EntryGateId);
            }
        }

        seatMapRepository.Add(seatMap);
        await seatMapRepository.SaveChangesAsync(cancellationToken);

        return new DefineSeatMapResult(DefineSeatMapOutcome.Created, seatMap.Id);
    }
}
