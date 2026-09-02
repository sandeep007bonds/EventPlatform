namespace Venues.Application.Features.AddVenueFacility;

/// <summary>Command to record something a venue offers.</summary>
/// <param name="VenueId">The venue to add the facility to.</param>
/// <param name="TenantId">Owning tenant (organizer), taken from the caller's token.</param>
/// <param name="Name">Facility name.</param>
/// <param name="Description">Optional detail shown alongside the name.</param>
public sealed record AddVenueFacilityCommand(Guid VenueId, Guid TenantId, string Name, string? Description)
    : IRequest<Guid?>;
