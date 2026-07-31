namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for updating a draft event's descriptive/promotional details. The tenant is
/// taken from the caller's token, never from this body (ADR-0011).
/// </summary>
/// <param name="Description">Marketing description.</param>
/// <param name="Category">Free-text category.</param>
/// <param name="EndsAt">Scheduled end time (UTC) — must be after the start time.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start time.</param>
/// <param name="OnSaleAt">Display-only sales-window start (UTC).</param>
/// <param name="BookingEndsAt">Enforced booking cutoff (UTC) — Inventory rejects new holds after this time.</param>
/// <param name="AgeRestriction">Free-text age restriction.</param>
/// <param name="BannerImageUrl">Banner image URL (from the Media service's upload endpoint).</param>
/// <param name="VideoUrl">Video embed URL.</param>
/// <param name="ContactPhone">Contact phone for this leg, overriding the tour default.</param>
/// <param name="ContactMobile">Contact mobile number for this leg, overriding the tour default.</param>
/// <param name="ContactEmail">Contact email for this leg, overriding the tour default.</param>
/// <param name="WebsiteUrl">Website URL for this leg, overriding the tour default.</param>
/// <param name="SocialLinks">This leg's own social links; empty means "no override."</param>
public sealed record UpdateEventDetailsRequest(
    string? Description,
    string? Category,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? OnSaleAt,
    DateTimeOffset? BookingEndsAt,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkRequest>? SocialLinks);
