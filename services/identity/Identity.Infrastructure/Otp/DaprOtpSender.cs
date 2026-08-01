namespace Identity.Infrastructure.Otp;

/// <summary>
/// Sends the OTP by SMS via Communication's <c>POST /v1/notifications/send</c>, over Dapr service
/// invocation (app-id <c>communication</c>) — same client shape as Ordering's
/// <c>DaprHoldClient</c>/<c>DaprPaymentClient</c>. The request body is built as an anonymous
/// object (matching Communication's field names) rather than referencing
/// <c>Communication.Domain.NotificationChannel</c> directly, so Identity has no project
/// dependency on Communication.
/// </summary>
internal sealed class DaprOtpSender : IOtpSender
{
    private const string CommunicationAppId = "communication";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Communication's SendNotificationRequest.TenantId must be non-empty (validated) but is only
    // used for delivery-log reporting, not authorization. OTP sends happen before any real buyer
    // tenant is known (buyers aren't tenant-scoped — ADR-0022), so this is a documented,
    // non-Guid.Empty placeholder attributing every OTP SMS to a platform pseudo-tenant in
    // Communication's delivery log, distinct from any real organizer tenant id.
    private static readonly Guid PlatformTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <inheritdoc />
    public async Task<bool> SendAsync(string phoneNumber, string code, int expiresInMinutes, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(CommunicationAppId);
        var body = $"Your EventPlatform verification code is {code}. It expires in {expiresInMinutes} minutes.";

        using var response = await http.PostAsJsonAsync(
            "v1/notifications/send",
            new
            {
                tenantId = PlatformTenantId,
                channel = "Sms",
                recipient = phoneNumber,
                templateKey = (string?)null,
                placeholders = (IReadOnlyDictionary<string, string>?)null,
                body,
                correlationId = (Guid?)null,
            },
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result = await response.Content.ReadFromJsonAsync<SendNotificationResponseDto>(JsonOptions, cancellationToken);
        return result?.Succeeded ?? false;
    }

    private sealed record SendNotificationResponseDto(bool Succeeded, Guid DeliveryLogId, string Provider, string? ProviderReference, string? FailureReason);
}
