namespace Identity.Infrastructure;

/// <summary>
/// Registers the Identity infrastructure layer: persistence, OTP hashing, the Dapr call to
/// Communication, and RSA signing-key management. Like Communication, this does NOT call
/// <c>AddOutbox</c> — Identity never publishes an integration event.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds the Identity infrastructure services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("identity");
        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IOrganizerRepository, OrganizerRepository>();
        services.AddScoped<ISigningKeyRepository, SigningKeyRepository>();

        AddOtpHasher(services, configuration);
        AddSigningKeyProvider(services, configuration);
        AddTokenIssuer(services, configuration);
        services.AddSingleton<IOtpSender, DaprOtpSender>();

        services.AddSingleton<PasswordHasher<OrganizerAccount>>();
        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();

        return services;
    }

    private static void AddOtpHasher(IServiceCollection services, IConfiguration configuration)
    {
        var hmacKey = configuration["Identity:Otp:HmacKey"]
            ?? "eventplatform-dev-otp-hmac-key-not-a-secret"; // dev-only fallback, mirrors Jwt:DevSigningKey's committed-plaintext posture
        services.AddSingleton<IOtpHasher>(new HmacOtpHasher(Encoding.UTF8.GetBytes(hmacKey)));
    }

    private static void AddSigningKeyProvider(IServiceCollection services, IConfiguration configuration)
    {
        var signingKeySource = configuration["Identity:Jwt:SigningKeySource"];
        if (string.Equals(signingKeySource, "KeyVault", StringComparison.OrdinalIgnoreCase))
        {
            // Deliberately not implemented this pass (see the ADR-0016 extension). Fails fast
            // rather than silently signing with a different key-custody model than requested.
            throw new NotSupportedException(
                "Identity:Jwt:SigningKeySource=KeyVault is not implemented yet. " +
                "Unset it to use the dev-persisted RSA key path.");
        }

        services.AddSingleton<ISigningKeyProvider, PersistedRsaSigningKeyProvider>();
    }

    private static void AddTokenIssuer(IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Identity:Jwt:Issuer"]!;
        var audience = configuration["Identity:Jwt:Audience"] ?? "eventplatform";
        var lifetimeDays = int.TryParse(configuration["Identity:Jwt:AccessTokenLifetimeDays"], out var parsedLifetimeDays)
            ? parsedLifetimeDays
            : 7;

        services.AddSingleton<ITokenIssuer>(sp => new JwtTokenIssuer(
            issuer,
            audience,
            TimeSpan.FromDays(lifetimeDays),
            sp.GetRequiredService<ISigningKeyProvider>()));
    }
}
