namespace EventPlatform.Hosting;

/// <summary>
/// Role requirements for endpoints, expressed at the route rather than re-checked by hand inside
/// every handler.
/// </summary>
/// <remarks>
/// <para>
/// These say <b>who</b> may call an endpoint, never <b>which records</b> they may reach. A handler
/// still has to check that the caller owns (or their tenant owns) the specific resource — an
/// organizer being an organizer says nothing about whether a given event is theirs. The two are
/// separate concerns and both are required.
/// </para>
/// <para>
/// Nothing here is a substitute for marking public endpoints <c>AllowAnonymous</c> explicitly.
/// Until a fallback policy is in place, an endpoint carrying no authorization metadata at all is
/// reachable by anyone — silence is not a deny.
/// </para>
/// </remarks>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Restricts the endpoint to callers holding the organizer role.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or route group to restrict.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder RequireOrganizer<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RequireAuthorization(AuthorizationPolicies.Organizer);
    }

    /// <summary>
    /// Restricts the endpoint to callers holding the buyer role.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or route group to restrict.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder RequireBuyer<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RequireAuthorization(AuthorizationPolicies.Buyer);
    }

    /// <summary>
    /// Requires any authenticated caller, whatever their role. For endpoints a buyer and an
    /// organizer both legitimately reach — reading an order, for instance, which its buyer and the
    /// selling tenant can each see.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint or route group to restrict.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder RequireAuthenticatedCaller<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RequireAuthorization();
    }

    /// <summary>
    /// Registers the EventPlatform authorization policies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    internal static IServiceCollection AddEventPlatformAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(
                AuthorizationPolicies.Organizer,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(EventPlatformClaims.Role, EventPlatformClaims.OrganizerRole))
            .AddPolicy(
                AuthorizationPolicies.Buyer,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(EventPlatformClaims.Role, EventPlatformClaims.BuyerRole));

        return services;
    }
}
