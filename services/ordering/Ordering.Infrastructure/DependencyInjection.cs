namespace Ordering.Infrastructure;

/// <summary>Registers the Ordering infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Ordering EF Core context (PostgreSQL), repository, the Inventory hold client, the
    /// (stub) payment client, and the transactional outbox. The connection string is read from the
    /// <c>ordering</c> connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOrderingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("ordering");

        services.AddDbContext<OrderingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IHoldClient, DaprHoldClient>();
        services.AddScoped<IPaymentClient, DaprPaymentClient>();
        services.AddScoped<ITicketClient, DaprTicketClient>();
        services.AddOutbox<OrderingDbContext>();

        return services;
    }
}
