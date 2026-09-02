namespace Venues.Application.Features.AddVenueGate;

/// <summary>Handles <see cref="AddVenueGateCommand"/>.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class AddVenueGateHandler(IVenueRepository repository)
    : IRequestHandler<AddVenueGateCommand, AddVenueGateResult>
{
    /// <inheritdoc />
    public async Task<AddVenueGateResult> Handle(AddVenueGateCommand request, CancellationToken cancellationToken)
    {
        var venue = await repository.GetTrackedByIdAsync(request.VenueId, cancellationToken);
        if (venue is null || venue.TenantId != request.TenantId)
        {
            return new AddVenueGateResult(AddVenueGateOutcome.VenueNotFound, null);
        }

        // The duplicate-code rule lives in the aggregate, which is where it can see every gate.
        // Catching it here turns an invariant into an HTTP status rather than a 500, without the
        // handler having to re-implement the check and risk the two disagreeing.
        try
        {
            var gate = venue.AddGate(request.Code, request.Name);
            await repository.SaveChangesAsync(cancellationToken);

            return new AddVenueGateResult(AddVenueGateOutcome.Added, gate.Id);
        }
        catch (InvalidOperationException)
        {
            return new AddVenueGateResult(AddVenueGateOutcome.DuplicateCode, null);
        }
    }
}
