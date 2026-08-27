namespace EventPlatform.Auditing;

/// <summary>
/// What kind of actor performed a write.
/// </summary>
/// <remarks>
/// This distinction is load-bearing rather than decorative: much of what this platform does is done
/// by the checkout saga, the expired-hold reaper or the queue admission controller, none of which
/// has a <c>ClaimsPrincipal</c>. Recording those as a null user would make the audit trail lie by
/// omission — "nobody did this" is a different claim from "a service did this" (ADR-0036).
/// </remarks>
public enum ActorType
{
    /// <summary>A person, identified by the subject of their access token.</summary>
    User,

    /// <summary>A service acting on its own behalf — a saga step, a background reaper, a subscriber.</summary>
    Service,

    /// <summary>The platform itself, for writes belonging to no service in particular (migrations, seeding).</summary>
    System,
}
