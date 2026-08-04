namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an event is published and its seat
/// inventory has been generated. Consumed by Search, Reporting, etc., and by
/// Inventory to provision seat/general-admission inventory and learn the booking cutoff.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the catalog event belongs to.</param>
/// <param name="CatalogEventId">The id of the published catalog event.</param>
/// <param name="Title">The event title.</param>
/// <param name="SeatCount">Total sellable capacity generated for the event (reserved seats plus general-admission capacity).</param>
/// <param name="BookingEndsAt">
/// Enforced booking cutoff (UTC), if set — Inventory rejects new holds for this event after this
/// time. Cannot change after publish in this pass (<c>UpdateEventDetails</c> is Draft-only).
/// </param>
/// <param name="MaxTicketsPerBuyer">
/// Maximum tickets a single buyer may hold for this event, if limited — Inventory enforces this
/// at hold-placement time, summing the buyer's active and converted holds.
/// </param>
/// <param name="OnSaleAt">
/// Enforced on-sale start (UTC), if set — Inventory rejects new holds for this event until this
/// time. Cannot change after publish in this pass (<c>UpdateEventDetails</c> is Draft-only).
/// </param>
public sealed record EventPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    string Title,
    int SeatCount,
    DateTimeOffset? BookingEndsAt,
    int? MaxTicketsPerBuyer = null,
    DateTimeOffset? OnSaleAt = null) : IntegrationEvent(EventId, OccurredAt, TenantId);
