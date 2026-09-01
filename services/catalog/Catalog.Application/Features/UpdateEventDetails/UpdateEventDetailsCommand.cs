namespace Catalog.Application.Features.UpdateEventDetails;

/// <summary>
/// Command to set the facts a ticket holder buys — dates, venue, tax, fees and ticketing rules.
/// Draft-only. <see cref="TenantId"/> is set server-side from the validated JWT (never from the
/// request body), per ADR-0011.
/// </summary>
/// <remarks>
/// Presentation (title, description, imagery, contact details) moved to
/// <c>UpdateEventPresentation</c>, which works at any status. The split is by consequence, not by
/// field: everything here changes what someone paid for, and nothing there does.
/// </remarks>
/// <param name="Id">The event id to update.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="StartsAt">Scheduled start time (UTC).</param>
/// <param name="EndsAt">Scheduled end time (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start time.</param>
/// <param name="OnSaleAt">Enforced sales-window start (UTC).</param>
/// <param name="BookingEndsAt">Enforced booking cutoff (UTC) — Inventory rejects new holds after this time.</param>
/// <param name="LocationName">Location/venue name.</param>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit for this event; <see langword="null"/> means no limit.</param>
/// <param name="RequiresQueue">Whether to gate holds behind the Queue service's waiting room.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage (e.g. 18 for 18% GST); <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. "GST 18%").</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units (e.g. 3000 for ₹30); 0 means no fee.</param>
/// <param name="TimeZoneId">The venue's IANA time zone (e.g. "Asia/Kolkata"); null when not set.</param>
public sealed record UpdateEventDetailsCommand(
    Guid Id,
    Guid TenantId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? OnSaleAt,
    DateTimeOffset? BookingEndsAt,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    int? MaxTicketsPerBuyer,
    bool RequiresQueue,
    decimal? TaxRatePercent,
    string? TaxLabel,
    long BookingFeePerTicketMinor,
    string? TimeZoneId) : IRequest<UpdateEventDetailsOutcome>;
