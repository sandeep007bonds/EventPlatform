namespace Catalog.Application.Features.RemoveEventSession;

/// <summary>Command to remove a draft performance that never went on sale.</summary>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to remove.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
public sealed record RemoveEventSessionCommand(Guid EventId, Guid EventSessionId, Guid TenantId)
    : IRequest<SessionCommandResult>;
