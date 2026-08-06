namespace Catalog.Application.Features.AddSeatMapSections;

/// <summary>
/// Command to add more sections to a draft event's existing seat map.
/// <see cref="TenantId"/> is set server-side from the validated JWT (never from the request body),
/// per ADR-0011.
/// </summary>
/// <param name="EventId">The event whose seat map gets the new sections.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Sections">The sections to add.</param>
public sealed record AddSeatMapSectionsCommand(
    Guid EventId,
    Guid TenantId,
    IReadOnlyList<SeatMapSectionInput> Sections) : IRequest<AddSeatMapSectionsResult>;
