namespace EventPlatform.Contracts.Ticketing;

/// <summary>
/// Published by the Ticketing service once every ticket for an order has been minted. Consumed by
/// notifications to send a single combined ticket-delivery email, rather than one per <see cref="TicketIssued"/>.
/// </summary>
/// <param name="EventId">Unique id of this event instance.</param>
/// <param name="OccurredAt">UTC instant at which the event occurred.</param>
/// <param name="TenantId">The tenant (organizer) the tickets belong to.</param>
/// <param name="OrderId">The order the tickets were issued for.</param>
/// <param name="CatalogEventId">The show/event the tickets admit to.</param>
/// <param name="EventSessionId">
/// The performance the seats belong to — the grain inventory, orders and tickets are keyed by
/// (ADR-0039). <see cref="CatalogEventId"/> travels alongside it because the per-buyer ticket
/// limit is counted across the whole run.
/// </param>
/// <param name="UserId">The ticket holder.</param>
/// <param name="BuyerEmail">The email address the buyer provided at checkout for ticket delivery, if any.</param>
/// <param name="Tickets">Every ticket minted for the order.</param>
public sealed record OrderTicketsIssued(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    Guid OrderId,
    Guid CatalogEventId,
    Guid EventSessionId,
    Guid UserId,
    string? BuyerEmail,
    IReadOnlyList<IssuedTicketSummary> Tickets) : IntegrationEvent(EventId, OccurredAt, TenantId);
