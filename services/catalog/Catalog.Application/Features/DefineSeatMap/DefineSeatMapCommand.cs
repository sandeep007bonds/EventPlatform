namespace Catalog.Application.Features.DefineSeatMap;

/// <summary>
/// Command to define the seat map for a draft event. <see cref="TenantId"/> is set server-side
/// from the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="EventId">The event to define the seat map for.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">Seat-map name.</param>
/// <param name="Sections">The sections to generate seats from.</param>
public sealed record DefineSeatMapCommand(
    Guid EventId,
    Guid TenantId,
    string Name,
    IReadOnlyList<SeatMapSectionInput> Sections) : IRequest<DefineSeatMapResult>;
