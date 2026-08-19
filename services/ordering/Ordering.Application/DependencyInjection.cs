namespace Ordering.Application;

/// <summary>Registers the Ordering application layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the Ordering application services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // CheckoutOptions supplies the fallback currency for the checkout workflow's
        // fetch-event-pricing activity.
        services.AddSingleton(new CheckoutOptions());

        // Scoped, not singleton: it depends on IOrderRepository, which is scoped. Shared by the
        // /v1/checkout/quote endpoint and the saga's own re-check, so the preview and the charge
        // can never disagree about whether a code applies.
        services.AddScoped<PromoCodeEvaluator>();

        return services;
    }
}
