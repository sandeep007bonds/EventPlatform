namespace Inventory.Application.Holds;

/// <summary>
/// Read model for a hold, used by the checkout saga to validate ownership/expiry and to price the
/// order lines.
/// </summary>
/// <param name="HoldId">The hold id.</param>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="CatalogEventId">The event the seats belong to — the whole run.</param>
/// <param name="EventSessionId">
/// The performance the seats belong to, and the grain the inventory actually has (ADR-0039).
/// <see cref="CatalogEventId"/> travels alongside it because promo codes and the per-buyer cap are
/// decided for the run, not for one night, and Ordering needs both without a call back to Catalog.
/// </param>
/// <param name="UserId">The buyer who owns the hold.</param>
/// <param name="Status">Hold status name (<c>Active</c>, <c>Converted</c>, <c>Released</c>).</param>
/// <param name="ExpiresAt">When the hold expires (UTC).</param>
/// <param name="TotalMinor">Total price of the held seats, in minor units.</param>
/// <param name="Lines">The held seats and their prices.</param>
public sealed record HoldView(
    Guid HoldId,
    Guid TenantId,
    Guid CatalogEventId,
    Guid EventSessionId,
    Guid UserId,
    string Status,
    DateTimeOffset ExpiresAt,
    long TotalMinor,
    IReadOnlyList<HoldLineView> Lines);
