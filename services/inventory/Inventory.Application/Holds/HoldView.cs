namespace Inventory.Application.Holds;

/// <summary>
/// Read model for a hold, used by the checkout saga to validate ownership/expiry and to price the
/// order lines.
/// </summary>
/// <param name="HoldId">The hold id.</param>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="UserId">The buyer who owns the hold.</param>
/// <param name="Status">Hold status name (<c>Active</c>, <c>Converted</c>, <c>Released</c>).</param>
/// <param name="ExpiresAt">When the hold expires (UTC).</param>
/// <param name="TotalMinor">Total price of the held seats, in minor units.</param>
/// <param name="Lines">The held seats and their prices.</param>
public sealed record HoldView(
    Guid HoldId,
    Guid TenantId,
    Guid CatalogEventId,
    Guid UserId,
    string Status,
    DateTimeOffset ExpiresAt,
    long TotalMinor,
    IReadOnlyList<HoldLineView> Lines);
