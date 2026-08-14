namespace Payments.Infrastructure;

/// <summary>
/// Says out loud, once at startup, which payment gateway this process actually resolved.
/// <para>
/// The gateway is chosen from configuration, and the fallback is <see cref="SimulatedPaymentGateway"/>
/// — which synthesises a successful capture. That is exactly right in local development and
/// catastrophic in a deployed environment: checkout would confirm orders and issue real tickets
/// without ever charging anyone, and nothing in the logs would say so. Failing to start instead is
/// worse, since it would take down a dev cluster that legitimately has no Stripe account. So the
/// simulator stays a valid choice, and this makes it a loud one.
/// </para>
/// </summary>
/// <param name="gateway">The gateway that was resolved.</param>
/// <param name="webhookGateways">
/// The webhook verifier, as an enumerable because it is registered only when a signing secret is
/// configured. A nullable constructor parameter would not do: the default container throws for an
/// unregistered service whatever its nullability, whereas an unfilled enumerable is simply empty.
/// </param>
/// <param name="environment">The host environment, used to decide how alarming this is.</param>
/// <param name="logger">The logger.</param>
internal sealed class PaymentGatewayStartupLog(
    IPaymentGateway gateway,
    IEnumerable<IPaymentWebhookGateway> webhookGateways,
    IHostEnvironment environment,
    ILogger<PaymentGatewayStartupLog> logger)
    : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var simulated = gateway is SimulatedPaymentGateway;

        if (simulated && !environment.IsDevelopment())
        {
            logger.LogWarning(
                "Payments is running the SIMULATED gateway in the {Environment} environment: no Stripe " +
                "secret key is configured, so checkout will confirm orders and issue tickets WITHOUT " +
                "charging anyone. Set the stripe-secret-key Key Vault secret to a real sk_ key.",
                environment.EnvironmentName);
        }
        else
        {
            logger.LogInformation(
                "Payments gateway: {Gateway}. Stripe webhook verification: {WebhookState}.",
                simulated ? "simulated" : "Stripe",
                webhookGateways.Any() ? "enabled" : "disabled (no signing secret)");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
