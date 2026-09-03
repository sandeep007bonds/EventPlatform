namespace Catalog.Application.Features.UpdateEventSession;

/// <summary>Command to rename and reschedule a draft performance.</summary>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to change.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">What to call it, or <see langword="null"/> to clear the name.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="EndsAt">Scheduled end (UTC) — must be after <see cref="StartsAt"/>.</param>
/// <param name="DoorsOpenAt">Doors-open time (UTC), if different from the start.</param>
/// <param name="BookingEndsAt">Booking cutoff (UTC), if any.</param>
public sealed record UpdateEventSessionCommand(
    Guid EventId,
    Guid EventSessionId,
    Guid TenantId,
    string? Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? BookingEndsAt) : IRequest<SessionCommandResult>;
