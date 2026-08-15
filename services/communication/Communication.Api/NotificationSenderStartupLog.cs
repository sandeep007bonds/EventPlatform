namespace Communication.Api;

/// <summary>
/// Says out loud, once at startup, which sender each channel actually resolved.
/// <para>
/// Each channel is chosen from configuration, and the fallback is a logging sender that writes the
/// message out and reports success without sending anything. That is exactly right in local
/// development and quietly wrong in a deployed environment: buyers would never receive their
/// tickets, organizers would never receive an OTP, and every delivery-log row would say
/// <c>Sent</c>. Refusing to start instead would be worse, since it would take down a dev cluster
/// that legitimately has no Azure Communication Services or Twilio account. So the logging senders
/// stay a valid choice, and this makes them a loud one.
/// </para>
/// <para>
/// This lives in the API host rather than beside the senders because the senders are internal to
/// Communication.Infrastructure, while <c>Provider</c> is on the public port — so the check needs
/// no <c>InternalsVisibleTo</c> and no reference to a hosting package the Infrastructure project
/// does not otherwise need. Mirrors Payments' <c>PaymentGatewayStartupLog</c>.
/// </para>
/// </summary>
/// <param name="emailSender">The resolved email sender.</param>
/// <param name="smsSender">The resolved SMS sender.</param>
/// <param name="whatsAppSender">The resolved WhatsApp sender.</param>
/// <param name="environment">The host environment, used to decide how alarming this is.</param>
/// <param name="logger">The logger.</param>
internal sealed class NotificationSenderStartupLog(
    IEmailSender emailSender,
    ISmsSender smsSender,
    IWhatsAppSender whatsAppSender,
    IHostEnvironment environment,
    ILogger<NotificationSenderStartupLog> logger)
    : IHostedService
{
    /// <summary>The <c>Provider</c> value every logging sender reports.</summary>
    private const string DevLogProvider = "dev-log";

    private const string SimulatedMessage =
        "Communication is running LOGGING-ONLY senders in the {Environment} environment for: {Channels}. " +
        "Nothing is actually delivered — ticket emails and OTP codes are written to the log and " +
        "recorded as Sent. Configure the Communication:{{Channel}}:Provider keys and their ACS/Twilio " +
        "credentials to send for real.";

    private const string ResolvedMessage =
        "Communication senders — email: {Email}, SMS: {Sms}, WhatsApp: {WhatsApp}.";

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var channels = new[]
        {
            (Name: "email", emailSender.Provider),
            (Name: "SMS", smsSender.Provider),
            (Name: "WhatsApp", whatsAppSender.Provider),
        };

        var simulated = channels
            .Where(channel => string.Equals(channel.Provider, DevLogProvider, StringComparison.Ordinal))
            .Select(channel => channel.Name)
            .ToArray();

        if (simulated.Length > 0 && !environment.IsDevelopment())
        {
            logger.LogWarning(SimulatedMessage, environment.EnvironmentName, string.Join(", ", simulated));
        }
        else
        {
            logger.LogInformation(
                ResolvedMessage,
                emailSender.Provider,
                smsSender.Provider,
                whatsAppSender.Provider);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
