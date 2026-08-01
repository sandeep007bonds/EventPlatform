namespace Communication.Domain;

/// <summary>Well-known template keys. Add a new constant alongside its embedded resource files.</summary>
public static class TemplateKeys
{
    /// <summary>One-time-password verification code (the only template shipped in Phase 1).</summary>
    public const string OtpCode = "otp-code";

    /// <summary>Order confirmation — recorded on delivery-log rows only; not yet rendered (no recipient resolver exists yet).</summary>
    public const string OrderConfirmed = "order-confirmed";

    /// <summary>Ticket issued — recorded on delivery-log rows only; not yet rendered.</summary>
    public const string TicketIssued = "ticket-issued";

    /// <summary>One combined email listing every ticket minted for an order — rendered and sent when a buyer email is present.</summary>
    public const string OrderTickets = "order-tickets";
}
