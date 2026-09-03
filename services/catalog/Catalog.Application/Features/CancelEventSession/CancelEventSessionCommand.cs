namespace Catalog.Application.Features.CancelEventSession;

/// <summary>Command to call off one performance.</summary>
/// <remarks>
/// Cancelling is not deleting. Orders and tickets reference the performance and their history has
/// to keep making sense, so the row stays and its status changes.
/// </remarks>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to cancel.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
public sealed record CancelEventSessionCommand(Guid EventId, Guid EventSessionId, Guid TenantId)
    : IRequest<SessionCommandResult>;
