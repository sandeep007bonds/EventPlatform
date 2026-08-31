namespace Catalog.Application.Features.DeactivateTicketType;

/// <summary>Handles <see cref="DeactivateTicketTypeCommand"/>.</summary>
/// <param name="eventRepository">The event repository, to check tenant ownership.</param>
/// <param name="ticketTypeRepository">The ticket-type repository.</param>
internal sealed class DeactivateTicketTypeHandler(
    IEventRepository eventRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<DeactivateTicketTypeCommand, DeactivateTicketTypeOutcome>
{
    /// <inheritdoc />
    public async Task<DeactivateTicketTypeOutcome> Handle(
        DeactivateTicketTypeCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return DeactivateTicketTypeOutcome.NotFound;
        }

        var ticketType = await ticketTypeRepository.GetByIdAsync(request.TicketTypeId, cancellationToken);
        if (ticketType is null || ticketType.EventId != request.EventId)
        {
            return DeactivateTicketTypeOutcome.NotFound;
        }

        // Idempotent: deactivating an already-inactive type is a success, not an error. The caller
        // asked for a state, and it holds.
        ticketType.Deactivate();
        await ticketTypeRepository.SaveChangesAsync(cancellationToken);

        return DeactivateTicketTypeOutcome.Deactivated;
    }
}
