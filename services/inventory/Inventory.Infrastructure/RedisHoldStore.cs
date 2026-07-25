namespace Inventory.Infrastructure;

/// <summary>
/// Redis implementation of <see cref="IHoldStore"/>. The place/release operations run as Lua
/// scripts, which Redis executes atomically (single-threaded) — so the availability check and the
/// flip to held cannot interleave. This is the core of no-oversell on the fast path.
/// </summary>
/// <remarks>
/// Sparse model: a missing seat key (or <c>A</c>) means available; <c>H:{holdId}</c> means held;
/// <c>S</c> means sold. Not seeding a key per seat keeps large venues cheap.
/// </remarks>
/// <param name="redis">The Redis connection.</param>
internal sealed class RedisHoldStore(IConnectionMultiplexer redis) : IHoldStore
{
    // KEYS = seat keys; ARGV[1]=holdId, ARGV[2]=ttl, ARGV[3]=holdKey, ARGV[4]=holdSeatsKey,
    // ARGV[5..]=seatIds (aligned to KEYS). Returns "OK" or "CONFLICT:{seatId}".
    private const string PlaceScript = """
        local n = #KEYS
        for i = 1, n do
          local v = redis.call('GET', KEYS[i])
          if v and v ~= 'A' then
            return 'CONFLICT:' .. ARGV[4 + i]
          end
        end
        local marker = 'H:' .. ARGV[1]
        for i = 1, n do
          redis.call('SET', KEYS[i], marker)
        end
        redis.call('SET', ARGV[3], '1', 'EX', tonumber(ARGV[2]))
        for i = 1, n do
          redis.call('SADD', ARGV[4], ARGV[4 + i])
        end
        redis.call('EXPIRE', ARGV[4], tonumber(ARGV[2]))
        return 'OK'
        """;

    // KEYS = seat keys; ARGV[1]=holdId, ARGV[2]=holdKey, ARGV[3]=holdSeatsKey.
    private const string ReleaseScript = """
        local marker = 'H:' .. ARGV[1]
        for i = 1, #KEYS do
          if redis.call('GET', KEYS[i]) == marker then
            redis.call('DEL', KEYS[i])
          end
        end
        redis.call('DEL', ARGV[2])
        redis.call('DEL', ARGV[3])
        return 'OK'
        """;

    // KEYS = seat keys; ARGV[1]=holdId, ARGV[2]=holdKey, ARGV[3]=holdSeatsKey.
    private const string MarkSoldScript = """
        local marker = 'H:' .. ARGV[1]
        for i = 1, #KEYS do
          if redis.call('GET', KEYS[i]) == marker then
            redis.call('SET', KEYS[i], 'S')
          end
        end
        redis.call('DEL', ARGV[2])
        redis.call('DEL', ARGV[3])
        return 'OK'
        """;

    /// <inheritdoc />
    public async Task<HoldStoreResult> TryHoldAsync(
        Guid eventId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = HoldKeys(eventId, seatIds);

        var values = new List<RedisValue>
        {
            holdId.ToString("N"),
            (int)ttl.TotalSeconds,
            HoldKey(eventId, holdId),
            HoldSeatsKey(eventId, holdId),
        };
        values.AddRange(seatIds.Select(seatId => (RedisValue)seatId.ToString("N")));

        var raw = (string?)await redis.GetDatabase().ScriptEvaluateAsync(PlaceScript, keys, values.ToArray());
        if (raw == "OK")
        {
            return new HoldStoreResult(true, null);
        }

        var conflictSeatId = raw?.Split(':', 2) is [_, var id] && Guid.TryParse(id, out var seat)
            ? seat
            : (Guid?)null;

        return new HoldStoreResult(false, conflictSeatId);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(
        Guid eventId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, HoldKeys(eventId, seatIds), HoldArgs(eventId, holdId));
    }

    /// <inheritdoc />
    public async Task MarkSoldAsync(
        Guid eventId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await redis.GetDatabase().ScriptEvaluateAsync(MarkSoldScript, HoldKeys(eventId, seatIds), HoldArgs(eventId, holdId));
    }

    private static RedisKey[] HoldKeys(Guid eventId, IReadOnlyList<Guid> seatIds) =>
        seatIds.Select(seatId => (RedisKey)SeatKey(eventId, seatId)).ToArray();

    private static RedisValue[] HoldArgs(Guid eventId, Guid holdId) =>
        new RedisValue[]
        {
            holdId.ToString("N"),
            HoldKey(eventId, holdId),
            HoldSeatsKey(eventId, holdId),
        };

    private static string SeatKey(Guid eventId, Guid seatId) => $"inv:{eventId:N}:seat:{seatId:N}";

    private static string HoldKey(Guid eventId, Guid holdId) => $"inv:{eventId:N}:hold:{holdId:N}";

    private static string HoldSeatsKey(Guid eventId, Guid holdId) => $"inv:{eventId:N}:hold:{holdId:N}:seats";
}
