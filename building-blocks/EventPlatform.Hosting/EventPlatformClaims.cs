namespace EventPlatform.Hosting;

/// <summary>
/// The claim types and role values EventPlatform tokens carry, in one place so a service never
/// spells one of them by hand.
/// </summary>
/// <remarks>
/// These are the claims minted by Identity's <c>JwtTokenIssuer</c>: every token carries
/// <see cref="Subject"/> and <see cref="Role"/>; only an organizer's carries <see cref="TenantId"/>,
/// because a buyer belongs to no tenant (ADR-0022).
/// </remarks>
public static class EventPlatformClaims
{
    /// <summary>The user id claim (<c>sub</c>).</summary>
    public const string Subject = "sub";

    /// <summary>The role claim (<c>role</c>) — see <see cref="BuyerRole"/> and <see cref="OrganizerRole"/>.</summary>
    public const string Role = "role";

    /// <summary>The tenant id claim (<c>tenant_id</c>). Present on organizer tokens only.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>Role value for a ticket buyer.</summary>
    public const string BuyerRole = "buyer";

    /// <summary>Role value for an event organizer.</summary>
    public const string OrganizerRole = "organizer";
}
