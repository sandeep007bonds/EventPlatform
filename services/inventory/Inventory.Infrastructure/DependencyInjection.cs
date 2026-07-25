namespace Inventory.Infrastructure;

/// <summary>Registers the Inventory infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Inventory EF Core context (PostgreSQL), repository, the Catalog seat-map client,
    /// and the transactional outbox. The connection string is read from the <c>inventory</c>
    /// connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("inventory");
        var redisConnection = configuration.GetConnectionString("redis") ?? "localhost:6380";

        services.AddDbContext<InventoryDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISeatMapClient, DaprSeatMapClient>();

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IHoldStore, RedisHoldStore>();

        services.AddOutbox<InventoryDbContext>();

        return services;
    }
}
