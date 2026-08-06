namespace Queue.Application;

/// <summary>Registers the Queue application layer's handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the join/status handlers and the settings provisioning service.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddQueueApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<JoinQueueHandler>();
        services.AddScoped<QueueStatusHandler>();
        services.AddScoped<QueueSettingsProvisioningService>();

        return services;
    }
}
