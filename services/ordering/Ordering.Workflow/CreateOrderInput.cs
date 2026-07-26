namespace Ordering.Workflow;

/// <summary>Input to the create-order activity.</summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="UserId">The buyer.</param>
/// <param name="HoldId">The hold being purchased.</param>
/// <param name="IdempotencyKey">Idempotency key.</param>
/// <param name="CatalogEventId">The show/event.</param>
/// <param name="Lines">The held seats and their prices.</param>
public sealed record CreateOrderInput(
    Guid TenantId,
    Guid UserId,
    Guid HoldId,
    string IdempotencyKey,
    Guid CatalogEventId,
    IReadOnlyList<HoldLineSnapshot> Lines);
