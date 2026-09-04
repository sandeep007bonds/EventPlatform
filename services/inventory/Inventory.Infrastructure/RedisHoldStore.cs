namespace Inventory.Infrastructure;

/// <summary>
/// Redis implementation of <see cref="IHoldStore"/>. The place/release operations run as Lua
/// scripts, which Redis executes atomically (single-threaded) — so the availability check and the
/// flip to held cannot interleave. This is the core of no-oversell on the fast path.
/// </summary>
/// <remarks>
/// Sparse model: a missing seat key (or <c>A</c>) means available; <c>H:{holdId}</c> means held;
/// <c>S</c> means sold; <c>B</c> means blocked by the organizer. Not seeding a key per seat keeps
/// large venues cheap.
/// </remarks>
/// <param name="redis">The Redis connection.</param>
internal sealed class RedisHoldStore(IConnectionMultiplexer redis) : IHoldStore
{
    // Presence of this key means the store has been rebuilt from Postgres since its last restart.
    // A flush wipes it, which is exactly how the reconciler detects that Redis lost its state.
    private const string ReconcilerSentinelKey = "inv:reconciler:initialized";

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

    // KEYS = capacity keys (one per allocation); ARGV[1]=holdId, ARGV[2]=ttl, ARGV[3]=holdKey
    // (shared TTL sentinel with the seat scripts), ARGV[4]=gaHoldItemsKey (a hash of
    // allocationId -> held quantity), ARGV[5..4+n]=quantities aligned to KEYS,
    // ARGV[5+n..4+2n]=allocationId strings aligned to KEYS. A missing capacity key (never
    // initialized, or lost to a flush) reads as zero remaining — fail-closed, unlike the sparse
    // seat model. Returns "OK" or "CONFLICT:{allocationId}".
    private const string TryHoldGeneralAdmissionScript = """
        local n = #KEYS
        for i = 1, n do
          local remaining = tonumber(redis.call('GET', KEYS[i]) or '0')
          local qty = tonumber(ARGV[4 + i])
          if remaining < qty then
            return 'CONFLICT:' .. ARGV[4 + n + i]
          end
        end
        for i = 1, n do
          local qty = tonumber(ARGV[4 + i])
          redis.call('DECRBY', KEYS[i], qty)
          redis.call('HSET', ARGV[4], ARGV[4 + n + i], qty)
        end
        redis.call('SET', ARGV[3], '1', 'EX', tonumber(ARGV[2]))
        redis.call('EXPIRE', ARGV[4], tonumber(ARGV[2]))
        return 'OK'
        """;

    // KEYS = capacity keys; ARGV[1]=gaHoldItemsKey, ARGV[2..]=quantities aligned to KEYS.
    private const string ReleaseGeneralAdmissionScript = """
        for i = 1, #KEYS do
          local qty = tonumber(ARGV[1 + i])
          redis.call('INCRBY', KEYS[i], qty)
        end
        redis.call('DEL', ARGV[1])
        return 'OK'
        """;

    // ARGV[1]=gaHoldItemsKey. Remaining capacity was already decremented at hold time and must not
    // be restored — only the hold's bookkeeping hash is cleared, mirroring how MarkSoldScript
    // deletes holdKey/holdSeatsKey without touching sold seats' permanent state.
    private const string MarkGeneralAdmissionSoldScript = """
        redis.call('DEL', ARGV[1])
        return 'OK'
        """;

    // KEYS = seat keys. Unlike ReleaseScript, there's no holdId to match against — a sold marker
    // ('S') carries none — so this clears the key outright for any seat currently marked sold.
    private const string ReleaseSoldScript = """
        for i = 1, #KEYS do
          if redis.call('GET', KEYS[i]) == 'S' then
            redis.call('DEL', KEYS[i])
          end
        end
        return 'OK'
        """;

    // KEYS = capacity keys (one per allocation); ARGV[1..]=quantities aligned to KEYS. Mirrors
    // ReleaseGeneralAdmissionScript's INCRBY loop, minus the hold-bookkeeping-hash delete —
    // MarkGeneralAdmissionSoldScript already deleted that hash, so there's nothing left to clear.
    private const string ReleaseSoldGeneralAdmissionScript = """
        for i = 1, #KEYS do
          local qty = tonumber(ARGV[i])
          redis.call('INCRBY', KEYS[i], qty)
        end
        return 'OK'
        """;

