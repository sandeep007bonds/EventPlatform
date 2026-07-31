namespace Catalog.Application.Features.CreateEventGroup;

/// <summary>
/// Command to create a new event group (tour). <see cref="TenantId"/> is set server-side from
/// the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Title">Group title (e.g. "Coldplay World Tour").</param>
public sealed record CreateEventGroupCommand(Guid TenantId, string Title) : IRequest<Guid>;
