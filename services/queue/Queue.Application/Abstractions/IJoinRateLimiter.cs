namespace Queue.Application.Abstractions;

/// <summary>
/// Caps how many *new* waiting-room sessions one client may create for an event in a window.
/// <para>
/// The waiting room paces access; on its own it does nothing about who is asking. A script minting
/// fresh session ids in a loop takes as many places in line as it likes, which is precisely the
/// scenario the queue exists to defend against. Budget is spent per created session rather than per
/// request, so a buyer refreshing the page resumes the same session and costs nothing while the
/// script pays for every id it invents.
/// </para>
/// <para>
/// This is one layer, not a solution: a client with many source addresses is unaffected. A proof of
/// work or a CAPTCHA is what actually raises the cost per identity, and neither is built (ADR-0026).
/// </para>
/// </summary>
public interface IJoinRateLimiter
{
    /// <summary>Whether <paramref name="clientKey"/> has budget left to create a session.</summary>
    /// <param name="eventId">The event being joined — budget is per event, not global.</param>
    /// <param name="clientKey">Opaque per-client key (today, the caller's address).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The decision, carrying a retry hint when the client is over budget.</returns>
    Task<JoinRateLimitDecision> CheckAsync(Guid eventId, string clientKey, CancellationToken cancellationToken);

    /// <summary>Charges one created session against <paramref name="clientKey"/>'s budget.</summary>
    /// <param name="eventId">The event that was joined.</param>
    /// <param name="clientKey">Opaque per-client key.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes once the charge is recorded.</returns>
    Task RecordCreatedSessionAsync(Guid eventId, string clientKey, CancellationToken cancellationToken);
}
