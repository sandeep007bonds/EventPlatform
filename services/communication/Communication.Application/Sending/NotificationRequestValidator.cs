namespace Communication.Application.Sending;

/// <summary>
/// Validates a <see cref="SendNotificationCommand"/> before it reaches <see cref="NotificationSendService"/>.
/// Plain hand-written checks — no FluentValidation, matching the rest of this lean, MediatR-free service.
/// </summary>
public static class NotificationRequestValidator
{
    /// <summary>Validates a send request.</summary>
    /// <param name="command">The request to validate.</param>
    /// <returns>Validation error messages; empty if the request is valid.</returns>
    public static IReadOnlyList<string> Validate(SendNotificationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var errors = new List<string>();

        if (command.TenantId == Guid.Empty)
        {
            errors.Add("tenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Recipient))
        {
            errors.Add("recipient is required.");
        }

        switch (command.Channel)
        {
            case NotificationChannel.Email:
                if (string.IsNullOrWhiteSpace(command.TemplateKey))
                {
                    errors.Add("templateKey is required for the Email channel.");
                }

                break;
            case NotificationChannel.Sms:
            case NotificationChannel.WhatsApp:
                if (string.IsNullOrWhiteSpace(command.Body))
                {
                    errors.Add("body is required for the Sms and WhatsApp channels.");
                }

                break;
            default:
                errors.Add($"Unknown channel '{command.Channel}'.");
                break;
        }

        return errors;
    }
}
