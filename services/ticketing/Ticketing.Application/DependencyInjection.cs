namespace Ticketing.Application;

/// <summary>Registers the Ticketing application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the Ticketing application services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTicketingApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<TicketIssuingService>();
        services.AddScoped<TicketVoidingService>();
        services.AddScoped<EventScanContextProvisioningService>();

        return services;
    }
}
