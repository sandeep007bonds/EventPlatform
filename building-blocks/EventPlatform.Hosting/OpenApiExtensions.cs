namespace EventPlatform.Hosting;

/// <summary>
/// OpenAPI/Swagger defaults using the built-in .NET OpenAPI document generator
/// plus the Scalar API reference UI (served outside Production).
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>Adds the OpenAPI document generator for the service, titled by service name.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">The logical service name shown in the document title.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddEventPlatformOpenApi(this IServiceCollection services, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = $"EventPlatform · {serviceName} API";
                document.Info.Version = "v1";
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Maps the OpenAPI JSON endpoint (<c>/openapi/v1.json</c>) and, outside Production,
    /// the Scalar API reference UI (<c>/scalar/v1</c>).
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication UseEventPlatformOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapOpenApi();

        if (!app.Environment.IsProduction())
        {
            app.MapScalarApiReference();
        }

        return app;
    }
}
