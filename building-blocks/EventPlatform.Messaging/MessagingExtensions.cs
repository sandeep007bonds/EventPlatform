namespace EventPlatform.Messaging;

/// <summary>DI registration for the transactional outbox.</summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Registers the outbox event publisher, bound to the given DbContext as the outbox store.
    /// The Dapr relay that publishes queued messages is added separately.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext that owns the outbox table.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IOutboxDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        return services;
    }
}
