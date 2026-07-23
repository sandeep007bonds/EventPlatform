using MediatR;

namespace Catalog.Application.Features.CreateEvent;

/// <summary>
/// Command to create a new draft event. <see cref="TenantId"/> is set server-side from the
/// validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="VenueId">Venue the event is held at.</param>
/// <param name="Title">Event title.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="Currency">ISO 4217 currency code.</param>
public sealed record CreateEventCommand(
    Guid TenantId,
    Guid VenueId,
    string Title,
    DateTimeOffset StartsAt,
    string Currency) : IRequest<Guid>;
