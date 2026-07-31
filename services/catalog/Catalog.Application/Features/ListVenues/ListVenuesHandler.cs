namespace Catalog.Application.Features.ListVenues;

/// <summary>Handles <see cref="ListVenuesQuery"/>, mapping a page of venues to read models.</summary>
/// <param name="repository">The venue repository.</param>
internal sealed class ListVenuesHandler(IVenueRepository repository)
    : IRequestHandler<ListVenuesQuery, ListVenuesResponse>
{
    /// <inheritdoc />
    public async Task<ListVenuesResponse> Handle(ListVenuesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.ListForTenantAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var venues = items
            .Select(v => new VenueResponse(
                v.Id,
                v.Name,
                v.AddressLine1,
                v.AddressLine2,
                v.City,
                v.Region,
                v.PostalCode,
                v.Country,
                v.Latitude,
                v.Longitude,
                v.Capacity))
            .ToList();

        return new ListVenuesResponse(venues, request.Page, request.PageSize, totalCount);
    }
}
