namespace Catalog.Application.Features.UpdateSeatMapSection;

/// <summary>
/// Handles <see cref="UpdateSeatMapSectionCommand"/> by removing the named section and re-adding
/// it with the new definition — a section's rows/capacity can only be expressed by regenerating
/// its seats/pool, so a genuine in-place edit isn't meaningfully different from remove-then-add,
/// and reusing it means duplicate-name detection (<see cref="SeatMap.AddReservedSection"/>/
/// <see cref="SeatMap.AddGeneralAdmissionSection"/>) needs no special-casing for the "same name,
/// no rename" case — freeing the name via <see cref="SeatMap.RemoveSection"/> first makes it
/// available again either way.
/// </summary>
/// <param name="eventRepository">The event repository.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
/// <param name="entryGateRepository">The entry-gate repository, to validate the section's gate reference.</param>
internal sealed class UpdateSeatMapSectionHandler(
    IEventRepository eventRepository,
    ISeatMapRepository seatMapRepository,
    IEntryGateRepository entryGateRepository)
    : IRequestHandler<UpdateSeatMapSectionCommand, UpdateSeatMapSectionResult>
{
    /// <inheritdoc />
    public async Task<UpdateSeatMapSectionResult> Handle(UpdateSeatMapSectionCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.EventNotFound, null);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.EventNotDraft, null);
        }

        var seatMap = await seatMapRepository.GetTrackedByEventIdAsync(request.EventId, cancellationToken);
        if (seatMap is null)
        {
            return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.SeatMapNotFound, null);
        }

        if (request.Section.EntryGateId is { } entryGateId)
        {
            var eventGates = await entryGateRepository.ListForEventAsync(request.EventId, cancellationToken);
            if (!eventGates.Any(g => g.Id == entryGateId))
            {
                return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.EntryGateNotFound, null);
            }
        }

        try
        {
            seatMap.RemoveSection(request.CurrentSectionName);
        }
        catch (InvalidOperationException)
        {
            return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.SectionNotFound, null);
        }

        try
        {
            var section = request.Section;
            if (section.AllocationType == AllocationType.Reserved)
            {
                seatMap.AddReservedSection(section.Name, section.PriceTier, section.PriceAmount, section.Rows!.Value, section.SeatsPerRow!.Value, section.EntryGateId);
            }
            else
            {
                seatMap.AddGeneralAdmissionSection(section.Name, section.PriceTier, section.PriceAmount, section.Capacity!.Value, section.EntryGateId);
            }
        }
        catch (InvalidOperationException)
        {
            // The new name collides with a different section still in the map — nothing has been
            // saved yet, so the in-memory remove+failed-add from this call is simply discarded
            // (SaveChangesAsync below never ran).
            return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.DuplicateSectionName, null);
        }

        await seatMapRepository.SaveChangesAsync(cancellationToken);

        return new UpdateSeatMapSectionResult(UpdateSeatMapSectionOutcome.Updated, seatMap.Id);
    }
}
