namespace EventPlatform.Contracts.Catalog;

/// <summary>
/// Published by the Catalog service when one performance of an event goes on sale — the message
/// Inventory provisions from and Ticketing builds its scan context from.
/// </summary>
/// <remarks>
/// <b>One per performance, not one per event.</b> Inventory is keyed by performance: seat A1 on
/// Friday and seat A1 on Saturday are two independent things to sell, and a single per-event
/// message could not say that.
/// <para>
/// It carries the seat map by <b>id and version</b>, never inline. A stadium plan is megabytes and
/// a message bus is the wrong place to move it; a consumer that needs the seats reads them from
/// Venue. The <see cref="Allocations"/> list <i>is</i> inline, because it is one row per block
/// rather than per seat — tens of rows even for a stadium — and carrying it saves a second call
/// back to Catalog on every provisioning run.
/// </para>
/// </remarks>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the performance belongs to.</param>
/// <param name="CatalogEventId">
/// The event this is a performance of. Kept alongside the performance id because the per-buyer
/// ticket limit is counted across the whole run, not per night.
/// </param>
/// <param name="EventSessionId">The performance — the grain every consumer keys on.</param>
/// <param name="VenueId">The venue it happens at.</param>
/// <param name="SeatMapId">The Venue seat map used.</param>
/// <param name="SeatMapVersionId">
/// The specific immutable version. Pinned rather than resolved, so a later venue reconfiguration
/// cannot move the seats a sold ticket names.
/// </param>
/// <param name="SeatMapVersionNumber">That version's number, for reading it back from Venue.</param>
/// <param name="Capacity">Sellable seats plus admission-area capacity, as the version reports it.</param>
/// <param name="StartsAt">Scheduled start (UTC).</param>
/// <param name="EndsAt">Scheduled end (UTC) — Ticketing rejects check-in after this.</param>
/// <param name="DoorsOpenAt">
/// Doors-open time (UTC), if set — Ticketing's check-in window opens here, falling back to
/// <see cref="StartsAt"/> when absent.
/// </param>
/// <param name="BookingEndsAt">
/// Booking cutoff (UTC), if set — Inventory rejects new holds for this performance after it.
/// </param>
/// <param name="OnSaleAt">Enforced on-sale start (UTC), if set — an event-level fact, repeated here so Inventory needs only one message.</param>
/// <param name="MaxTicketsPerBuyer">
/// Maximum tickets one buyer may hold for the <b>event</b>, if limited. Inventory sums across every
/// performance, which is why <see cref="CatalogEventId"/> travels with it.
/// </param>
/// <param name="RequiresQueue">Whether holds require a Queue admission token — an event-level fact, repeated for the same reason.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="Allocations">Which block is sold as which ticket type, and at what price.</param>
public sealed record EventSessionPublished(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId,
    Guid VenueId,
    Guid SeatMapId,
    Guid SeatMapVersionId,
    int SeatMapVersionNumber,
    int Capacity,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    DateTimeOffset? DoorsOpenAt,
    DateTimeOffset? BookingEndsAt,
    DateTimeOffset? OnSaleAt,
    int? MaxTicketsPerBuyer,
    bool RequiresQueue,
    string Currency,
    IReadOnlyList<SessionAllocationContract> Allocations) : IntegrationEvent(EventId, OccurredAt, TenantId);
