namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Query to fetch the seat map (with seats) for an event.</summary>
/// <param name="EventId">The event id.</param>
/// <param name="CallerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
public sealed record GetSeatMapQuery(Guid EventId, Guid? CallerTenantId) : IRequest<SeatMapResponse?>;
