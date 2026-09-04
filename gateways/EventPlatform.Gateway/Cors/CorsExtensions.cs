namespace EventPlatform.Gateway.Cors;

/// <summary>
/// CORS policy for the frontend origin(s). The gateway is the only place in the platform that
/// needs CORS — the backend services are never called directly from a browser.
/// </summary>
public static class CorsExtensions
{
    /// <summary>Name of the named CORS policy applied to the frontend.</summary>
    public const string FrontendPolicyName = "frontend";

    /// <summary>
    /// Adds a CORS policy allowing only the configured frontend origin(s)
    /// (<c>Cors:AllowedOrigins</c>). No <c>AllowCredentials</c> — the frontend carries its token as
    /// a bearer header, not a cookie, so credentialed CORS isn't needed. Empty by default; never a
    /// wildcard.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST", "PUT", "DELETE")

                    // Response headers are invisible to a cross-origin caller unless exposed, so
                    // without this the SPA could never read back the correlation id it needs to
                    // show a buyer on a failure — the id would exist and be unreachable.
                    .WithExposedHeaders(CorrelationExtensions.HeaderName);
            });
        });

        return services;
    }
}
