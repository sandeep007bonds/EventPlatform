namespace Catalog.Application.Features.CreateEvent;

/// <summary>
/// Command to create a new draft event. <see cref="TenantId"/> is set server-side from the
/// validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Title">Event title.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="EndsAt">Scheduled end (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="LocationName">Location/venue name.</param>
/// <param name="AddressLine1">Street address, line 1.</param>
/// <param name="AddressLine2">Street address, line 2, if any.</param>
/// <param name="City">City.</param>
/// <param name="Region">State/province/region, if applicable.</param>
/// <param name="PostalCode">Postal/ZIP code, if applicable.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="Latitude">Latitude, if known.</param>
/// <param name="Longitude">Longitude, if known.</param>
/// <param name="EventGroupId">The tour/series this event is one leg of, if any.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit for this event; <see langword="null"/> means no limit.</param>
/// <param name="RequiresQueue">Whether to gate holds behind the Queue service's waiting room.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage (e.g. 18 for 18% GST); <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. "GST 18%").</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units (e.g. 3000 for ₹30); 0 means no fee.</param>
/// <param name="TimeZoneId">The venue's IANA time zone (e.g. "Asia/Kolkata"); null when not set.</param>
public sealed record CreateEventCommand(
    Guid TenantId,
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Currency,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string? PostalCode,
    string Country,
    double? Latitude,
    double? Longitude,
    Guid? EventGroupId,
    int? MaxTicketsPerBuyer = null,
    bool RequiresQueue = false,
    decimal? TaxRatePercent = null,
    string? TaxLabel = null,
    long BookingFeePerTicketMinor = 0,
    string? TimeZoneId = null) : IRequest<CreateEventResult>;
