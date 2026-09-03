namespace Catalog.Application.Features.CreateTicketType;

/// <summary>Handles <see cref="CreateTicketTypeCommand"/> by creating and persisting a ticket type.</summary>
/// <param name="eventRepository">The event repository, to check tenant ownership.</param>
/// <param name="ticketTypeRepository">The ticket-type repository.</param>
internal sealed class CreateTicketTypeHandler(
    IEventRepository eventRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<CreateTicketTypeCommand, CreateTicketTypeResult>
{
    /// <inheritdoc />
    public async Task<CreateTicketTypeResult> Handle(CreateTicketTypeCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            // Opaque not-found on a tenant mismatch — never reveal that another tenant's event
            // exists. Same pattern as CreatePromoCode.
            return new CreateTicketTypeResult(CreateTicketTypeOutcome.EventNotFound, null);
        }

        // Deliberately not restricted to a draft event. Adding a type to a published event is the
        // whole point of modelling this separately — an organizer opening a late release should not
        // have to create a second event. Note that until seat-map capacity can be added after
        // publish, a type created here has nothing referencing it and so sells nothing yet.
        var existing = await ticketTypeRepository.GetByNameAsync(request.EventId, request.Name, cancellationToken);
        if (existing is not null)
        {
            return new CreateTicketTypeResult(CreateTicketTypeOutcome.DuplicateName, null);
        }

        var ticketType = TicketType.Create(
            request.EventId,
            request.TenantId,
            request.Name,
            request.PriceMinor,
            request.Description,
            request.SalesStartsAt,
            request.SalesEndsAt,
            request.MaxPerBuyer,
            request.SortOrder);

        ticketTypeRepository.Add(ticketType);
        await ticketTypeRepository.SaveChangesAsync(cancellationToken);

        return new CreateTicketTypeResult(CreateTicketTypeOutcome.Created, ticketType.Id);
    }
}
