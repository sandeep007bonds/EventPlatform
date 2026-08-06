namespace Catalog.Application.Features.GetEvent;

/// <summary>Read model returned for a single event.</summary>
/// <param name="Id">Event id.</param>
/// <param name="Title">Event title.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="Status">Lifecycle status name.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="EventGroupId">
/// The tour/series this event is one leg of, if any — fetch via <c>GET /v1/event-groups/{id}</c>
/// for the group's title, or <c>GET /v1/events?eventGroupId={id}</c> for sibling legs.
/// </param>
/// <param name="Description">Marketing description, if set.</param>
/// <param name="Category">Free-text category, if set.</param>
/// <param name="EndsAt">Scheduled end time (UTC) — this leg's run at its location ends at this time.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if set.</param>
/// <param name="OnSaleAt">Display-only sales-window start (UTC), if set — not enforced.</param>
/// <param name="BookingEndsAt">Enforced booking cutoff (UTC), if set — Inventory rejects new holds after this time.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit for this event, if set.</param>
/// <param name="RequiresQueue">Whether a buyer must pass through the Queue service's waiting room before holding a seat.</param>
/// <param name="AgeRestriction">Free-text age restriction, if set.</param>
/// <param name="BannerImageUrl">Banner image URL, if set.</param>
/// <param name="VideoUrl">Video embed URL, if set.</param>
/// <param name="LocationName">Location/venue name.</param>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
/// <param name="ContactPhone">Contact phone — this leg's own value, or the tour's default if the leg sets none.</param>
/// <param name="ContactMobile">Contact mobile number — see <see cref="ContactPhone"/>.</param>
/// <param name="ContactEmail">Contact email — see <see cref="ContactPhone"/>.</param>
/// <param name="WebsiteUrl">Website URL — see <see cref="ContactPhone"/>.</param>
/// <param name="SocialLinks">
/// Social links — this leg's own list if it set any, otherwise the tour's default list.
/// </param>
public sealed record EventResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAt,
    string Status,
    string Currency,
    Guid? EventGroupId,
    string? Description,
    string? Category,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? OnSaleAt,
    DateTimeOffset? BookingEndsAt,
    int? MaxTicketsPerBuyer,
    bool RequiresQueue,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkResponse> SocialLinks);
