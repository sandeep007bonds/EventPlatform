namespace EventPlatform.Hosting;

/// <summary>
/// Standard System.Text.Json settings shared across all services so every service
/// serializes identically: camelCase, string enums, case-insensitive reads.
/// </summary>
/// <remarks>
/// HTTP responses write nulls explicitly; the outbox does not. That difference is deliberate.
/// <para>
/// A client cannot tell "the field is null" from "the field does not exist", and every hand-written
/// TypeScript type in the SPA declares these fields as <c>| null</c>. Omitting them made the wire
/// disagree with the contract the client was written against: a missing <c>maxPerBuyer</c> arrived
/// as <c>undefined</c>, so a <c>=== null</c> test for "no cap" failed and rendered "undefined per
/// buyer" — and the same mismatch made checkout mount an empty payment form instead of skipping
/// payment. Reading is unaffected either way, since a missing property and an explicit null both
/// deserialize to null.
/// </para>
/// <para>
/// The outbox keeps <see cref="JsonIgnoreCondition.WhenWritingNull"/>: those payloads are persisted
/// and replayed, only ever read back by C#, and there is no client to mislead.
/// </para>
/// </remarks>
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
