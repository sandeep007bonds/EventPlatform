namespace EventPlatform.Hosting;

/// <summary>
/// Names of the authorization policies every EventPlatform service registers.
/// </summary>
/// <remarks>
/// Prefer the route-builder extensions in <see cref="AuthorizationExtensions"/> over naming a
/// policy directly — they read better at the call site and keep the string in one place.
/// </remarks>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Requires an authenticated caller carrying the organizer role. Organizer endpoints
    /// additionally derive the tenant from <see cref="ITenantContext"/>; this policy establishes
    /// only that the caller is an organizer at all.
    /// </summary>
    public const string Organizer = "eventplatform:organizer";

    /// <summary>Requires an authenticated caller carrying the buyer role.</summary>
    public const string Buyer = "eventplatform:buyer";
}
