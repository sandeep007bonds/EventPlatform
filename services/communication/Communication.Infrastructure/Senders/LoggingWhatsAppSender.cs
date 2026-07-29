namespace Communication.Infrastructure.Senders;

/// <summary>
/// Dev/test WhatsApp sender that logs the payload and always succeeds — the local stand-in for a
/// real vendor. Selected whenever no WhatsApp vendor is configured.
/// </summary>
/// <param name="logger">The logger.</param>
internal sealed class LoggingWhatsAppSender(ILogger<LoggingWhatsAppSender> logger) : IWhatsAppSender
{
    /// <inheritdoc />
    public string Provider => "dev-log";

    /// <inheritdoc />
    public Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("WhatsApp to {ToPhoneNumber}: {Body}", toPhoneNumber, body);

        return Task.FromResult(new SendResult(Succeeded: true, ProviderReference: $"log_{Guid.CreateVersion7():N}", FailureReason: null));
    }
}
