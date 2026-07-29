namespace Communication.Application.Sending;

/// <summary>Outcome of a <see cref="NotificationSendService"/> send.</summary>
/// <param name="Succeeded"><see langword="true"/> if the vendor accepted the send.</param>
/// <param name="DeliveryLogId">The id of the delivery-log row recorded for this attempt.</param>
/// <param name="Provider">The vendor that handled the send.</param>
/// <param name="ProviderReference">The vendor's own reference for the send, if any.</param>
/// <param name="FailureReason">Why the send failed, if it did.</param>
public sealed record SendNotificationResult(bool Succeeded, Guid DeliveryLogId, string Provider, string? ProviderReference, string? FailureReason);
