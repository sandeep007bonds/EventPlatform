namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for creating an event and its first performance. The tenant is taken from the
/// caller's token, never from this body (ADR-0011).
/// </summary>
/// <remarks>
/// The dates here belong to the <b>first performance</b>, not the event. An event with none sells
/// nothing and has no date to be listed by, so it is created with one; a run of several nights adds
/// the rest through <c>POST /v1/events/{id}/sessions</c>. The venue is attached to the performance
/// afterwards, because it is a Venue seat-map version rather than an address typed here.
/// </remarks>
/// <param name="Title">Event title.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="StartsAt">The first performance's start (UTC).</param>
/// <param name="EndsAt">The first performance's end (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="DoorsOpenAt">The first performance's doors-open time (UTC), if different.</param>
/// <param name="BookingEndsAt">The first performance's booking cutoff (UTC), if any.</param>
/// <param name="EventGroupId">The tour/series this event is one leg of, if any.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit across the run; <see langword="null"/> means no limit.</param>
/// <param name="RequiresQueue">Whether to gate holds behind the Queue service's waiting room.</param>
/// <param name="OnSaleAt">Enforced sales-window start (UTC) for the whole run, if set.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage (e.g. 18 for 18% GST); <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. "GST 18%").</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units (e.g. 3000 for ₹30); 0 means no fee.</param>
/// <param name="Slug">Optional vanity URL slug; derived from <see cref="Title"/> when omitted.</param>
public sealed record CreateEventRequest(
    string Title,
    string Currency,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt = null,
    DateTimeOffset? BookingEndsAt = null,
    Guid? EventGroupId = null,
    int? MaxTicketsPerBuyer = null,
    bool RequiresQueue = false,
    DateTimeOffset? OnSaleAt = null,
    decimal? TaxRatePercent = null,
    string? TaxLabel = null,
    long BookingFeePerTicketMinor = 0,
    string? Slug = null);
