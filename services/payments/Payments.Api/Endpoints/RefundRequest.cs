namespace Payments.Api.Endpoints;

/// <summary>Request body for refunding an order's payment (checkout-saga compensation).</summary>
/// <param name="OrderId">The order to refund.</param>
public sealed record RefundRequest(Guid OrderId);