    /// <inheritdoc />
    public async Task<HoldStoreResult> TryHoldAsync(
        Guid eventSessionId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = HoldKeys(eventSessionId, seatIds);

        var values = new List<RedisValue>
        {
            holdId.ToString("N"),
            (int)ttl.TotalSeconds,
            HoldKey(eventSessionId, holdId),
            HoldSeatsKey(eventSessionId, holdId),
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
        Guid eventSessionId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, HoldKeys(eventSessionId, seatIds), HoldArgs(eventSessionId, holdId));
    }

    /// <inheritdoc />
    public async Task ExtendAsync(Guid eventSessionId, Guid holdId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Only the bookkeeping/TTL-bearing keys need extending — the per-seat/per-allocation markers
        // themselves carry no TTL (cleared explicitly by the release/convert scripts instead).
        // KeyExpireAsync on a key that doesn't exist for this hold (e.g. an all-GA hold has no
        // HoldSeatsKey) is a safe no-op, so no need to branch on which kinds of items the hold has.
        var db = redis.GetDatabase();
        await db.KeyExpireAsync(HoldKey(eventSessionId, holdId), ttl);
        await db.KeyExpireAsync(HoldSeatsKey(eventSessionId, holdId), ttl);
        await db.KeyExpireAsync(GaHoldItemsKey(eventSessionId, holdId), ttl);
    }

    /// <inheritdoc />
    public async Task MarkSoldAsync(
        Guid eventSessionId,
        Guid holdId,
        IReadOnlyList<Guid> seatIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await redis.GetDatabase().ScriptEvaluateAsync(MarkSoldScript, HoldKeys(eventSessionId, seatIds), HoldArgs(eventSessionId, holdId));
    }

    /// <inheritdoc />
    public async Task ReleaseSoldAsync(Guid eventSessionId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await redis.GetDatabase().ScriptEvaluateAsync(ReleaseSoldScript, HoldKeys(eventSessionId, seatIds), Array.Empty<RedisValue>());
    }

    /// <inheritdoc />
    public async Task BlockAsync(Guid eventSessionId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        foreach (var seatId in seatIds)
        {
            await db.StringSetAsync(SeatKey(eventSessionId, seatId), "B");
        }
    }

    /// <inheritdoc />
    public async Task UnblockAsync(Guid eventSessionId, IReadOnlyList<Guid> seatIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        foreach (var seatId in seatIds)
        {
            await db.KeyDeleteAsync(SeatKey(eventSessionId, seatId));
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsInitializedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await redis.GetDatabase().KeyExistsAsync(ReconcilerSentinelKey);
    }

    /// <inheritdoc />
    public async Task RestoreAsync(
        IReadOnlyList<SeatReconciliationState> states,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(states);
        cancellationToken.ThrowIfCancellationRequested();

        var db = redis.GetDatabase();
        foreach (var state in states)
        {
            var seatKey = SeatKey(state.EventSessionId, state.SeatId);
            if (state.Condition == SeatCondition.Sold)
            {
                await db.StringSetAsync(seatKey, "S");
                continue;
            }

            if (state.Condition == SeatCondition.Blocked)
            {
                await db.StringSetAsync(seatKey, "B");
                continue;
            }

            // Held: restore only while the hold still has time left; an already-expired one is left
            // for the reaper to release, so we never resurrect a stale hold.
            var ttl = (state.HoldExpiresAt ?? now) - now;
            if (state.HoldId is not { } holdId || ttl <= TimeSpan.Zero)
            {
                continue;
            }

            var seatValue = state.SeatId.ToString("N");
            await db.StringSetAsync(seatKey, "H:" + holdId.ToString("N"));
            await db.StringSetAsync(HoldKey(state.EventSessionId, holdId), "1", ttl);
            await db.SetAddAsync(HoldSeatsKey(state.EventSessionId, holdId), seatValue);
            await db.KeyExpireAsync(HoldSeatsKey(state.EventSessionId, holdId), ttl);
        }
    }

    /// <inheritdoc />
    public async Task MarkInitializedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().StringSetAsync(ReconcilerSentinelKey, "1");
    }

    /// <inheritdoc />
    public async Task InitializeGeneralAdmissionCapacityAsync(
        Guid eventSessionId,
        Guid allocationId,
        int totalCapacity,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().StringSetAsync(CapacityKey(eventSessionId, allocationId), totalCapacity);
    }

