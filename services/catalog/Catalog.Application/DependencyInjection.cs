namespace Catalog.Application;

/// <summary>Registers the Catalog application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Catalog application services: MediatR handlers, FluentValidation validators,
    /// and the validation pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Shared by the three seat-map handlers, which all turn a section's tier name into a type.
        services.AddScoped<TicketTypeResolver>();

        return services;
    }
}
