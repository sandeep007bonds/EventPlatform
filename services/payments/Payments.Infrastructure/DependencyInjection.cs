namespace Payments.Infrastructure;

/// <summary>Registers the Payments infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Payments EF Core context (PostgreSQL), repository, the payment gateway, and the
    /// transactional outbox. The connection string is read from the <c>payments</c> connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("payments");

        services.AddDbContext<PaymentDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Use the real Stripe gateway when a secret key is configured, otherwise the dev
        // simulator. The key comes from Key Vault or user-secrets and is never committed.
        var stripeSecretKey = configuration["Payments:Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();
        }
        else
        {
            services.AddSingleton<IPaymentGateway>(new StripePaymentGateway(stripeSecretKey));
        }

        // Inbound Stripe webhooks (async capture / 3-D Secure / refunds) are only accepted when a
        // signing secret is configured; without it the endpoint has nothing to verify against and
        // returns 503. The secret is never committed (Key Vault / user-secrets / env).
        var stripeWebhookSecret = configuration["Payments:Stripe:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(stripeWebhookSecret))
        {
            services.AddSingleton<IPaymentWebhookGateway>(new StripeWebhookGateway(stripeWebhookSecret));
        }

        services.AddOutbox<PaymentDbContext>();

        // Closes out payments the buyer never came back to finish — the last route by which a
        // captured payment could otherwise be stranded with no order behind it (ADR-0028).
        services.AddHostedService<StalePaymentReconciler>();

        return services;
    }
}
