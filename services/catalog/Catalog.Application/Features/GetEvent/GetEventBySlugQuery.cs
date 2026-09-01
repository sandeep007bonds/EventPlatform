namespace Catalog.Application.Features.GetEvent;

/// <summary>Query to fetch a single event by its public slug.</summary>
/// <param name="Slug">The slug.</param>
/// <param name="CallerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
public sealed record GetEventBySlugQuery(string Slug, Guid? CallerTenantId) : IRequest<EventResponse?>;
