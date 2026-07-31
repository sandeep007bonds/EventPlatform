namespace Catalog.Application.Features.GetVenue;

/// <summary>
/// Query to fetch a single venue by id. Unlike <see cref="Catalog.Application.Features.GetEvent.GetEventQuery"/>,
/// there is no tenant-visibility rule — a venue by itself, unlinked from any event, reveals
/// nothing sensitive, so this is fetchable by anyone.
/// </summary>
/// <param name="Id">The venue id.</param>
public sealed record GetVenueQuery(Guid Id) : IRequest<VenueResponse?>;
