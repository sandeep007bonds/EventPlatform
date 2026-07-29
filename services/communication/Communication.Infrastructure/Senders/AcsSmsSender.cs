namespace Communication.Infrastructure.Senders;

/// <summary>
/// Azure Communication Services-backed <see cref="ISmsSender"/>. Selected when
/// <c>Communication:Sms:Provider</c> is <c>"Acs"</c> and a connection string is configured.
/// </summary>
internal sealed class AcsSmsSender : ISmsSender
{
    private readonly SmsClient client;
    private readonly string fromNumber;

    /// <summary>Creates the sender for the given ACS connection string.</summary>
    /// <param name="connectionString">The ACS connection string (from configuration).</param>
    /// <param name="fromNumber">The ACS-provisioned sender phone number.</param>
    public AcsSmsSender(string connectionString, string fromNumber)
    {
        client = new SmsClient(connectionString);
        this.fromNumber = fromNumber;
    }

    /// <inheritdoc />
    public string Provider => "acs";

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client.SendAsync(fromNumber, toPhoneNumber, body, cancellationToken: cancellationToken);
            var result = response.Value;
            return new SendResult(result.Successful, result.MessageId, result.Successful ? null : result.ErrorMessage);
        }
        catch (RequestFailedException ex)
        {
            return new SendResult(Succeeded: false, ProviderReference: null, FailureReason: ex.Message);
        }
    }
}
