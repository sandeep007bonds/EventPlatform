namespace Inventory.Application.Provisioning;

/// <summary>Everything a published performance announced, as the provisioner needs it.</summary>
/// <remarks>
/// A parameter object rather than ten positional arguments. Nine of them are ids, dates and flags
/// that a caller could transpose without the compiler noticing, and the list only grows.
/// </remarks>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="EventSessionId">The performance being provisioned.</param>
/// <param name="CatalogEventId">The event it belongs to — kept for the per-run buyer limit.</param>
/// <param name="SeatMapId">The Venue seat map.</param>
/// <param name="SeatMapVersionNumber">The pinned version number.</param>
/// <param name="BookingEndsAt">The performance's enforced booking cutoff (UTC), if any.</param>
/// <param name="OnSaleAt">The event's enforced on-sale start (UTC), if any.</param>
/// <param name="MaxTicketsPerBuyer">The event's per-buyer ticket limit, if any.</param>
/// <param name="RequiresQueue">Whether a Queue admission token is required at hold time.</param>
/// <param name="Allocations">Which block sells as which ticket type, and at what price.</param>
public sealed record ProvisionSessionRequest(
    Guid TenantId,
    Guid EventSessionId,
    Guid CatalogEventId,
    Guid SeatMapId,
    int SeatMapVersionNumber,
    DateTimeOffset? BookingEndsAt,
    DateTimeOffset? OnSaleAt,
    int? MaxTicketsPerBuyer,
    bool RequiresQueue,
    IReadOnlyList<SessionAllocationContract> Allocations);
