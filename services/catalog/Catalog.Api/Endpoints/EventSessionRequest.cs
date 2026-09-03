namespace Catalog.Api.Endpoints;

/// <summary>Request body for adding or rescheduling a performance.</summary>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="EndsAt">Scheduled end (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="Name">What to call it when there is more than one, e.g. <c>Matinee</c>.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start.</param>
/// <param name="BookingEndsAt">
/// Booking cutoff (UTC), if any — after this, holds for this performance are refused.
/// </param>
public sealed record EventSessionRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string? Name = null,
    DateTimeOffset? DoorsOpenAt = null,
    DateTimeOffset? BookingEndsAt = null);
