namespace Communication.Infrastructure.Senders;

/// <summary>
/// Dev/test email sender that logs the rendered payload and always succeeds — the local stand-in
/// for a real vendor. Selected whenever no email vendor is configured (see
/// <c>Communication.Infrastructure.DependencyInjection</c>).
/// </summary>
/// <param name="logger">The logger.</param>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    /// <inheritdoc />
    public string Provider => "dev-log";

    /// <inheritdoc />
    public Task<SendResult> SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email to {ToAddress}: subject={Subject} body={HtmlBody}",
            toAddress,
            subject,
            htmlBody);

        return Task.FromResult(new SendResult(Succeeded: true, ProviderReference: $"log_{Guid.CreateVersion7():N}", FailureReason: null));
    }
}
