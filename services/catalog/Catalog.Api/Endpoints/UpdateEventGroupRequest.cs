namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for updating an event group. The tenant is taken from the caller's token, never
/// from this body (ADR-0011).
/// </summary>
/// <param name="Title">Group title.</param>
/// <param name="StartsAt">Overall advertised start of the tour, if known.</param>
/// <param name="EndsAt">Overall advertised end of the tour, if known.</param>
/// <param name="ContactPhone">Tour-wide default contact phone.</param>
/// <param name="ContactMobile">Tour-wide default contact mobile number.</param>
/// <param name="ContactEmail">Tour-wide default contact email.</param>
/// <param name="WebsiteUrl">Tour-wide default website URL.</param>
/// <param name="SocialLinks">Tour-wide default social links; replaces the existing list.</param>
public sealed record UpdateEventGroupRequest(
    string Title,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkRequest>? SocialLinks);
