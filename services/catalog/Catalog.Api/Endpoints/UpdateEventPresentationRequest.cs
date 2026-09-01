namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for updating how an event is presented. Accepted at any status. The tenant is
/// taken from the caller's token, never from this body (ADR-0011).
/// </summary>
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
/// <param name="SocialLinks">This leg's own social links; empty means "no override".</param>
public sealed record UpdateEventPresentationRequest(
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
    IReadOnlyList<SocialLinkRequest>? SocialLinks);
