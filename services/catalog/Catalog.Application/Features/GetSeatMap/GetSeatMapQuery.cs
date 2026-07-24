namespace Catalog.Application.Features.GetSeatMap;

/// <summary>Query to fetch the seat map (with seats) for an event.</summary>
/// <param name="EventId">The event id.</param>
public sealed record GetSeatMapQuery(Guid EventId) : IRequest<SeatMapResponse?>;