    /// <inheritdoc />
    public async Task<GeneralAdmissionHoldStoreResult> TryHoldGeneralAdmissionAsync(
        Guid eventSessionId,
        Guid holdId,
        IReadOnlyList<(Guid AllocationId, int Quantity)> selections,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = selections
            .Select(selection => (RedisKey)CapacityKey(eventSessionId, selection.AllocationId))
            .ToArray();

        var values = new List<RedisValue>
        {
            holdId.ToString("N"),
            (int)ttl.TotalSeconds,
            HoldKey(eventSessionId, holdId),
            GaHoldItemsKey(eventSessionId, holdId),
        };
        values.AddRange(selections.Select(selection => (RedisValue)selection.Quantity));
        values.AddRange(selections.Select(selection => (RedisValue)selection.AllocationId.ToString("N")));

        var raw = (string?)await redis.GetDatabase()
            .ScriptEvaluateAsync(TryHoldGeneralAdmissionScript, keys, values.ToArray());
        if (raw == "OK")
        {
            return new GeneralAdmissionHoldStoreResult(true, null);
        }

        var conflictAllocationId = raw?.Split(':', 2) is [_, var id] && Guid.TryParse(id, out var allocation)
            ? allocation
            : (Guid?)null;

        return new GeneralAdmissionHoldStoreResult(false, conflictAllocationId);
    }

    /// <inheritdoc />
    public async Task ReleaseGeneralAdmissionAsync(
        Guid eventSessionId,
        Guid holdId,
        IReadOnlyList<(Guid AllocationId, int Quantity)> selections,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = selections
            .Select(selection => (RedisKey)CapacityKey(eventSessionId, selection.AllocationId))
            .ToArray();

        var values = new List<RedisValue> { GaHoldItemsKey(eventSessionId, holdId) };
        values.AddRange(selections.Select(selection => (RedisValue)selection.Quantity));

        await redis.GetDatabase().ScriptEvaluateAsync(ReleaseGeneralAdmissionScript, keys, values.ToArray());
    }

    /// <inheritdoc />
    public async Task MarkGeneralAdmissionSoldAsync(Guid eventSessionId, Guid holdId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var values = new RedisValue[] { GaHoldItemsKey(eventSessionId, holdId) };
        await redis.GetDatabase().ScriptEvaluateAsync(MarkGeneralAdmissionSoldScript, Array.Empty<RedisKey>(), values);
    }

    /// <inheritdoc />
    public async Task ReleaseSoldGeneralAdmissionAsync(
        Guid eventSessionId,
        IReadOnlyList<(Guid AllocationId, int Quantity)> selections,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = selections
            .Select(selection => (RedisKey)CapacityKey(eventSessionId, selection.AllocationId))
            .ToArray();
        var values = selections.Select(selection => (RedisValue)selection.Quantity).ToArray();

        await redis.GetDatabase().ScriptEvaluateAsync(ReleaseSoldGeneralAdmissionScript, keys, values);
    }

    private static RedisKey[] HoldKeys(Guid eventSessionId, IReadOnlyList<Guid> seatIds) =>
        seatIds.Select(seatId => (RedisKey)SeatKey(eventSessionId, seatId)).ToArray();

    private static RedisValue[] HoldArgs(Guid eventSessionId, Guid holdId) =>
        new RedisValue[]
        {
            holdId.ToString("N"),
            HoldKey(eventSessionId, holdId),
            HoldSeatsKey(eventSessionId, holdId),
        };

    private static string SeatKey(Guid eventSessionId, Guid seatId) => $"inv:{eventSessionId:N}:seat:{seatId:N}";

    private static string HoldKey(Guid eventSessionId, Guid holdId) => $"inv:{eventSessionId:N}:hold:{holdId:N}";

    private static string HoldSeatsKey(Guid eventSessionId, Guid holdId) => $"inv:{eventSessionId:N}:hold:{holdId:N}:seats";

    private static string CapacityKey(Guid eventSessionId, Guid allocationId) =>
        $"inv:{eventSessionId:N}:ga:{allocationId:N}:remaining";

    private static string GaHoldItemsKey(Guid eventSessionId, Guid holdId) => $"inv:{eventSessionId:N}:hold:{holdId:N}:ga";
}
