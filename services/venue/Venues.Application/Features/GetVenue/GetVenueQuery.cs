namespace Venues.Application.Features.GetVenue;

/// <summary>Query for one venue in full, including its gates and facilities.</summary>
/// <param name="VenueId">The venue id.</param>
/// <param name="TenantId">
/// The calling tenant, or <see langword="null"/> for an anonymous caller. A venue is only ever
/// visible to the tenant that owns it: an unpublished line-up can be inferred from where an
/// organizer is building maps.
/// </param>
public sealed record GetVenueQuery(Guid VenueId, Guid? TenantId) : IRequest<VenueResponse?>;
