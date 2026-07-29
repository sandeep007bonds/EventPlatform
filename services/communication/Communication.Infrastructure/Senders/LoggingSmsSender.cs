namespace Communication.Infrastructure.Senders;

/// <summary>
/// Dev/test SMS sender that logs the payload and always succeeds — the local stand-in for a real
/// vendor. Selected whenever no SMS vendor is configured.
/// </summary>
/// <param name="logger">The logger.</param>
internal sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    /// <inheritdoc />
    public string Provider => "dev-log";

    /// <inheritdoc />
    public Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("SMS to {ToPhoneNumber}: {Body}", toPhoneNumber, body);

        return Task.FromResult(new SendResult(Succeeded: true, ProviderReference: $"log_{Guid.CreateVersion7():N}", FailureReason: null));
    }
}
