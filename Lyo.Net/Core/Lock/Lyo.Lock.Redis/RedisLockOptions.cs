namespace Lyo.Lock.Redis;

/// <summary>Extends <see cref="LockOptions"/> with Redis acquire strategy (poll interval vs pub/sub wakeups).</summary>
public class RedisLockOptions : LockOptions
{
    /// <summary>Delay between acquisition attempts when <see cref="UsePubSubForAcquireWait"/> is <see langword="false"/>.</summary>
    public TimeSpan AcquirePollInterval { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// When <see langword="true"/>, competing acquirers subscribe to a per-key channel and retry immediately when the holder publishes on release.
    /// When <see langword="false"/>, acquirers only poll at <see cref="AcquirePollInterval"/>.
    /// </summary>
    public bool UsePubSubForAcquireWait { get; set; } = true;
}