namespace Ordering.Api.Endpoints;

/// <summary>Read model returned for a single order.</summary>
/// <param name="Id">Order id.</param>
/// <param name="Status">Order status name.</param>
/// <param name="TotalMinor">Order total in minor currency units.</param>
/// <param name="Currency">Pricing currency (ISO 4217).</param>
/// <param name="CatalogEventId">The show/event the seats belong to.</param>
/// <param name="HoldId">The hold the order was created from.</param>
/// <param name="Lines">The order lines.</param>
/// <param name="PaymentClientSecret">
/// The Stripe PaymentIntent client secret, while the order is awaiting payment — lets a buyer who
/// reloads mid-authentication (or a redirect-return page) resume Payment Element without a fresh
/// checkout call. <see langword="null"/> once the order reaches a terminal status.
/// </param>
public sealed record OrderResponse(
    Guid Id,
    string Status,
    long TotalMinor,
    string Currency,
    Guid CatalogEventId,
    Guid HoldId,
    IReadOnlyList<OrderLineResponse> Lines,
    string? PaymentClientSecret);
