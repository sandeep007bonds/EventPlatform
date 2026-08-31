namespace Payments.Api.Endpoints;

/// <summary>Request body for refunding an order's payment (checkout-saga compensation).</summary>
/// <param name="OrderId">The order to refund.</param>
/// <param name="AmountMinor">
/// How much to return, in minor units, or <see langword="null"/> for the whole captured amount.
/// Ordering supplies it, because only Ordering knows what part of the total was a non-refundable
/// booking fee.
/// </param>
public sealed record RefundRequest(Guid OrderId, long? AmountMinor = null);
