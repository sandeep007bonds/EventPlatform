using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EventPlatform.Hosting;

/// <summary>
/// OpenTelemetry tracing, metrics and logging defaults with OTLP export.
/// The OTLP endpoint is taken from the standard <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> variable.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds OpenTelemetry traces, metrics and structured logs for the service,
    /// exporting via OTLP, with ASP.NET Core, HttpClient and runtime instrumentation.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="serviceName">The service name recorded on the telemetry resource.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static WebApplicationBuilder AddDefaultObservability(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());

        return builder;
    }
}
