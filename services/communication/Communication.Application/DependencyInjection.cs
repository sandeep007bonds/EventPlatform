namespace Communication.Application;

/// <summary>Registers the Communication application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the Communication application services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCommunicationApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<NotificationSendService>();
        services.AddScoped<IntegrationEventNotificationHandler>();

        return services;
    }
}
