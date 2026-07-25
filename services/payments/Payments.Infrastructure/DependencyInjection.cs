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
        services.AddSingleton<IPaymentGateway, SimulatedPaymentGateway>();
        services.AddOutbox<PaymentDbContext>();

        return services;
    }
}
