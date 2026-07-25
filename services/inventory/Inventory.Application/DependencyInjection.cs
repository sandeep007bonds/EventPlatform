namespace Inventory.Application;

/// <summary>Registers the Inventory application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the Inventory application services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new HoldOptions());
        services.AddSingleton(new HoldReaperOptions());
        services.AddScoped<InventoryProvisioningService>();
        services.AddScoped<HoldService>();

        return services;
    }
}
