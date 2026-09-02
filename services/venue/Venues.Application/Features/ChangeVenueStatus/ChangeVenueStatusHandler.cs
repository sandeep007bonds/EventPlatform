namespace Venues.Application.Features.ChangeVenueStatus;

/// <summary>Handles <see cref="ChangeVenueStatusCommand"/>.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class ChangeVenueStatusHandler(IVenueRepository repository)
    : IRequestHandler<ChangeVenueStatusCommand, ChangeVenueStatusOutcome>
{
    /// <inheritdoc />
    public async Task<ChangeVenueStatusOutcome> Handle(
        ChangeVenueStatusCommand request,
        CancellationToken cancellationToken)
    {
        var venue = await repository.GetTrackedByIdAsync(request.VenueId, cancellationToken);
        if (venue is null || venue.TenantId != request.TenantId)
        {
            return ChangeVenueStatusOutcome.NotFound;
        }

        if (request.Archive)
        {
            venue.Archive();
        }
        else if (venue.Status == VenueStatus.Archived)
        {
            return ChangeVenueStatusOutcome.AlreadyArchived;
        }
        else
        {
            venue.Activate();
        }

        await repository.SaveChangesAsync(cancellationToken);

        return ChangeVenueStatusOutcome.Changed;
    }
}
