namespace Inventory.Infrastructure;

/// <summary>Registers the Inventory infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    // Dev-only fallback, mirrors Jwt:DevSigningKey/Identity:Otp:HmacKey's committed-plaintext
    // posture. Must be the SAME literal value Queue's DependencyInjection.cs falls back to — this
    // is a genuinely shared secret between the two services (see ADR-0026).
    private const string DevQueueAdmissionHmacKey = "eventplatform-dev-queue-admission-hmac-key-not-a-secret";

    /// <summary>
    /// Adds the Inventory EF Core context (PostgreSQL), repository, the Catalog seat-map client,
    /// the Queue admission-token validator, and the transactional outbox. The connection string is
    /// read from the <c>inventory</c> connection string.
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

        services.AddDbContext<InventoryDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseAuditFields(sp));
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISeatMapClient, DaprSeatMapClient>();

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IHoldStore, RedisHoldStore>();
        services.AddHostedService<ExpiredHoldReaper>();
        services.AddHostedService<InventoryReconciler>();

        var queueAdmissionHmacKey = configuration["QueueAdmission:HmacKey"] ?? DevQueueAdmissionHmacKey;
        services.AddSingleton<IQueueAdmissionTokenValidator>(
            new HmacQueueAdmissionTokenValidator(Encoding.UTF8.GetBytes(queueAdmissionHmacKey)));

        services.AddOutbox<InventoryDbContext>();
        services.AddDeadLetters<InventoryDbContext>();

        return services;
    }
}
