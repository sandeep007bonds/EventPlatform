namespace Catalog.Application.Features.ChangeEventSlug;

/// <summary>
/// Command to change a draft event's public slug. <see cref="TenantId"/> is set server-side from
/// the validated JWT (never from the request body), per ADR-0011.
/// </summary>
/// <param name="Id">The event id.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
/// <param name="Slug">The requested slug. Normalized before use, so "My Show!" is accepted.</param>
public sealed record ChangeEventSlugCommand(Guid Id, Guid TenantId, string Slug)
    : IRequest<ChangeEventSlugOutcome>;
