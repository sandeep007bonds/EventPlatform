namespace Catalog.Application.Features.ListEventSessions;

/// <summary>Query for an event's performances.</summary>
/// <param name="EventId">The event id.</param>
/// <param name="TenantId">
/// The calling tenant, or <see langword="null"/> for an anonymous caller. A draft event's
/// performances are visible only to the tenant that owns it, the same rule the event itself uses.
/// </param>
public sealed record ListEventSessionsQuery(Guid EventId, Guid? TenantId)
    : IRequest<IReadOnlyList<EventSessionResponse>?>;
