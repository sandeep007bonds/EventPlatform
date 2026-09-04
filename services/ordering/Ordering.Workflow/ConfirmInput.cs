namespace Ordering.Workflow;

/// <summary>Input to the confirm-order activity.</summary>
/// <param name="OrderId">The order to confirm.</param>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="CatalogEventId">The show/event.</param>
/// <param name="EventSessionId">The performance the seats belong to.</param>
/// <param name="UserId">The buyer.</param>
/// <param name="Lines">The purchased lines — reserved seats and/or general-admission quantities.</param>
public sealed record ConfirmInput(
    Guid OrderId,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId,
    Guid UserId,
    IReadOnlyList<OrderLineSummary> Lines);
