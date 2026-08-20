namespace EventPlatform.Hosting;

/// <summary>
/// Authentication and authorization defaults. Adds JWT bearer validation and
/// registers the per-request <see cref="ITenantContext"/> populated from the token.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT bearer authentication configured from the <c>Jwt</c> configuration section
    /// (<c>Authority</c>, <c>Audience</c>, <c>RequireHttpsMetadata</c>), plus authorization
    /// and the scoped tenant context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddEventPlatformAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var jwt = configuration.GetSection("Jwt");
        var devSigningKey = jwt["DevSigningKey"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwt.GetValue("RequireHttpsMetadata", defaultValue: true);

                if (string.IsNullOrWhiteSpace(devSigningKey))
                {
                    // Production: validate against the real OIDC identity provider.
                    options.Authority = jwt["Authority"];
                    options.Audience = jwt["Audience"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                    };
                }
                else
                {
                    // DEV ONLY — `Jwt:DevSigningKey` is set only in Development config, never in
                    // production/Key Vault. Validates tokens signed with a local symmetric key so
                    // the stack can be exercised (and load-tested) without a real identity provider.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt["Issuer"] ?? "eventplatform-dev",
                        ValidateAudience = true,
                        ValidAudience = jwt["Audience"] ?? "eventplatform",
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(devSigningKey)),
                        ClockSkew = TimeSpan.FromSeconds(30),
                    };
                }
            });

        // Named role policies (organizer/buyer). Deliberately no fallback policy yet: one would
        // deny every endpoint that carries no authorization metadata, which today is most of them,
        // and would take the Dapr pub/sub subscribers down with it. The fallback goes in once every
        // endpoint is explicitly annotated — see ADR-0035.
        services.AddEventPlatformAuthorization();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        return services;
    }
}
