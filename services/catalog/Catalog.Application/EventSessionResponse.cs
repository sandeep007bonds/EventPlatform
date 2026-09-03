namespace Catalog.Application;

/// <summary>One performance of an event, as returned by the API.</summary>
/// <param name="Id">Performance id — the <c>eventSessionId</c> every downstream service keys on.</param>
/// <param name="EventId">The event this is a performance of.</param>
/// <param name="Name">What to call it when there is more than one, e.g. <c>Matinee</c>.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="EndsAt">Scheduled end (UTC).</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start.</param>
/// <param name="BookingEndsAt">Booking cutoff (UTC), if set — Inventory rejects holds after this.</param>
/// <param name="Status">Lifecycle status name.</param>
/// <param name="SalesPaused">Whether an organizer has paused sales for this performance.</param>
/// <param name="VenueId">The venue, once one is attached.</param>
/// <param name="SeatMapId">The Venue seat map, once one is attached.</param>
/// <param name="SeatMapVersionId">The specific immutable seat-map version used.</param>
/// <param name="SeatMapVersionNumber">That version's number.</param>
/// <param name="VenueName">Venue name, from the cached display snapshot.</param>
/// <param name="City">City, from the cached display snapshot.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code, from the cached display snapshot.</param>
/// <param name="TimeZoneId">The venue's IANA time zone — render this performance's times in it.</param>
/// <param name="Allocations">Which block is sold as which ticket type, for this performance.</param>
public sealed record EventSessionResponse(
    Guid Id,
    Guid EventId,
    string? Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? BookingEndsAt,
    string Status,
    bool SalesPaused,
    Guid? VenueId,
    Guid? SeatMapId,
    Guid? SeatMapVersionId,
    int? SeatMapVersionNumber,
    string? VenueName,
    string? City,
    string? Country,
    string? TimeZoneId,
    IReadOnlyList<SessionAllocationResponse> Allocations);
