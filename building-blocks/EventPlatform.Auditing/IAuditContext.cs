namespace EventPlatform.Auditing;

/// <summary>
/// The actor responsible for the writes happening in the current scope.
/// </summary>
/// <remarks>
/// Resolved per scope, so a request-scoped write attributes to the caller and a background write
/// attributes to the service. Implementations must always return an actor — never null, never
/// empty. An unattributable write is the failure this exists to prevent, so it is recorded as the
/// service that made it rather than left blank.
/// </remarks>
public interface IAuditContext
{
    /// <summary>
    /// Who to record. A user's token subject where there is one, otherwise a service identity of
    /// the form <c>service:ordering</c>.
    /// </summary>
    string Actor { get; }

    /// <summary>What kind of actor <see cref="Actor"/> names.</summary>
    ActorType ActorType { get; }
}
