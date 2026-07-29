namespace Communication.Api.Endpoints;

/// <summary>Request body for <c>POST /v1/notifications/send</c>.</summary>
/// <param name="TenantId">Owning tenant, for per-organizer reporting.</param>
/// <param name="Channel">The channel to send over.</param>
/// <param name="Recipient">The recipient email address (Email) or phone number in E.164 (Sms/WhatsApp).</param>
/// <param name="TemplateKey">The template to render. Required for Email; ignored otherwise.</param>
/// <param name="Placeholders">Placeholder values for template rendering. Only used for Email.</param>
/// <param name="Body">The raw message body. Required for Sms/WhatsApp; ignored for Email.</param>
/// <param name="CorrelationId">An optional caller-supplied correlation id for the delivery-log row.</param>
public sealed record SendNotificationRequest(
    Guid TenantId,
    NotificationChannel Channel,
    string Recipient,
    string? TemplateKey,
    IReadOnlyDictionary<string, string>? Placeholders,
    string? Body,
    Guid? CorrelationId);
