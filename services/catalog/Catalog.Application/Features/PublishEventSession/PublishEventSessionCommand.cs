namespace Catalog.Application.Features.PublishEventSession;

/// <summary>
/// Command to take one performance on sale, without republishing the event.
/// </summary>
/// <remarks>
/// The path for adding a late show to a run that is already selling: the new performance was a
/// draft while its seat map and pricing were set up, and this is what makes it live.
/// </remarks>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to publish.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
public sealed record PublishEventSessionCommand(Guid EventId, Guid EventSessionId, Guid TenantId)
    : IRequest<SessionCommandResult>;
