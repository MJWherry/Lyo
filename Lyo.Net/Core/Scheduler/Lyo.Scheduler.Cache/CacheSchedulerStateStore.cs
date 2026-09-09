using Lyo.Cache;
using Lyo.Exceptions;

namespace Lyo.Scheduler.Cache;

/// <summary>An <see cref="ISchedulerStateStore" /> backed by <see cref="ICacheService" />, so state survives restarts and can be shared when the cache is distributed.</summary>
public sealed class CacheSchedulerStateStore : ISchedulerStateStore
{
    private const string KeyPrefix = "lyo:scheduler:";
    private const string LastRunSuffix = ":lastrun";
    private const string LastSlotSuffix = ":lastslot";
    private readonly ICacheService _cache;

    /// <summary>Builds a state store that writes through <see cref="ICacheService" />.</summary>
    public CacheSchedulerStateStore(ICacheService cache) => _cache = ArgumentHelpers.ThrowIfNullReturn(cache);

    /// <inheritdoc />
    public async ValueTask<DateTime?> GetLastRunAsync(string scheduleId, CancellationToken ct = default)
    {
        var key = KeyPrefix + scheduleId + LastRunSuffix;
        var value = await _cache.GetOrSetAsync(key, (DateTime?)null, token: ct).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc />
    public ValueTask SetLastRunAsync(string scheduleId, DateTime timestamp, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = KeyPrefix + scheduleId + LastRunSuffix;
        _cache.Set(key, timestamp);
        return default;
    }

    /// <inheritdoc />
    public async ValueTask<DateTime?> GetLastExecutedSlotAsync(string scheduleId, CancellationToken ct = default)
    {
        var key = KeyPrefix + scheduleId + LastSlotSuffix;
        var value = await _cache.GetOrSetAsync(key, (DateTime?)null, token: ct).ConfigureAwait(false);
        return value;
    }

    /// <inheritdoc />
    public ValueTask SetLastExecutedSlotAsync(string scheduleId, DateTime slotTimestamp, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = KeyPrefix + scheduleId + LastSlotSuffix;
        _cache.Set(key, slotTimestamp);
        return default;
    }
}