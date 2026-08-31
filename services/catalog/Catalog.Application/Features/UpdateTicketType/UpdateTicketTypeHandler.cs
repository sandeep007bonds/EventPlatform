namespace Catalog.Application.Features.UpdateTicketType;

/// <summary>Handles <see cref="UpdateTicketTypeCommand"/>.</summary>
/// <param name="eventRepository">The event repository, to check tenant ownership and draft status.</param>
/// <param name="ticketTypeRepository">The ticket-type repository.</param>
internal sealed class UpdateTicketTypeHandler(
    IEventRepository eventRepository,
    ITicketTypeRepository ticketTypeRepository)
    : IRequestHandler<UpdateTicketTypeCommand, UpdateTicketTypeOutcome>
{
    /// <inheritdoc />
    public async Task<UpdateTicketTypeOutcome> Handle(
        UpdateTicketTypeCommand request,
        CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return UpdateTicketTypeOutcome.NotFound;
        }

        var ticketType = await ticketTypeRepository.GetByIdAsync(request.TicketTypeId, cancellationToken);
        if (ticketType is null || ticketType.EventId != request.EventId)
        {
            // The EventId check is what stops a type id from one of the caller's events being
            // updated through another's route — the tenant check above passes in that case.
            return UpdateTicketTypeOutcome.NotFound;
        }

        // A type keeping its own name is not a duplicate of itself.
        var sameName = await ticketTypeRepository.GetByNameAsync(request.EventId, request.Name, cancellationToken);
        if (sameName is not null && sameName.Id != ticketType.Id)
        {
            return UpdateTicketTypeOutcome.DuplicateName;
        }

        var isDraft = @event.Status == EventStatus.Draft;
        var repricing = request.PriceMinor != ticketType.PriceMinor;
        if (repricing && !isDraft)
        {
            // Refused rather than silently ignored. Inventory holds its own copy of the price taken
            // at provisioning time, so until that copy can be updated, accepting this would move the
            // storefront's number while the charged number stayed put — a silent divergence, about
            // money. Everything else in the request is still safe, but applying half an update the
            // caller asked for is its own trap, so the whole thing is rejected.
            return UpdateTicketTypeOutcome.PriceLockedAfterPublish;
        }

        ticketType.Rename(request.Name);
        if (repricing)
        {
            ticketType.Reprice(request.PriceMinor, isDraft);
        }

        ticketType.UpdateRules(
            request.Description,
            request.SalesStartsAt,
            request.SalesEndsAt,
            request.MaxPerBuyer,
            request.SortOrder);

        await ticketTypeRepository.SaveChangesAsync(cancellationToken);
        return UpdateTicketTypeOutcome.Updated;
    }
}
