namespace Communication.Infrastructure.Senders;

/// <summary>
/// Twilio-backed <see cref="IWhatsAppSender"/>. Selected when <c>Communication:WhatsApp:Provider</c>
/// is <c>"Twilio"</c>. See <see cref="TwilioSmsSender"/> for why this builds its
/// <see cref="ITwilioRestClient"/> from an injected <see cref="HttpClient"/> rather than the SDK's
/// static <c>TwilioClient.Init(...)</c>.
/// </summary>
internal sealed class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly ITwilioRestClient client;
    private readonly string fromNumber;

    /// <summary>Creates the sender for the given Twilio credentials.</summary>
    /// <param name="httpClient">The injected HTTP client backing the Twilio REST client.</param>
    /// <param name="accountSid">The Twilio Account SID (from configuration).</param>
    /// <param name="authToken">The Twilio Auth Token (from configuration).</param>
    /// <param name="fromNumber">The Twilio WhatsApp-enabled sender phone number.</param>
    public TwilioWhatsAppSender(HttpClient httpClient, string accountSid, string authToken, string fromNumber)
    {
        client = new TwilioRestClient(accountSid, authToken, httpClient: httpClient);
        this.fromNumber = fromNumber;
    }

    /// <inheritdoc />
    public string Provider => "twilio";

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        var message = await MessageResource.CreateAsync(
            to: new PhoneNumber($"whatsapp:{toPhoneNumber}"),
            from: new PhoneNumber($"whatsapp:{fromNumber}"),
            body: body,
            client: client);

        return message.ErrorCode is null
            ? new SendResult(Succeeded: true, ProviderReference: message.Sid, FailureReason: null)
            : new SendResult(Succeeded: false, ProviderReference: message.Sid, FailureReason: message.ErrorMessage);
    }
}
