namespace Venues.Application.Features.CreateSeatMap;

/// <summary>
/// Command to add a seating configuration to a venue. The map is created with an empty version 1
/// open for editing.
/// </summary>
/// <param name="VenueId">The venue this map configures.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">Configuration name (e.g. <c>End stage</c>).</param>
public sealed record CreateSeatMapCommand(Guid VenueId, Guid TenantId, string Name) : IRequest<SeatMapResponse?>;
