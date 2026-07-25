namespace Payments.Api.Endpoints;

/// <summary>Response for a charge attempt.</summary>
/// <param name="Status">The charge outcome name (<c>Captured</c> or <c>Failed</c>).</param>
/// <param name="PaymentId">The payment id.</param>
/// <param name="ProviderReference">The PSP reference when captured; otherwise <see langword="null"/>.</param>
/// <param name="FailureReason">The failure reason when failed; otherwise <see langword="null"/>.</param>
public sealed record ChargeResponse(
    string Status,
    Guid PaymentId,
    string? ProviderReference,
    string? FailureReason);
