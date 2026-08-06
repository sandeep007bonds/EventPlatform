namespace Catalog.Application.Features.RemoveSeatMapSection;

/// <summary>
/// Command to remove one section from a draft event's existing seat map entirely.
/// <see cref="TenantId"/> is set server-side from the validated JWT (never from the request body),
/// per ADR-0011.
/// </summary>
/// <param name="EventId">The event whose seat map section is being removed.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="SectionName">The section name to remove.</param>
public sealed record RemoveSeatMapSectionCommand(
    Guid EventId,
    Guid TenantId,
    string SectionName) : IRequest<RemoveSeatMapSectionResult>;
