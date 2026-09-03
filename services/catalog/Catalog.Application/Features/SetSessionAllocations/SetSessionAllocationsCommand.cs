namespace Catalog.Application.Features.SetSessionAllocations;

/// <summary>
/// Command to set which block of the venue is sold as which ticket type, for one performance.
/// </summary>
/// <remarks>
/// The whole map every time, not a patch: the caller is looking at every block in the version, and
/// a partial update would leave "which blocks are still unassigned" unanswerable without re-reading
/// everything anyway.
/// </remarks>
/// <param name="EventId">The event the performance belongs to.</param>
/// <param name="EventSessionId">The performance to allocate.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Allocations">Section/area code paired with the ticket type it sells as.</param>
public sealed record SetSessionAllocationsCommand(
    Guid EventId,
    Guid EventSessionId,
    Guid TenantId,
    IReadOnlyList<SessionAllocationInput> Allocations) : IRequest<SessionCommandResult>;
