namespace Ordering.Api.Endpoints;

/// <summary>Read model returned for one order in a list (no lines — see <see cref="OrderResponse"/> for that).</summary>
/// <param name="Id">Order id.</param>
/// <param name="Status">Order status name.</param>
/// <param name="TotalMinor">Order total in minor currency units.</param>
/// <param name="Currency">Pricing currency (ISO 4217).</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="CreatedAt">When the order was created (UTC).</param>
public sealed record OrderSummaryResponse(
    Guid Id,
    string Status,
    long TotalMinor,
    string Currency,
    Guid CatalogEventId,
    DateTimeOffset CreatedAt);
