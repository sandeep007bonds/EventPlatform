namespace Catalog.Application.Features.GetEvent;

/// <summary>
/// Maps an <see cref="Event"/> to its <see cref="EventResponse"/>, resolving contact/social
/// fields against the owning <see cref="EventGroup"/>'s defaults when the leg sets none of its
/// own. Shared by <c>GetEvent</c> and <c>ListEvents</c> so the fallback rule lives in one place.
/// </summary>
public static class EventResponseMapper
{
    /// <summary>Maps an event to its read model.</summary>
    /// <param name="event">The event.</param>
    /// <param name="group">The owning event group, if <see cref="Event.EventGroupId"/> is set and it was found; otherwise <see langword="null"/>.</param>
    /// <returns>The mapped <see cref="EventResponse"/>.</returns>
    public static EventResponse Map(Event @event, EventGroup? group)
    {
        var socialLinks = @event.SocialLinks.Count > 0
            ? @event.SocialLinks.Select(l => new SocialLinkResponse(l.Platform, l.Url)).ToList()
            : group?.SocialLinks.Select(l => new SocialLinkResponse(l.Platform, l.Url)).ToList()
                ?? new List<SocialLinkResponse>();

        return new EventResponse(
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
            @event.BookingEndsAt,
            @event.MaxTicketsPerBuyer,
            @event.RequiresQueue,
            @event.TaxRatePercent,
            @event.TaxLabel,
            @event.SalesPaused,
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
            @event.Longitude,
            @event.ContactPhone ?? group?.ContactPhone,
            @event.ContactMobile ?? group?.ContactMobile,
            @event.ContactEmail ?? group?.ContactEmail,
            @event.WebsiteUrl ?? group?.WebsiteUrl,
            socialLinks);
    }
}
