namespace Ordering.Workflow;

/// <summary>Output of the charge activity.</summary>
/// <param name="Succeeded">Whether the charge succeeded.</param>
/// <param name="FailureReason">The failure reason when not succeeded; otherwise <see langword="null"/>.</param>
public sealed record ChargeOutput(bool Succeeded, string? FailureReason);
