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

                // Off, deliberately. Left at its default of true, the handler rewrites inbound
                // claims through the legacy WS-Federation map before any policy sees them: `role`
                // becomes http://schemas.microsoft.com/ws/2008/06/identity/claims/role and `sub`
                // becomes ...claims/nameidentifier. Both token issuers mint the short names
                // (ADR-0022), so RequireClaim(EventPlatformClaims.Role, ...) then cannot match a
                // token this platform issued, and every organizer endpoint answers 403 to a
                // perfectly good token. `tenant_id` is absent from that map, which is why tenant
                // scoping kept working and hid the problem for as long as it did.
                options.MapInboundClaims = false;

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
                        NameClaimType = EventPlatformClaims.Subject,
                        RoleClaimType = EventPlatformClaims.Role,
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
                        NameClaimType = EventPlatformClaims.Subject,
                        RoleClaimType = EventPlatformClaims.Role,
                    };
                }
            });

        // Named role policies (organizer/buyer) plus the deny-by-default fallback: an endpoint that
        // carries no authorization metadata is denied rather than open. Every public endpoint says
        // AllowAnonymous explicitly, which suppresses the fallback for it — see ADR-0035.
        services.AddEventPlatformAuthorization();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        return services;
    }
}
