namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>
/// Command to set a draft event's descriptive/promotional details. <see cref="TenantId"/> is set
/// server-side from the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="Id">The event id to update.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="Description">Marketing description.</param>
/// <param name="Category">Free-text category.</param>
/// <param name="EndsAt">Scheduled end time (UTC), if known.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start time.</param>
/// <param name="OnSaleAt">Display-only sales-window start (UTC).</param>
/// <param name="OffSaleAt">Display-only sales-window end (UTC).</param>
/// <param name="AgeRestriction">Free-text age restriction.</param>
/// <param name="BannerImageUrl">Banner image URL (from the Media service's upload endpoint).</param>
/// <param name="VideoUrl">Video embed URL.</param>
public sealed record UpdateEventDetailsCommand(
    Guid Id,
    Guid TenantId,
    string? Description,
    string? Category,
    DateTimeOffset? EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? OnSaleAt,
    DateTimeOffset? OffSaleAt,
    string? AgeRestriction,
    string? BannerImageUrl,
    string? VideoUrl) : IRequest<UpdateEventDetailsOutcome>;
