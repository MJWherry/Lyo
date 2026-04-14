namespace Lyo.Lock.Redis;

/// <summary>Options for RedisLockService. Extends base LockOptions with Redis-specific settings.</summary>
public class RedisLockOptions : LockOptions
{
    /// <summary>Interval between retries when waiting to acquire a distributed lock. Only used when UsePubSubForAcquireWait is false.</summary>
    public TimeSpan AcquirePollInterval { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>When true, uses Redis pub/sub to wake waiters immediately when a lock is released instead of polling. Reduces latency under contention.</summary>
    public bool UsePubSubForAcquireWait { get; set; } = true;
}