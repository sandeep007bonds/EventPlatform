namespace Venues.Application;

/// <summary>Registers the Venue application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Venue application services: MediatR handlers, FluentValidation validators, and the
    /// validation pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddVenuesApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
