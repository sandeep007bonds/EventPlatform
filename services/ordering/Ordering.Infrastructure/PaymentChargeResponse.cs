namespace Ordering.Infrastructure;

/// <summary>The Payment charge response deserialized by the payment client.</summary>
/// <param name="Status">The charge outcome name.</param>
/// <param name="PaymentId">The payment id.</param>
/// <param name="ProviderReference">The PSP reference when captured; otherwise <see langword="null"/>.</param>
/// <param name="FailureReason">The failure reason when failed; otherwise <see langword="null"/>.</param>
internal sealed record PaymentChargeResponse(
    string Status,
    Guid PaymentId,
    string? ProviderReference,
    string? FailureReason);
