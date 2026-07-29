namespace Communication.Infrastructure.Senders;

/// <summary>
/// Azure Communication Services-backed <see cref="IEmailSender"/>. Selected when
/// <c>Communication:Email:Provider</c> is <c>"Acs"</c> and a connection string is configured.
/// </summary>
internal sealed class AcsEmailSender : IEmailSender
{
    private readonly EmailClient client;
    private readonly string fromAddress;

    /// <summary>Creates the sender for the given ACS connection string.</summary>
    /// <param name="connectionString">The ACS connection string (from configuration).</param>
    /// <param name="fromAddress">The verified ACS sender email address.</param>
    public AcsEmailSender(string connectionString, string fromAddress)
    {
        client = new EmailClient(connectionString);
        this.fromAddress = fromAddress;
    }

    /// <inheritdoc />
    public string Provider => "acs";

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new EmailMessage(fromAddress, toAddress, new EmailContent(subject) { Html = htmlBody });

        try
        {
            var operation = await client.SendAsync(WaitUntil.Completed, message, cancellationToken);
            var succeeded = operation.Value.Status == EmailSendStatus.Succeeded;
            return new SendResult(succeeded, operation.Value.Id, succeeded ? null : operation.Value.Status.ToString());
        }
        catch (RequestFailedException ex)
        {
            return new SendResult(Succeeded: false, ProviderReference: null, FailureReason: ex.Message);
        }
    }
}
