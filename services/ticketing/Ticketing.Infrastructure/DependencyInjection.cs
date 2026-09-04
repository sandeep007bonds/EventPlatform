namespace Ticketing.Infrastructure;

/// <summary>Registers the Ticketing infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Ticketing EF Core context (PostgreSQL), repository, and the transactional outbox.
    /// The connection string is read from the <c>ticketing</c> connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTicketingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("ticketing");

        services.AddDbContext<TicketingDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseAuditFields(sp));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ISessionScanContextRepository, SessionScanContextRepository>();
        services.AddScoped<IVenueGateMapClient, DaprVenueGateMapClient>();
        services.AddScoped<IInventoryGaClient, DaprInventoryGaClient>();
        services.AddOutbox<TicketingDbContext>();
        services.AddDeadLetters<TicketingDbContext>();

        return services;
    }
}
