namespace Communication.Infrastructure.Senders;

/// <summary>
/// Twilio-backed <see cref="ISmsSender"/>. Selected when <c>Communication:Sms:Provider</c> is
/// <c>"Twilio"</c>. Twilio's SDK does not integrate with <see cref="IHttpClientFactory"/> on its
/// own, so this is registered via <c>services.AddHttpClient&lt;ISmsSender, TwilioSmsSender&gt;()</c>
/// and builds its <see cref="ITwilioRestClient"/> from the injected <see cref="HttpClient"/>,
/// rather than the SDK's process-wide static <c>TwilioClient.Init(...)</c>.
/// </summary>
internal sealed class TwilioSmsSender : ISmsSender
{
    private readonly ITwilioRestClient client;
    private readonly string fromNumber;

    /// <summary>Creates the sender for the given Twilio credentials.</summary>
    /// <param name="httpClient">The injected HTTP client backing the Twilio REST client.</param>
    /// <param name="accountSid">The Twilio Account SID (from configuration).</param>
    /// <param name="authToken">The Twilio Auth Token (from configuration).</param>
    /// <param name="fromNumber">The Twilio-provisioned sender phone number.</param>
    public TwilioSmsSender(HttpClient httpClient, string accountSid, string authToken, string fromNumber)
    {
        client = new TwilioRestClient(accountSid, authToken, httpClient: new SystemNetHttpClient(httpClient));
        this.fromNumber = fromNumber;
    }

    /// <inheritdoc />
    public string Provider => "twilio";

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        var message = await MessageResource.CreateAsync(
            to: new PhoneNumber(toPhoneNumber),
            from: new PhoneNumber(fromNumber),
            body: body,
            client: client);

        return message.ErrorCode is null
            ? new SendResult(Succeeded: true, ProviderReference: message.Sid, FailureReason: null)
            : new SendResult(Succeeded: false, ProviderReference: message.Sid, FailureReason: message.ErrorMessage);
    }
}
