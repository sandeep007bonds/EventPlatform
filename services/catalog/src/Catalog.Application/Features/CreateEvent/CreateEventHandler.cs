namespace Catalog.Application.Features.CreateEvent;

/// <summary>Handles <see cref="CreateEventCommand"/> by creating and persisting a draft event.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class CreateEventHandler(IEventRepository repository)
    : IRequestHandler<CreateEventCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var @event = Event.Create(
            request.TenantId,
            request.VenueId,
            request.Title,
            request.StartsAt,
            request.Currency);

        repository.Add(@event);
        await repository.SaveChangesAsync(cancellationToken);

        return @event.Id;
    }
}
