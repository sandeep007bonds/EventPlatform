namespace Catalog.Application.Features.ListTicketTypes;

/// <summary>Handles <see cref="ListTicketTypesQuery"/>.</summary>
/// <param name="eventRepository">The event repository, to check tenant ownership.</param>
/// <param name="ticketTypeRepository">The ticket-type repository.</param>
internal sealed class ListTicketTypesHandler(
    IEventRepository eventRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<ListTicketTypesQuery, IReadOnlyList<TicketTypeResponse>?>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketTypeResponse>?> Handle(
        ListTicketTypesQuery request,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            // Null means "no such event, as far as you're concerned" — the endpoint turns it into a
            // 404 whether the event is missing or simply someone else's.
            return null;
        }

        var types = await ticketTypeRepository.ListForEventAsync(request.EventId, cancellationToken);

        return types
            .Select(type => new TicketTypeResponse(
                type.Id,
                type.Name,
                type.PriceMinor,
                type.Description,
                type.SalesStartsAt,
                type.SalesEndsAt,
                type.MaxPerBuyer,
                type.SortOrder,
                type.IsActive))
            .ToList();
    }
}
