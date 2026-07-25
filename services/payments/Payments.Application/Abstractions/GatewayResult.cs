namespace Payments.Application.Abstractions;

/// <summary>Result of a payment-gateway charge.</summary>
/// <param name="Succeeded">Whether the charge succeeded.</param>
/// <param name="Reference">The provider reference when succeeded; otherwise <see langword="null"/>.</param>
/// <param name="FailureReason">The failure reason when not succeeded; otherwise <see langword="null"/>.</param>
public sealed record GatewayResult(bool Succeeded, string? Reference, string? FailureReason);
