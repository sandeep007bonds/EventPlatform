namespace Catalog.Application.Features.SetSessionAllocations;

/// <summary>
/// Handles <see cref="SetSessionAllocationsCommand"/>.
/// </summary>
/// <remarks>
/// Two cross-aggregate checks that the session itself cannot make, and both of them catch a mistake
/// that would otherwise surface as missing inventory long after the fact: every code must exist in
/// the pinned seat-map version (Venue owns the codes), and every ticket type must belong to this
/// event and still be active (Catalog owns the types, but they are a different aggregate).
/// <para>
/// A capacity cap is checked here for the same reason — whether a block is an admission area, and
/// how many it holds, are facts about the venue, so only the caller holding the version can say
/// whether the number makes sense.
/// </para>
/// </remarks>
/// <param name="repository">The event repository.</param>
/// <param name="ticketTypes">The ticket-type repository.</param>
/// <param name="venue">The Venue service client.</param>
internal sealed class SetSessionAllocationsHandler(
    IEventRepository repository,
    ITicketTypeRepository ticketTypes,
    IVenueClient venue)
    : IRequestHandler<SetSessionAllocationsCommand, SessionCommandResult>
{
    /// <inheritdoc />
    public async Task<SessionCommandResult> Handle(
        SetSessionAllocationsCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return SessionCommandResult.NotFound();
        }

        var session = @event.FindSession(request.EventSessionId);
        if (session is null)
        {
            return SessionCommandResult.NotFound();
        }

        if (session.SeatMapId is not { } seatMapId)
        {
            return SessionCommandResult.Refused(
                "This performance has no seat map yet, so there are no blocks to allocate.");
        }

        var version = await venue.GetSeatMapVersionAsync(seatMapId, session.SeatMapVersionNumber, cancellationToken);
        if (version is null)
        {
            return SessionCommandResult.Refused("This performance's seat map could no longer be read from the venue.");
        }

        var blockRefusal = CheckBlocks(request, version);
        if (blockRefusal is not null)
        {
            return SessionCommandResult.Refused(blockRefusal);
        }

        var refusal = await CheckTicketTypesAsync(request, cancellationToken);
        if (refusal is not null)
        {
            return SessionCommandResult.Refused(refusal);
        }

        try
        {
            session.SetAllocations(request.Allocations.Select(a => new SessionAllocationSpec(
                a.Code,
                a.TicketTypeId,
                a.IsExcluded,
                a.DisplayName,
                a.CapacityOverride)));
            await repository.SaveChangesAsync(cancellationToken);

            return SessionCommandResult.Ok(session.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }
    }

    private static string? CheckBlocks(SetSessionAllocationsCommand request, SeatMapVersionSnapshot version)
    {
        foreach (var allocation in request.Allocations)
        {
            if (!version.Blocks.TryGetValue(allocation.Code, out var block))
            {
                return $"'{allocation.Code}' is not a block in this performance's seat map.";
            }

            if (allocation.CapacityOverride is not { } capacity)
            {
                continue;
            }

            // Refused rather than clamped or reinterpreted. A section holds named seats, so "sell
            // 200" leaves the other 200 unnamed and nothing downstream could pick them; blocking
            // the seats you mean is the operation that exists for that.
            if (!block.IsAdmissionArea)
            {
                return $"'{allocation.Code}' has reserved seats, so it cannot be capped to a number. "
                    + "Block the individual seats you are holding back instead.";
            }

            if (capacity > block.Capacity)
            {
                return $"'{allocation.Code}' holds {block.Capacity}, so it cannot sell {capacity}.";
            }
        }

        return null;
    }

    private async Task<string?> CheckTicketTypesAsync(
        SetSessionAllocationsCommand request,
        CancellationToken cancellationToken)
    {
        var types = await ticketTypes.ListForEventAsync(request.EventId, cancellationToken);
        var usable = types.Where(t => t.IsActive).Select(t => t.Id).ToHashSet();

        // An excluded block names no type, so there is nothing here to check for it.
        var unknown = request.Allocations
            .Where(a => a.TicketTypeId is { } id && !usable.Contains(id))
            .Select(a => a.TicketTypeId)
            .FirstOrDefault();

        return unknown is null
            ? null
            : $"Ticket type '{unknown.Value}' does not belong to this event, or is no longer active.";
    }
}
