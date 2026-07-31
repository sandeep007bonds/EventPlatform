namespace Communication.Infrastructure.Senders;

/// <summary>
/// Azure Communication Services (Advanced Messaging)-backed <see cref="IWhatsAppSender"/>. Selected
/// when <c>Communication:WhatsApp:Provider</c> is <c>"Acs"</c> and both a connection string and a
/// WhatsApp channel registration id are configured.
/// </summary>
internal sealed class AcsWhatsAppSender : IWhatsAppSender
{
    private readonly NotificationMessagesClient client;
    private readonly string channelRegistrationId;

    /// <summary>Creates the sender for the given ACS connection string and WhatsApp channel.</summary>
    /// <param name="connectionString">The ACS connection string (from configuration).</param>
    /// <param name="channelRegistrationId">The ACS Advanced Messaging WhatsApp channel registration id.</param>
    public AcsWhatsAppSender(string connectionString, string channelRegistrationId)
    {
        client = new NotificationMessagesClient(connectionString);
        this.channelRegistrationId = channelRegistrationId;
    }

    /// <inheritdoc />
    public string Provider => "acs";

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken)
    {
        var content = new TextNotificationContent(Guid.Parse(channelRegistrationId), [toPhoneNumber], body);

        try
        {
            var response = await client.SendAsync(content, cancellationToken);
            var receipts = response.Value.Receipts;
            var receipt = receipts.Count > 0 ? receipts[0] : null;
            return new SendResult(Succeeded: receipt is not null, ProviderReference: receipt?.MessageId, FailureReason: receipt is null ? "no delivery receipt returned" : null);
        }
        catch (RequestFailedException ex)
        {
            return new SendResult(Succeeded: false, ProviderReference: null, FailureReason: ex.Message);
        }
    }
}
