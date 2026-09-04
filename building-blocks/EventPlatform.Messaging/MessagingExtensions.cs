namespace EventPlatform.Messaging;

/// <summary>DI registration for the transactional outbox.</summary>
public static class MessagingExtensions
{
    /// <summary>
    /// Registers the transactional outbox: the event publisher (write path, bound to the given
    /// DbContext) plus the background relay that publishes queued messages to Dapr pub/sub.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext that owns the outbox table.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional relay configuration (pub/sub name, poll interval, batch size).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOutbox<TDbContext>(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null)
        where TDbContext : DbContext, IOutboxDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new OutboxOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddScoped<IOutboxDbContext>(sp => sp.GetRequiredService<TDbContext>());

        // The publisher stamps every event with the current chain of work, so the context has to
        // exist wherever the outbox does. TryAdd because AddServiceDefaults registers the same
        // pair — order of the two calls must not matter.
        services.TryAddScoped<CorrelationContext>();
        services.TryAddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        services.AddSingleton(_ => new DaprClientBuilder().Build());
        services.AddHostedService<OutboxRelay>();

        return services;
    }
}
