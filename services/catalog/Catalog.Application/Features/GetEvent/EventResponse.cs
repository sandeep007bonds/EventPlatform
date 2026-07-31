namespace Catalog.Application.Features.GetEvent;

/// <summary>Read model returned for a single event.</summary>
/// <param name="Id">Event id.</param>
/// <param name="Title">Event title.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="Status">Lifecycle status name.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="VenueId">Venue the event is held at — fetch via <c>GET /v1/venues/{id}</c> for venue details.</param>
/// <param name="Description">Marketing description, if set.</param>
/// <param name="Category">Free-text category, if set.</param>
/// <param name="EndsAt">Scheduled end time (UTC), if set.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if set.</param>
/// <param name="OnSaleAt">Display-only sales-window start (UTC), if set — not enforced.</param>
/// <param name="OffSaleAt">Display-only sales-window end (UTC), if set — not enforced.</param>
/// <param name="AgeRestriction">Free-text age restriction, if set.</param>
/// <param name="BannerImageUrl">Banner image URL, if set.</param>
/// <param name="VideoUrl">Video embed URL, if set.</param>
public sealed record EventResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAt,
    string Status,
    string Currency,
    Guid VenueId,
    string? Description,
    string? Category,
    DateTimeOffset? EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? OnSaleAt,
    DateTimeOffset? OffSaleAt,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl);
