namespace EventPlatform.Contracts.Venues;

/// <summary>
/// Published by the Venue service when an organizer adds a venue. Consumed by Search (so a venue
/// becomes findable) and, once they exist, Audit and Reporting.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) that owns the venue.</param>
/// <param name="VenueId">The new venue's id.</param>
/// <param name="Name">Venue name.</param>
/// <param name="City">City the venue is in.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
public sealed record VenueCreated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid VenueId,
    string Name,
    string City,
    string Country) : IntegrationEvent(EventId, OccurredAt, TenantId);
