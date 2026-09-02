namespace Venues.Infrastructure;

/// <summary>Registers the Venue infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Venue EF Core context (PostgreSQL), repositories, and transactional outbox. The
    /// connection string is read from the <c>venue</c> connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddVenuesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("venue");

        services.AddDbContext<VenuesDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseAuditFields(sp));
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<ISeatMapRepository, SeatMapRepository>();
        services.AddOutbox<VenuesDbContext>();

        return services;
    }
}
