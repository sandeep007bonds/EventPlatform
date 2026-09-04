namespace Communication.Application.Sending;

/// <summary>
/// Sends a notification over one of the three channels and records the outcome to the delivery
/// log. Callers must validate the command with <see cref="NotificationRequestValidator"/> first —
/// this service assumes a valid, channel-consistent command.
/// </summary>
/// <param name="emailSender">The configured email vendor (or dev/logging fallback).</param>
/// <param name="smsSender">The configured SMS vendor (or dev/logging fallback).</param>
/// <param name="whatsAppSender">The configured WhatsApp vendor (or dev/logging fallback).</param>
/// <param name="templates">The template store, for Email's template-driven rendering.</param>
/// <param name="renderer">The placeholder renderer, for Email's template-driven rendering.</param>
/// <param name="notifications">The delivery-log/dedup-ledger repository.</param>
/// <param name="correlation">The chain of work this send belongs to, stamped on the delivery log.</param>
public sealed class NotificationSendService(
    IEmailSender emailSender,
    ISmsSender smsSender,
    IWhatsAppSender whatsAppSender,
    ITemplateStore templates,
    ITemplateRenderer renderer,
    INotificationRepository notifications,
    ICorrelationContext correlation)
{
    /// <summary>Sends the notification described by <paramref name="command"/>.</summary>
    /// <param name="command">The validated send request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The send outcome.</returns>
    /// <exception cref="InvalidOperationException">The requested template key does not exist.</exception>
    public async Task<SendNotificationResult> SendAsync(SendNotificationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string provider;
        SendResult result;

        switch (command.Channel)
        {
            case NotificationChannel.Email:
                (provider, result) = await SendEmailAsync(command, cancellationToken);
                break;
            case NotificationChannel.Sms:
                provider = smsSender.Provider;
                result = await smsSender.SendAsync(command.Recipient, command.Body!, cancellationToken);
                break;
            case NotificationChannel.WhatsApp:
                provider = whatsAppSender.Provider;
                result = await whatsAppSender.SendAsync(command.Recipient, command.Body!, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), command.Channel, "Unknown notification channel.");
        }

        var logEntry = result.Succeeded
            ? DeliveryLogEntry.Sent(command.TenantId, command.Channel, command.Recipient, command.TemplateKey, provider, result.ProviderReference, correlation.CorrelationId, command.CausationId)
            : DeliveryLogEntry.Failed(command.TenantId, command.Channel, command.Recipient, command.TemplateKey, provider, result.FailureReason ?? "unknown failure", correlation.CorrelationId, command.CausationId);

        notifications.AddDeliveryLog(logEntry);
        await notifications.SaveChangesAsync(cancellationToken);

        return new SendNotificationResult(result.Succeeded, logEntry.Id, provider, result.ProviderReference, result.FailureReason);
    }

    private async Task<(string Provider, SendResult Result)> SendEmailAsync(SendNotificationCommand command, CancellationToken cancellationToken)
    {
        var template = await templates.GetAsync(command.TemplateKey!, cancellationToken);
        if (template is null)
        {
            throw new InvalidOperationException($"No template registered for key '{command.TemplateKey}'.");
        }

        var placeholders = command.Placeholders ?? new Dictionary<string, string>();
        var subject = renderer.Render(template.Subject, placeholders);
        var htmlBody = renderer.Render(template.Body, placeholders);

        var result = await emailSender.SendAsync(command.Recipient, subject, htmlBody, cancellationToken);
        return (emailSender.Provider, result);
    }
}
