namespace Queue.Infrastructure;

/// <summary>
/// Registers the Queue infrastructure layer: persistence, the Redis waiting-room store, HMAC
/// admission-token issuance, and the admission background service. Like Communication/Identity,
/// this does NOT call <c>AddOutbox</c> — Queue never publishes an integration event.
/// </summary>
public static class DependencyInjection
{
    // Dev-only fallback, mirrors Jwt:DevSigningKey/Identity:Otp:HmacKey's committed-plaintext
    // posture. Must be the SAME literal value Inventory's DependencyInjection.cs falls back to —
    // this is a genuinely shared secret between the two services (see ADR-0026).
    private const string DevHmacKey = "eventplatform-dev-queue-admission-hmac-key-not-a-secret";

    /// <summary>Adds the Queue infrastructure services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddQueueInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("queue");
        var redisConnection = configuration.GetConnectionString("redis") ?? "localhost:6380";

        services.AddDbContext<QueueDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseAuditFields(sp));
        services.AddScoped<IQueueSettingsRepository, QueueSettingsRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddScoped<IQueueStore, RedisQueueStore>();

        var hmacKey = configuration["QueueAdmission:HmacKey"] ?? DevHmacKey;
        services.AddSingleton<IAdmissionTokenIssuer>(new HmacAdmissionTokenIssuer(Encoding.UTF8.GetBytes(hmacKey)));

        services.AddSingleton(new QueueAdmissionOptions());
        services.AddSingleton(new QueueRateLimitOptions());
        services.AddScoped<IJoinRateLimiter, RedisJoinRateLimiter>();
        services.AddHostedService<QueueAdmissionController>();

        return services;
    }
}
