namespace Catalog.Application.Features.ListEvents;

/// <summary>Handles <see cref="ListEventsQuery"/>, mapping a page of events to read models.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class ListEventsHandler(IEventRepository repository)
    : IRequestHandler<ListEventsQuery, ListEventsResponse>
{
    /// <inheritdoc />
    public async Task<ListEventsResponse> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = request.Mine
            ? await repository.ListForTenantAsync(
                request.CallerTenantId!.Value,
                request.Status,
                request.EventGroupId,
                request.Page,
                request.PageSize,
                cancellationToken)
            : await repository.ListAsync(
                request.CallerTenantId,
                request.Status,
                request.EventGroupId,
                request.Page,
                request.PageSize,
                cancellationToken);

        var events = items
            .Select(e => new EventResponse(
                e.Id,
                e.Title,
                e.StartsAt,
                e.Status.ToString(),
                e.Currency,
                e.EventGroupId,
                e.Description,
                e.Category,
                e.EndsAt,
                e.DoorsOpenAt,
                e.OnSaleAt,
                e.OffSaleAt,
                e.AgeRestriction,
                e.BannerImageUrl,
                e.VideoUrl,
                e.LocationName,
                e.AddressLine1,
                e.AddressLine2,
                e.City,
                e.Region,
                e.PostalCode,
                e.Country,
                e.Latitude,
                e.Longitude))
            .ToList();

        return new ListEventsResponse(events, request.Page, request.PageSize, totalCount);
    }
}
