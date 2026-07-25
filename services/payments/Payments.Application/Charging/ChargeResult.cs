namespace Payments.Application.Charging;

/// <summary>Outcome of a charge, with the payment id and provider/failure details.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="PaymentId">The payment id.</param>
/// <param name="ProviderReference">The PSP reference when captured; otherwise <see langword="null"/>.</param>
/// <param name="FailureReason">The failure reason when failed; otherwise <see langword="null"/>.</param>
public sealed record ChargeResult(
    ChargeOutcome Outcome,
    Guid PaymentId,
    string? ProviderReference,
    string? FailureReason);
