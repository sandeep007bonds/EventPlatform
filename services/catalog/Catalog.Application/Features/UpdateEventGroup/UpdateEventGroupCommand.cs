namespace Catalog.Application.Features.UpdateEventGroup;

/// <summary>
/// Command to update an event group's title, overall date range, and tour-wide contact/social
/// defaults. <see cref="TenantId"/> is set server-side from the validated JWT (never from the
/// request body), per ADR-0011.
/// </summary>
/// <param name="Id">The event group id to update.</param>
/// <param name="TenantId">The caller's tenant id; must own the group.</param>
/// <param name="Title">Group title.</param>
/// <param name="StartsAt">Overall advertised start of the tour, if known.</param>
/// <param name="EndsAt">Overall advertised end of the tour, if known.</param>
/// <param name="ContactPhone">Tour-wide default contact phone.</param>
/// <param name="ContactMobile">Tour-wide default contact mobile number.</param>
/// <param name="ContactEmail">Tour-wide default contact email.</param>
/// <param name="WebsiteUrl">Tour-wide default website URL.</param>
/// <param name="SocialLinks">Tour-wide default social links; replaces the existing list.</param>
public sealed record UpdateEventGroupCommand(
    Guid Id,
    Guid TenantId,
    string Title,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkInput> SocialLinks) : IRequest<UpdateEventGroupOutcome>;
