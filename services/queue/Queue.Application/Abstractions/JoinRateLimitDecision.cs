namespace Queue.Application.Abstractions;

/// <summary>The outcome of a <see cref="IJoinRateLimiter"/> check.</summary>
/// <param name="Allowed">Whether the client may create a new session.</param>
/// <param name="RetryAfterSeconds">
/// How long until the client's window resets, when <paramref name="Allowed"/> is
/// <see langword="false"/>; otherwise <see langword="null"/>.
/// </param>
public sealed record JoinRateLimitDecision(bool Allowed, int? RetryAfterSeconds)
{
    /// <summary>The client has budget left.</summary>
    public static readonly JoinRateLimitDecision Allow = new(true, null);

    /// <summary>The client is over budget for this window.</summary>
    /// <param name="retryAfterSeconds">Seconds until the window resets.</param>
    /// <returns>A denying decision.</returns>
    public static JoinRateLimitDecision Deny(int retryAfterSeconds) => new(false, retryAfterSeconds);
}
