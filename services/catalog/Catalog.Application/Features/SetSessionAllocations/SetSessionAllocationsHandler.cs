namespace Catalog.Application.Features.SetSessionAllocations;

/// <summary>
/// Handles <see cref="SetSessionAllocationsCommand"/>.
/// </summary>
/// <remarks>
/// Two cross-aggregate checks that the session itself cannot make, and both of them catch a mistake
/// that would otherwise surface as missing inventory long after the fact: every code must exist in
/// the pinned seat-map version (Venue owns the codes), and every ticket type must belong to this
/// event and still be active (Catalog owns the types, but they are a different aggregate).
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

        var unknownCode = request.Allocations.FirstOrDefault(a => !version.BlockCodes.Contains(a.Code));
        if (unknownCode is not null)
        {
            return SessionCommandResult.Refused(
                $"'{unknownCode.Code}' is not a block in this performance's seat map.");
        }

        var refusal = await CheckTicketTypesAsync(request, cancellationToken);
        if (refusal is not null)
        {
            return SessionCommandResult.Refused(refusal);
        }

        try
        {
            session.SetAllocations(request.Allocations.Select(a => (a.Code, a.TicketTypeId)));
            await repository.SaveChangesAsync(cancellationToken);

            return SessionCommandResult.Ok(session.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return SessionCommandResult.Refused(exception.Message);
        }
    }

    private async Task<string?> CheckTicketTypesAsync(
        SetSessionAllocationsCommand request,
        CancellationToken cancellationToken)
    {
        var types = await ticketTypes.ListForEventAsync(request.EventId, cancellationToken);
        var usable = types.Where(t => t.IsActive).Select(t => t.Id).ToHashSet();

        var unknown = request.Allocations
            .Where(a => !usable.Contains(a.TicketTypeId))
            .Select(a => (Guid?)a.TicketTypeId)
            .FirstOrDefault();

        return unknown is null
            ? null
            : $"Ticket type '{unknown.Value}' does not belong to this event, or is no longer active.";
    }
}
