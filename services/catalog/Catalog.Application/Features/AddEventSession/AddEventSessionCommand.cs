namespace Catalog.Application.Features.AddEventSession;

/// <summary>
/// Command to add a performance to an event. <see cref="TenantId"/> is set server-side from the
/// validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="EventId">The event to add a performance to.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">What to call it when there is more than one, e.g. <c>Matinee</c>.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="EndsAt">Scheduled end (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start.</param>
/// <param name="BookingEndsAt">Booking cutoff (UTC), if any.</param>
public sealed record AddEventSessionCommand(
    Guid EventId,
    Guid TenantId,
    string? Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? BookingEndsAt) : IRequest<SessionCommandResult>;
