namespace Catalog.Application.Features.GetEvent;

/// <summary>Read model returned for a single event.</summary>
/// <remarks>
/// The dates and the venue are on the <see cref="Sessions"/>, not here — one event can run several
/// nights, possibly in different configurations of the hall.
/// <see cref="FirstSessionStartsAt"/>/<see cref="LastSessionEndsAt"/> summarise the run so a list
/// can be sorted and filtered without opening every performance.
/// </remarks>
/// <param name="Id">Event id.</param>
/// <param name="Title">Event title.</param>
/// <param name="Slug">URL-safe public identifier — the <c>/events/{slug}</c> a buyer sees.</param>
/// <param name="Status">Lifecycle status name.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="EventGroupId">
/// The tour/series this event is one leg of, if any — fetch via <c>GET /v1/event-groups/{id}</c>
/// for the group's title, or <c>GET /v1/events?eventGroupId={id}</c> for sibling legs.
/// </param>
/// <param name="Description">Marketing description, if set.</param>
/// <param name="Category">Free-text category, if set.</param>
/// <param name="FirstSessionStartsAt">When the first performance starts — what the run is listed by.</param>
/// <param name="LastSessionEndsAt">When the last performance ends.</param>
/// <param name="OnSaleAt">Enforced sales-window start (UTC) for the whole run, if set.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit across the run, if set.</param>
/// <param name="RequiresQueue">Whether a buyer must pass through the Queue service's waiting room before holding a seat.</param>
/// <param name="TaxRatePercent">Sales-tax rate applied to orders for this event, as a percentage; <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. "GST 18%").</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units; 0 means no fee.</param>
/// <param name="AllSalesPaused">Whether sales are currently paused on every performance.</param>
/// <param name="AgeRestriction">Free-text age restriction, if set.</param>
/// <param name="BannerImageUrl">Banner image URL, if set.</param>
/// <param name="VideoUrl">Video embed URL, if set.</param>
/// <param name="ContactPhone">Contact phone — this leg's own value, or the tour's default if the leg sets none.</param>
/// <param name="ContactMobile">Contact mobile number — see <see cref="ContactPhone"/>.</param>
/// <param name="ContactEmail">Contact email — see <see cref="ContactPhone"/>.</param>
/// <param name="WebsiteUrl">Website URL — see <see cref="ContactPhone"/>.</param>
/// <param name="SocialLinks">
/// Social links — this leg's own list if it set any, otherwise the tour's default list.
/// </param>
/// <param name="Sessions">The performances, earliest first. Always at least one.</param>
public sealed record EventResponse(
    Guid Id,
    string Title,
    string Slug,
    string Status,
    string Currency,
    Guid? EventGroupId,
    string? Description,
    string? Category,
    DateTimeOffset? FirstSessionStartsAt,
    DateTimeOffset? LastSessionEndsAt,
    DateTimeOffset? OnSaleAt,
    int? MaxTicketsPerBuyer,
    bool RequiresQueue,
    decimal? TaxRatePercent,
    string? TaxLabel,
    long BookingFeePerTicketMinor,
    bool AllSalesPaused,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl,
    string? ContactPhone,
    string? ContactMobile,
    string? ContactEmail,
    string? WebsiteUrl,
    IReadOnlyList<SocialLinkResponse> SocialLinks,
    IReadOnlyList<EventSessionResponse> Sessions);
