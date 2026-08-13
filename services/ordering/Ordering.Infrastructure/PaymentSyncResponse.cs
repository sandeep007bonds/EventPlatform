namespace Ordering.Infrastructure;

/// <summary>The Payments status re-read response deserialized by the payment client.</summary>
/// <param name="Status"><c>NotFound</c>, <c>Pending</c>, <c>Captured</c> or <c>Failed</c>.</param>
internal sealed record PaymentSyncResponse(string Status);
