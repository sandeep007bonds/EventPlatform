namespace Communication.Application.Abstractions;

/// <summary>Outcome of one vendor send attempt.</summary>
/// <param name="Succeeded"><see langword="true"/> if the vendor accepted the send.</param>
/// <param name="ProviderReference">The vendor's own reference for the send, if any.</param>
/// <param name="FailureReason">Why the send failed, if <paramref name="Succeeded"/> is <see langword="false"/>.</param>
public sealed record SendResult(bool Succeeded, string? ProviderReference, string? FailureReason);
