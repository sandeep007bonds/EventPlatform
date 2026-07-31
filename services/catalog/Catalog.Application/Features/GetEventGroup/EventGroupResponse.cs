namespace Catalog.Application.Features.GetEventGroup;

/// <summary>Read model returned for a single event group.</summary>
/// <param name="Id">Event group id.</param>
/// <param name="Title">Group title.</param>
/// <param name="StartsAt">Overall advertised start of the tour, if known.</param>
/// <param name="EndsAt">Overall advertised end of the tour, if known.</param>
/// <param name="ContactPhone">Tour-wide default contact phone.</param>
/// <param name="ContactMobile">Tour-wide default contact mobile number.</param>
/// <param name="ContactEmail">Tour-wide default contact email.</param>
/// <param name="WebsiteUrl">Tour-wide default website URL.</param>
/// <param name="SocialLinks">Tour-wide default social links.</param>
public sealed record EventGroupResponse(
    Guid Id,
    string Title,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkResponse> SocialLinks);
