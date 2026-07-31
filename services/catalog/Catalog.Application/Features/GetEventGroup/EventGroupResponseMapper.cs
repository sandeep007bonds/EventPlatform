namespace Catalog.Application.Features.GetEventGroup;

/// <summary>Maps an <see cref="EventGroup"/> to its <see cref="EventGroupResponse"/>.</summary>
public static class EventGroupResponseMapper
{
    /// <summary>Maps an event group to its read model.</summary>
    /// <param name="group">The event group.</param>
    /// <returns>The mapped <see cref="EventGroupResponse"/>.</returns>
    public static EventGroupResponse Map(EventGroup group) => new(
        group.Id,
        group.Title,
        group.StartsAt,
        group.EndsAt,
        group.ContactPhone,
        group.ContactMobile,
        group.ContactEmail,
        group.WebsiteUrl,
        group.SocialLinks.Select(l => new SocialLinkResponse(l.Platform, l.Url)).ToList());
}
