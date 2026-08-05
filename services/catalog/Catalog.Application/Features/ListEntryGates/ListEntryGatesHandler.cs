namespace Catalog.Application.Features.ListEntryGates;

/// <summary>Handles <see cref="ListEntryGatesQuery"/>, mapping each gate to a read model.</summary>
/// <param name="repository">The entry-gate repository.</param>
internal sealed class ListEntryGatesHandler(IEntryGateRepository repository)
    : IRequestHandler<ListEntryGatesQuery, IReadOnlyList<EntryGateResponse>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EntryGateResponse>> Handle(ListEntryGatesQuery request, CancellationToken cancellationToken)
    {
        var gates = await repository.ListForEventAsync(request.EventId, cancellationToken);

        return gates.Select(g => new EntryGateResponse(g.Id, g.EventId, g.Name)).ToList();
    }
}
