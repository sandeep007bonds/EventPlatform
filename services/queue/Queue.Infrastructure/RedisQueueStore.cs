namespace Queue.Infrastructure;

/// <summary>
/// Redis implementation of <see cref="IQueueStore"/>. Every operation runs as a Lua script, which
/// Redis executes atomically (single-threaded) — the same "Lua = the fast, safe gate" pattern
/// <c>RedisHoldStore</c> already establishes for Inventory's holds.
/// </summary>
/// <remarks>
/// Key scheme: <c>queue:{eventId:N}:waiting</c> is a sorted set (score = enqueue time, via Redis's
/// own <c>TIME</c> command for FIFO ordering with no separate sequence key);
/// <c>queue:{eventId:N}:admitted:{sessionId:N}</c> is a plain string whose presence alone means
/// admitted — its TTL is the admission's expiry, mirroring the sparse-seat-model convention of
/// "presence/absence carries the state" already used by <c>RedisHoldStore</c>.
/// </remarks>
/// <param name="redis">The Redis connection.</param>
internal sealed class RedisQueueStore(IConnectionMultiplexer redis) : IQueueStore
{
    // KEYS[1] = waiting set, KEYS[2] = admitted key. ARGV[1] = sessionId.
    // Returns "ADMITTED", "WAITING:{rank}", never "NOTFOUND" — joining always resolves to one of
    // the two, resuming an existing waiting session at its original (not reset) position.
    private const string EnqueueOrResumeScript = """
        if redis.call('EXISTS', KEYS[2]) == 1 then
          return 'ADMITTED'
        end
        local rank = redis.call('ZRANK', KEYS[1], ARGV[1])
        if rank then
          return 'WAITING:' .. rank .. ':RESUMED'
        end
        local time = redis.call('TIME')
        local score = tonumber(time[1]) + (tonumber(time[2]) / 1000000)
        redis.call('ZADD', KEYS[1], score, ARGV[1])
        return 'WAITING:' .. redis.call('ZRANK', KEYS[1], ARGV[1]) .. ':CREATED'
        """;

    // KEYS[1] = waiting set, KEYS[2] = admitted key. ARGV[1] = sessionId.
    // Returns "ADMITTED", "WAITING:{rank}", or "NOTFOUND" (never joined, or admission expired).
    private const string GetStatusScript = """
        if redis.call('EXISTS', KEYS[2]) == 1 then
          return 'ADMITTED'
        end
        local rank = redis.call('ZRANK', KEYS[1], ARGV[1])
        if rank then
          return 'WAITING:' .. rank
        end
        return 'NOTFOUND'
        """;

    // KEYS[1] = waiting set. ARGV[1] = count, ARGV[2] = ttlSeconds, ARGV[3] = admitted-key prefix.
    // ZPOPMIN is itself atomic, so concurrent callers (e.g. multiple Queue.Api replicas ticking at
    // once) can never pop the same session twice — each gets a disjoint slice of the front of the
    // line. The admitted-key names are only known once the pop resolves, so they're built inline
    // from a precomputed prefix rather than passed as KEYS — safe on the single Redis instance this
    // repo runs (no cluster-mode key-slot routing to satisfy), same simplification already
    // accepted elsewhere in this dev-scale deployment.
    private const string PromoteBatchScript = """
        local popped = redis.call('ZPOPMIN', KEYS[1], tonumber(ARGV[1]))
        local ids = {}
        for i = 1, #popped, 2 do
          local sessionId = popped[i]
          redis.call('SET', ARGV[3] .. sessionId, '1', 'EX', tonumber(ARGV[2]))
          table.insert(ids, sessionId)
        end
        return ids
        """;

    /// <inheritdoc />
    public async Task<QueueStoreResult> EnqueueOrResumeAsync(Guid eventId, Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = new RedisKey[] { WaitingKey(eventId), AdmittedKey(eventId, sessionId) };
        var values = new RedisValue[] { sessionId.ToString("N") };

        var raw = (string?)await redis.GetDatabase().ScriptEvaluateAsync(EnqueueOrResumeScript, keys, values);
        return Parse(raw);
    }

    /// <inheritdoc />
    public async Task<QueueStoreResult> GetStatusAsync(Guid eventId, Guid sessionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = new RedisKey[] { WaitingKey(eventId), AdmittedKey(eventId, sessionId) };
        var values = new RedisValue[] { sessionId.ToString("N") };

        var raw = (string?)await redis.GetDatabase().ScriptEvaluateAsync(GetStatusScript, keys, values);
        return Parse(raw);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> PromoteBatchAsync(
        Guid eventId,
        int count,
        TimeSpan sessionTtl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = new RedisKey[] { WaitingKey(eventId) };
        var values = new RedisValue[] { count, (int)sessionTtl.TotalSeconds, AdmittedKeyPrefix(eventId) };

        var raw = (RedisValue[]?)await redis.GetDatabase().ScriptEvaluateAsync(PromoteBatchScript, keys, values);
        if (raw is null || raw.Length == 0)
        {
            return Array.Empty<Guid>();
        }

        return raw
            .Select(value => Guid.TryParse((string?)value, out var sessionId) ? sessionId : (Guid?)null)
            .Where(sessionId => sessionId is not null)
            .Select(sessionId => sessionId!.Value)
            .ToList();
    }

    private static QueueStoreResult Parse(string? raw)
    {
        if (raw == "ADMITTED")
        {
            return new QueueStoreResult(QueueSessionStatus.Admitted, null);
        }

        // "WAITING:{rank}" from the status script, "WAITING:{rank}:{CREATED|RESUMED}" from the
        // enqueue script — the suffix is what tells a rate limiter whether a new session was minted.
        if (raw?.StartsWith("WAITING:", StringComparison.Ordinal) == true)
        {
            var parts = raw.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[1], out var position))
            {
                var wasCreated = parts.Length > 2 && parts[2] == "CREATED";
                return new QueueStoreResult(QueueSessionStatus.Waiting, position, wasCreated);
            }
        }

        return new QueueStoreResult(QueueSessionStatus.NotFound, null);
    }

    private static string WaitingKey(Guid eventId) => $"queue:{eventId:N}:waiting";

    private static string AdmittedKeyPrefix(Guid eventId) => $"queue:{eventId:N}:admitted:";

    private static string AdmittedKey(Guid eventId, Guid sessionId) => $"{AdmittedKeyPrefix(eventId)}{sessionId:N}";
}
