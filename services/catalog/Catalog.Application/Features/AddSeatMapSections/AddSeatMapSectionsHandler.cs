namespace Catalog.Application.Features.AddSeatMapSections;

/// <summary>
/// Handles <see cref="AddSeatMapSectionsCommand"/> by appending sections to a draft event's
/// existing seat map. <see cref="SeatMap.AddReservedSection"/>/<see cref="SeatMap.AddGeneralAdmissionSection"/>
/// already enforce section-name uniqueness against every section already in the map (not just the
/// ones in this request) — the same domain method <see cref="DefineSeatMap.DefineSeatMapHandler"/>
/// uses for the initial definition, just called again against a change-tracked load.
/// </summary>
/// <param name="eventRepository">The event repository.</param>
/// <param name="seatMapRepository">The seat-map repository.</param>
/// <param name="entryGateRepository">The entry-gate repository, to validate section gate references.</param>
/// <param name="ticketTypes">Resolves each section's tier name to the ticket type it is sold as.</param>
internal sealed class AddSeatMapSectionsHandler(
    IEventRepository eventRepository,
    ISeatMapRepository seatMapRepository,
    IEntryGateRepository entryGateRepository,
    TicketTypeResolver ticketTypes)
    : IRequestHandler<AddSeatMapSectionsCommand, AddSeatMapSectionsResult>
{
    /// <inheritdoc />
    public async Task<AddSeatMapSectionsResult> Handle(AddSeatMapSectionsCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new AddSeatMapSectionsResult(AddSeatMapSectionsOutcome.EventNotFound, null);
        }

        if (@event.Status != EventStatus.Draft)
        {
            return new AddSeatMapSectionsResult(AddSeatMapSectionsOutcome.EventNotDraft, null);
        }

        var seatMap = await seatMapRepository.GetTrackedByEventIdAsync(request.EventId, cancellationToken);
        if (seatMap is null)
        {
            return new AddSeatMapSectionsResult(AddSeatMapSectionsOutcome.SeatMapNotFound, null);
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
                return new AddSeatMapSectionsResult(AddSeatMapSectionsOutcome.EntryGateNotFound, null);
            }
        }

        try
        {
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
        }
        catch (InvalidOperationException)
        {
            // A duplicate section name — nothing has been saved yet (SaveChangesAsync below never
            // ran), so the in-memory additions from this loop are simply discarded.
            return new AddSeatMapSectionsResult(AddSeatMapSectionsOutcome.DuplicateSectionName, null);
        }

        await seatMapRepository.SaveChangesAsync(cancellationToken);

        return new AddSeatMapSectionsResult(AddSeatMapSectionsOutcome.Added, seatMap.Id);
    }
}
