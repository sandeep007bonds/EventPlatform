namespace EventPlatform.Persistence;

/// <summary>Wires <see cref="AuditFieldsInterceptor"/> into a context's options.</summary>
public static class AuditFieldsDbContextOptionsExtensions
{
    /// <summary>
    /// Attaches the audit-field interceptor, resolved from the scope the context belongs to.
    /// </summary>
    /// <remarks>
    /// Call from the <c>AddDbContext</c> overload that supplies an <see cref="IServiceProvider"/>:
    /// the interceptor depends on the scoped <see cref="IAuditContext"/>, so it cannot come from
    /// the root provider without attributing every write to whatever scope happened to build the
    /// options first.
    /// <code>
    /// services.AddDbContext&lt;CatalogDbContext&gt;((sp, options) => options
    ///     .UseNpgsql(connectionString)
    ///     .UseAuditFields(sp));
    /// </code>
    /// </remarks>
    /// <param name="options">The options builder.</param>
    /// <param name="serviceProvider">The scoped provider handed to the <c>AddDbContext</c> callback.</param>
    /// <returns>The same <paramref name="options"/> for chaining.</returns>
    public static DbContextOptionsBuilder UseAuditFields(
        this DbContextOptionsBuilder options,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        // Constructed here rather than resolved, so there is no second registration a service can
        // forget: calling UseAuditFields is the whole opt-in. The instance follows the options,
        // which the (sp, options) overload builds per scope — so it captures that scope's actor.
        return options.AddInterceptors(
            new AuditFieldsInterceptor(serviceProvider.GetRequiredService<IAuditContext>()));
    }
}
