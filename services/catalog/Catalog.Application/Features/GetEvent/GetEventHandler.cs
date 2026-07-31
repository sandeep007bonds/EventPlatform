namespace Catalog.Application.Features.GetEvent;

/// <summary>Handles <see cref="GetEventQuery"/>, mapping the aggregate to a read model.</summary>
/// <param name="repository">The event repository.</param>
internal sealed class GetEventHandler(IEventRepository repository)
    : IRequestHandler<GetEventQuery, EventResponse?>
{
    /// <inheritdoc />
    public async Task<EventResponse?> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        var @event = await repository.GetByIdAsync(request.Id, cancellationToken);

        return @event is null || !@event.IsVisibleTo(request.CallerTenantId)
            ? null
            : new EventResponse(
                @event.Id,
                @event.Title,
                @event.StartsAt,
                @event.Status.ToString(),
                @event.Currency,
                @event.EventGroupId,
                @event.Description,
                @event.Category,
                @event.EndsAt,
                @event.DoorsOpenAt,
                @event.OnSaleAt,
                @event.OffSaleAt,
                @event.AgeRestriction,
                @event.BannerImageUrl,
                @event.VideoUrl,
                @event.LocationName,
                @event.AddressLine1,
                @event.AddressLine2,
                @event.City,
                @event.Region,
                @event.PostalCode,
                @event.Country,
                @event.Latitude,
                @event.Longitude);
    }
}
