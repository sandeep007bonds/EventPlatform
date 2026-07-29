namespace Communication.Api.Endpoints;

/// <summary>Response body for <c>POST /v1/notifications/send</c>.</summary>
/// <param name="Succeeded"><see langword="true"/> if the vendor accepted the send.</param>
/// <param name="DeliveryLogId">The id of the recorded delivery-log row.</param>
/// <param name="Provider">The vendor that handled the send.</param>
/// <param name="ProviderReference">The vendor's own reference for the send, if any.</param>
/// <param name="FailureReason">Why the send failed, if it did.</param>
public sealed record SendNotificationResponse(bool Succeeded, Guid DeliveryLogId, string Provider, string? ProviderReference, string? FailureReason);
