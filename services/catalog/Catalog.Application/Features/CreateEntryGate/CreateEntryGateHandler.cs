namespace Catalog.Application.Features.CreateEntryGate;

/// <summary>Handles <see cref="CreateEntryGateCommand"/> by creating and persisting an entry gate.</summary>
/// <param name="eventRepository">The event repository, to check tenant ownership.</param>
/// <param name="entryGateRepository">The entry-gate repository.</param>
internal sealed class CreateEntryGateHandler(IEventRepository eventRepository, IEntryGateRepository entryGateRepository)
    : IRequestHandler<CreateEntryGateCommand, CreateEntryGateResult>
{
    /// <inheritdoc />
    public async Task<CreateEntryGateResult> Handle(CreateEntryGateCommand request, CancellationToken cancellationToken)
    {
        var @event = await eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event is null || @event.TenantId != request.TenantId)
        {
            return new CreateEntryGateResult(CreateEntryGateOutcome.EventNotFound, null);
        }

        var entryGate = EntryGate.Create(request.EventId, request.TenantId, request.Name);

        entryGateRepository.Add(entryGate);
        await entryGateRepository.SaveChangesAsync(cancellationToken);

        return new CreateEntryGateResult(CreateEntryGateOutcome.Created, entryGate.Id);
    }
}
