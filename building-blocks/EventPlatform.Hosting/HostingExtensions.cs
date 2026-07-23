namespace EventPlatform.Hosting;

/// <summary>
/// Registers the standard EventPlatform service defaults shared by every service:
/// JSON options, authentication, OpenAPI, observability, health checks and problem details.
/// This is the single entry point services use so every service is wired the same way.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Adds the common EventPlatform service defaults to the application builder.
    /// Call once from a service's <c>Program.cs</c> before building the app.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="serviceName">The logical service name (used for telemetry and OpenAPI).</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Services.AddDefaultJsonOptions();
        builder.Services.AddEventPlatformAuthentication(builder.Configuration);
        builder.Services.AddEventPlatformOpenApi(serviceName);
        builder.AddDefaultObservability(serviceName);
        builder.Services.AddDefaultHealthChecks();
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpContextAccessor();

        return builder;
    }

    /// <summary>
    /// Wires the common middleware pipeline for an EventPlatform service:
    /// exception handling (problem details), authentication, tenant resolution,
    /// authorization, OpenAPI and health-check endpoints.
    /// Call once after <see cref="WebApplicationBuilder.Build"/>.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseMiddleware<TenantContextMiddleware>();
        app.UseAuthorization();
        app.UseEventPlatformOpenApi();
        app.MapDefaultHealthChecks();

        return app;
    }
}
