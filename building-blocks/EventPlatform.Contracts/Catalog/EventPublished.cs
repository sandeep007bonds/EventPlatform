namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an event is published and its seat
/// inventory has been generated. Consumed by Search, Reporting, etc.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the catalog event belongs to.</param>
/// <param name="CatalogEventId">The id of the published catalog event.</param>
/// <param name="Title">The event title.</param>
/// <param name="SeatCount">Number of seats generated for the event (from its seat map).</param>
public sealed record EventPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    string Title,
    int SeatCount) : IntegrationEvent(EventId, OccurredAt, TenantId);
