namespace EventPlatform.Contracts.Inventory;

/// <summary>
/// Published by the Inventory service when an organizer blocks seats (e.g. a kill or a restricted
/// view) so they can no longer be held or sold.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the seats belong to.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="SeatIds">The blocked seat ids.</param>
/// <param name="Reason">Optional organizer-supplied reason.</param>
public sealed record SeatBlocked(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    IReadOnlyList<Guid> SeatIds,
    string? Reason) : IntegrationEvent(EventId, OccurredAt, TenantId);
