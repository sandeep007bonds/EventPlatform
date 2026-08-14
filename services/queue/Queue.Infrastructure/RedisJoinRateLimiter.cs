namespace Queue.Infrastructure;

/// <summary>
/// Redis fixed-window implementation of <see cref="IJoinRateLimiter"/>.
/// <para>
/// The counter lives in Redis rather than in process memory so the budget is shared across every
/// Queue replica — an in-memory limiter would multiply the real allowance by the replica count, and
/// a client would be free to keep retrying until it landed on a pod that had not seen it yet.
/// </para>
/// <para>
/// Both operations fail <em>open</em>. A limiter that cannot reach Redis must not close the waiting
/// room: this is abuse mitigation, and denying every genuine buyer during a Redis blip would cause
/// far more damage than the abuse it is guarding against. (A real join fails anyway if Redis is
/// down, since the waiting set lives there too — so failing open here costs nothing extra.)
/// </para>
/// </summary>
/// <param name="redis">The Redis connection.</param>
/// <param name="options">The window and allowance.</param>
/// <param name="logger">The logger.</param>
internal sealed class RedisJoinRateLimiter(
    IConnectionMultiplexer redis,
    QueueRateLimitOptions options,
    ILogger<RedisJoinRateLimiter> logger)
    : IJoinRateLimiter
{
    // KEYS[1] = counter key, ARGV[1] = window seconds.
    private const string IncrementScript = """
        local used = redis.call('INCR', KEYS[1])
        if used == 1 then
          redis.call('EXPIRE', KEYS[1], tonumber(ARGV[1]))
        end
        return used
        """;

    /// <inheritdoc />
    public async Task<JoinRateLimitDecision> CheckAsync(
        Guid eventId,
        string clientKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var database = redis.GetDatabase();
            var key = RateKey(eventId, clientKey);

            var used = (int?)await database.StringGetAsync(key) ?? 0;
            if (used < options.MaxNewSessionsPerWindow)
            {
                return JoinRateLimitDecision.Allow;
            }

            // The key's remaining TTL is exactly when this client's window resets.
            var ttl = await database.KeyTimeToLiveAsync(key);
            var retryAfter = (int)Math.Ceiling((ttl ?? options.Window).TotalSeconds);

            return JoinRateLimitDecision.Deny(Math.Max(retryAfter, 1));
        }
        catch (RedisException exception)
        {
            logger.LogWarning(
                exception,
                "Join rate-limit check failed for event {EventId}; allowing the join rather than " +
                "closing the waiting room.",
                eventId);

            return JoinRateLimitDecision.Allow;
        }
    }

    /// <inheritdoc />
    public async Task RecordCreatedSessionAsync(Guid eventId, string clientKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // INCR then EXPIRE-if-first, in one script: doing it in two round trips can leave the
            // counter with no TTL at all if the process dies between them, permanently locking that
            // client out of the event.
            await redis.GetDatabase().ScriptEvaluateAsync(
                IncrementScript,
                [RateKey(eventId, clientKey)],
                [(int)options.Window.TotalSeconds]);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(
                exception,
                "Could not record a join against the rate limit for event {EventId}.",
                eventId);
        }
    }

    private static string RateKey(Guid eventId, string clientKey) =>
        $"queue:{eventId:N}:joinrate:{clientKey}";
}
