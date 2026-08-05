namespace Catalog.Application.Features.ListEntryGates;

/// <summary>Query to list every entry gate defined for an event. Anonymous — reveals nothing sensitive alone.</summary>
/// <param name="EventId">The event id.</param>
public sealed record ListEntryGatesQuery(Guid EventId) : IRequest<IReadOnlyList<EntryGateResponse>>;
