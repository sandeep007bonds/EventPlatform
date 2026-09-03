namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when an event goes live.
/// </summary>
/// <remarks>
/// Event-level facts only. Everything a performance owns — its dates, its seat map, its capacity,
/// its inventory — travels on <see cref="EventSessionPublished"/> instead, one message per
/// performance, because that is the grain Inventory, Ticketing and Ordering work at.
/// <para>
/// What is left here is what genuinely belongs to the whole run, and today that means the waiting
/// room: Queue provisions one room per event, gating one on-sale, so it consumes this and nothing
/// else.
/// </para>
/// </remarks>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the catalog event belongs to.</param>
/// <param name="CatalogEventId">The id of the published catalog event.</param>
/// <param name="Title">The event title.</param>
/// <param name="RequiresQueue">
/// Whether this event gates seat holds behind the Queue service's virtual waiting room. Queue reads
/// it to provision its per-event settings as enabled or as an immediate-admit no-op.
/// </param>
/// <param name="OnSaleAt">
/// Enforced on-sale start (UTC), if set. On the event rather than the performance because a run
/// goes on sale once, at one advertised moment, for every night at the same time.
/// </param>
public sealed record EventPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    string Title,
    bool RequiresQueue = false,
    DateTimeOffset? OnSaleAt = null) : IntegrationEvent(EventId, OccurredAt, TenantId);
