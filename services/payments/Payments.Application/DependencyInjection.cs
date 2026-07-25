namespace Payments.Application;

/// <summary>Registers the Payments application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the Payments application services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddPaymentsApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<PaymentService>();

        return services;
    }
}
