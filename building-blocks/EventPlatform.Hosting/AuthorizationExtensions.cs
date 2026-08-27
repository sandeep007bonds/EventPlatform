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
/// Public endpoints must still be marked <c>AllowAnonymous</c> explicitly. The fallback policy
/// registered by <see cref="AddEventPlatformAuthorization"/> means an endpoint carrying no
/// authorization metadata is now denied rather than open, so the failure mode of forgetting an
/// annotation is a 401 in testing instead of a silent hole in production.
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
    /// Registers the EventPlatform authorization policies and the deny-by-default fallback policy.
    /// </summary>
    /// <remarks>
    /// The fallback policy applies only where an endpoint supplies no authorization metadata of its
    /// own; <c>AllowAnonymous</c> suppresses it, which is why every public endpoint — including the
    /// health probes, the OpenAPI document and the Dapr subscribe manifest — says so explicitly.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    internal static IServiceCollection AddEventPlatformAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
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
