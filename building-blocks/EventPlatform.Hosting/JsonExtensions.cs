using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventPlatform.Hosting;

/// <summary>
/// Standard System.Text.Json settings shared across all services so every service
/// serializes identically: camelCase, string enums, skip nulls, case-insensitive reads.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// The shared serializer options for non-HTTP serialization (e.g., event payloads, outbox).
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = Create();

    /// <summary>Configures Minimal API HTTP JSON to use the shared conventions.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddDefaultJsonOptions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
