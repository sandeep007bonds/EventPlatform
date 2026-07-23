namespace EventPlatform.Hosting;

/// <summary>
/// Liveness and readiness health-check defaults. AKS/Argo probes depend on these
/// endpoints being present in every service.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>Tag applied to checks that gate readiness (a service being able to serve traffic).</summary>
    public const string ReadyTag = "ready";

    /// <summary>Registers the default health-check services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHealthChecks();
        return services;
    }

    /// <summary>
    /// Maps <c>/health/live</c> (liveness — process is up) and <c>/health/ready</c>
    /// (readiness — dependencies tagged <see cref="ReadyTag"/> are healthy).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication MapDefaultHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });

        return app;
    }
}
