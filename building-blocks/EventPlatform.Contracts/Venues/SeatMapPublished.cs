namespace EventPlatform.Contracts.Venues;

/// <summary>
/// Published by the Venue service when a seat-map version is frozen and becomes live.
/// </summary>
/// <remarks>
/// Carries the version number and total capacity, not the layout. A consumer that needs the seats
/// reads them back by id: a stadium plan is megabytes, and a message bus is the wrong place to move
/// it. Capacity is included because it is the one number nearly every consumer wants and none of
/// them should have to sum tens of thousands of rows to get.
/// </remarks>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) that owns the venue.</param>
/// <param name="VenueId">The venue the map configures.</param>
/// <param name="SeatMapId">The seat map that was published.</param>
/// <param name="SeatMapVersionId">The specific version that is now live.</param>
/// <param name="VersionNumber">That version's number.</param>
/// <param name="Capacity">Sellable seats plus admission-area capacity.</param>
public sealed record SeatMapPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid VenueId,
    Guid SeatMapId,
    Guid SeatMapVersionId,
    int VersionNumber,
    int Capacity) : IntegrationEvent(EventId, OccurredAt, TenantId);
