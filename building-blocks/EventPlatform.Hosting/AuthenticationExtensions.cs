using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = jwt["Authority"];
                options.Audience = jwt["Audience"];
                options.RequireHttpsMetadata = jwt.GetValue("RequireHttpsMetadata", defaultValue: true);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        return services;
    }
}
