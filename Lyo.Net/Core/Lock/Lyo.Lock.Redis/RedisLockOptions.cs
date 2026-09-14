using Lyo.Exceptions;
using Lyo.Lock;

namespace Lyo.Lock.Redis;

/// <summary>Adds a Redis acquire strategy to <see cref="LockOptions" /> (poll interval versus pub/sub wakeups).</summary>
public class RedisLockOptions : LockOptions
{
    /// <summary>Pause between acquire attempts when <see cref="UsePubSubForAcquireWait" /> is <see langword="false" />.</summary>
    public TimeSpan AcquirePollInterval { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// When <see langword="true" />, competing acquirers subscribe to a per-key channel and retry as soon as the holder publishes on release. When <see langword="false" />,
    /// acquirers only poll at <see cref="AcquirePollInterval" />.
    /// </summary>
    public bool UsePubSubForAcquireWait { get; set; } = true;

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIf(AcquirePollInterval <= TimeSpan.Zero, "AcquirePollInterval must be greater than zero.");
    }
}