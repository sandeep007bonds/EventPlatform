namespace Queue.Application.Abstractions;

/// <summary>
/// Mints the short-lived, HMAC-signed capability token an admitted session presents to Inventory
/// at hold-placement time. Verified locally by Inventory against a shared secret — no cross-service
/// call at hold time, the same "propagate once, verify locally" philosophy ADR-0025 established.
/// </summary>
public interface IAdmissionTokenIssuer
{
    /// <summary>Issues an admission token for a session.</summary>
    /// <param name="eventId">The event the session was admitted for.</param>
    /// <param name="sessionId">The admitted session id.</param>
    /// <param name="validFor">How long the token remains valid.</param>
    /// <returns>The opaque, signed token string.</returns>
    string Issue(Guid eventId, Guid sessionId, TimeSpan validFor);
}
