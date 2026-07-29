namespace Communication.Domain;

/// <summary>The outbound channel a notification is sent over.</summary>
public enum NotificationChannel
{
    /// <summary>Email, sent template-driven.</summary>
    Email,

    /// <summary>SMS text message.</summary>
    Sms,

    /// <summary>WhatsApp message.</summary>
    WhatsApp,
}
