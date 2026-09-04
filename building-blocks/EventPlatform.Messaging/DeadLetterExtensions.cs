namespace EventPlatform.Messaging;

/// <summary>Registers the dead-letter drain for a service that subscribes to anything.</summary>
public static class DeadLetterExtensions
{
    /// <summary>Wires <see cref="DeadLetterDrain"/> over the given context.</summary>
    /// <typeparam name="TDbContext">The service's dead-letter-carrying context.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddDeadLetters<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IDeadLetterDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IDeadLetterDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.AddScoped<DeadLetterDrain>();

        return services;
    }
}
