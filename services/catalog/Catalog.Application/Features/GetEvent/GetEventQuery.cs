namespace Catalog.Application.Features.GetEvent;

/// <summary>Query to fetch a single event by id.</summary>
/// <param name="Id">The event id.</param>
/// <param name="CallerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
public sealed record GetEventQuery(Guid Id, Guid? CallerTenantId) : IRequest<EventResponse?>;
