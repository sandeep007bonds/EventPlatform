namespace Communication.Application.Sending;

/// <summary>A request to send one notification over one channel.</summary>
/// <param name="TenantId">Owning tenant, for per-organizer reporting.</param>
/// <param name="Channel">The channel to send over.</param>
/// <param name="Recipient">The recipient email address (Email) or phone number in E.164 (Sms/WhatsApp).</param>
/// <param name="TemplateKey">The template to render. Required for <see cref="Communication.Domain.NotificationChannel.Email"/>; ignored otherwise.</param>
/// <param name="Placeholders">Placeholder values for template rendering. Only used for Email.</param>
/// <param name="Body">The raw message body. Required for Sms/WhatsApp; ignored for Email (which is always template-driven).</param>
/// <param name="CorrelationId">The triggering integration event id, if this send was raised from a subscriber.</param>
public sealed record SendNotificationCommand(
    Guid TenantId,
    NotificationChannel Channel,
    string Recipient,
    string? TemplateKey,
    IReadOnlyDictionary<string, string>? Placeholders,
    string? Body,
    Guid? CorrelationId);
