namespace Catalog.Application.Features.CreateEvent;

/// <summary>
/// Command to create a new draft event and its first performance. <see cref="TenantId"/> is set
/// server-side from the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <remarks>
/// The first performance is created here rather than added afterwards: an event with none sells
/// nothing, has no date to list it by, and cannot be checked against its tour's range. The
/// single-performance case — the overwhelming majority — then needs no second call.
/// </remarks>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Title">Event title.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="StartsAt">The first performance's start (UTC).</param>
/// <param name="EndsAt">The first performance's end (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="DoorsOpenAt">The first performance's doors-open time (UTC), if different.</param>
/// <param name="BookingEndsAt">The first performance's booking cutoff (UTC), if any.</param>
/// <param name="EventGroupId">The tour/series this event is one leg of, if any.</param>
/// <param name="MaxTicketsPerBuyer">Per-buyer ticket limit across the whole run; <see langword="null"/> means no limit.</param>
/// <param name="RequiresQueue">Whether to gate holds behind the Queue service's waiting room.</param>
/// <param name="OnSaleAt">Enforced sales-window start (UTC) for the whole run, if set.</param>
/// <param name="TaxRatePercent">Sales-tax rate as a percentage (e.g. 18 for 18% GST); <see langword="null"/> means untaxed.</param>
/// <param name="TaxLabel">Display name for the tax on a receipt (e.g. "GST 18%").</param>
/// <param name="BookingFeePerTicketMinor">Booking fee per ticket in minor currency units; 0 means no fee.</param>
/// <param name="Slug">
/// A vanity URL slug. <see langword="null"/> or blank derives one from <see cref="Title"/>; a
/// supplied one that is already taken gets the same numeric suffix a derived one would.
/// </param>
public sealed record CreateEventCommand(
    Guid TenantId,
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
    string? Slug = null) : IRequest<CreateEventResult>;
