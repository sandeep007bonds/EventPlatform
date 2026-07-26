namespace Ordering.Workflow;

/// <summary>Input to the confirm-order activity.</summary>
/// <param name="OrderId">The order to confirm.</param>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="CatalogEventId">The show/event.</param>
/// <param name="UserId">The buyer.</param>
/// <param name="SeatIds">The purchased seat ids.</param>
public sealed record ConfirmInput(
    Guid OrderId,
    Guid TenantId,
    Guid CatalogEventId,
    Guid UserId,
    IReadOnlyList<Guid> SeatIds);
