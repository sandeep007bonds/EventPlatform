namespace Identity.Application;

/// <summary>Registers the Identity application layer's handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the OTP request/verify handlers.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<RequestOtpHandler>();
        services.AddScoped<VerifyOtpHandler>();

        return services;
    }
}
