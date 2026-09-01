namespace Catalog.Application.Features.UpdateEventPresentation;

/// <summary>
/// Command to set how an event is presented — title, description, imagery, contact and social
/// links. Permitted at <b>any</b> status. <see cref="TenantId"/> is set server-side from the
/// validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="Id">The event id to update.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="Title">Event title.</param>
/// <param name="Description">Marketing description.</param>
/// <param name="Category">Free-text category.</param>
/// <param name="AgeRestriction">Free-text age restriction.</param>
/// <param name="BannerImageUrl">Banner image URL (from the Media service's upload endpoint).</param>
/// <param name="VideoUrl">Video embed URL.</param>
/// <param name="ContactPhone">Contact phone for this leg, overriding the tour default.</param>
/// <param name="ContactMobile">Contact mobile number for this leg, overriding the tour default.</param>
/// <param name="ContactEmail">Contact email for this leg, overriding the tour default.</param>
/// <param name="WebsiteUrl">Website URL for this leg, overriding the tour default.</param>
/// <param name="SocialLinks">This leg's own social links; empty means "no override" — the tour's defaults apply.</param>
public sealed record UpdateEventPresentationCommand(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Description,
    string? Category,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkInput> SocialLinks) : IRequest<UpdateEventPresentationOutcome>;
