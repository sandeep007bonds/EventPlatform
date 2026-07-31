namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for updating a draft event's descriptive/promotional details. The tenant is
/// taken from the caller's token, never from this body (ADR-0011).
/// </summary>
/// <param name="Description">Marketing description.</param>
/// <param name="Category">Free-text category.</param>
/// <param name="EndsAt">Scheduled end time (UTC), if known.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start time.</param>
/// <param name="OnSaleAt">Display-only sales-window start (UTC).</param>
/// <param name="OffSaleAt">Display-only sales-window end (UTC).</param>
/// <param name="AgeRestriction">Free-text age restriction.</param>
/// <param name="BannerImageUrl">Banner image URL (from the Media service's upload endpoint).</param>
/// <param name="VideoUrl">Video embed URL.</param>
public sealed record UpdateEventDetailsRequest(
    string? Description,
    string? Category,
    DateTimeOffset? EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? OnSaleAt,
    DateTimeOffset? OffSaleAt,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl);
