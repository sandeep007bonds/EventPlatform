namespace Ordering.Application.Abstractions;

/// <summary>Result of a payment charge.</summary>
/// <param name="Succeeded">Whether the charge succeeded.</param>
/// <param name="PaymentReference">The PSP reference when succeeded; otherwise <see langword="null"/>.</param>
/// <param name="FailureReason">The failure reason when not succeeded; otherwise <see langword="null"/>.</param>
public sealed record PaymentResult(bool Succeeded, string? PaymentReference, string? FailureReason);
