namespace Venues.Application.Features.ListVenues;

/// <summary>Handles <see cref="ListVenuesQuery"/>.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class ListVenuesHandler(IVenueRepository repository)
    : IRequestHandler<ListVenuesQuery, IReadOnlyList<VenueSummaryResponse>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<VenueSummaryResponse>> Handle(
        ListVenuesQuery request,
        CancellationToken cancellationToken)
    {
        var venues = await repository.ListForTenantAsync(
            request.TenantId,
            request.IncludeArchived,
            cancellationToken);

        return venues.Select(venue => venue.ToSummary()).ToList();
    }
}
