namespace Catalog.Infrastructure;

/// <summary>Registers the Catalog infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Catalog EF Core context (PostgreSQL), repository, and transactional outbox.
    /// The connection string is read from the <c>catalog</c> connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("catalog");

        services.AddDbContext<CatalogDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseAuditFields(sp));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ISeatMapRepository, SeatMapRepository>();
        services.AddScoped<IEventGroupRepository, EventGroupRepository>();
        services.AddScoped<IEntryGateRepository, EntryGateRepository>();
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
        services.AddOutbox<CatalogDbContext>();

        return services;
    }
}
