namespace Catalog.Application.Features.UpdateSeatMapSection;

/// <summary>
/// Command to replace one existing section of a draft event's seat map with a new definition —
/// implemented as remove-then-add (see <see cref="SeatMap.RemoveSection"/>), so any field
/// (including the name itself and the allocation type) may change freely.
/// <see cref="TenantId"/> is set server-side from the validated JWT (never from the request body),
/// per ADR-0011.
/// </summary>
/// <param name="EventId">The event whose seat map section is being edited.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="CurrentSectionName">The existing section name to replace.</param>
/// <param name="Section">The new section definition.</param>
public sealed record UpdateSeatMapSectionCommand(
    Guid EventId,
    Guid TenantId,
    string CurrentSectionName,
    SeatMapSectionInput Section) : IRequest<UpdateSeatMapSectionResult>;
