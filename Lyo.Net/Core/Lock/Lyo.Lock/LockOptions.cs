namespace Lyo.Lock;

/// <summary>Settings for <see cref="LocalLockService" /> and shared defaults used by Redis-backed locks.</summary>
public class LockOptions
{
    /// <summary>Configuration section name used by <see cref="LockServiceExtensions" /> bind helpers.</summary>
    public const string SectionName = "LockOptions";

    /// <summary>How long to wait for a lock when the caller omits a timeout.</summary>
    public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>How long a distributed lock stays held (auto-release if the process dies).</summary>
    public TimeSpan DefaultLockDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Prefix prepended to distributed lock keys (for example <c>lyo:lock:</c>).</summary>
    public string KeyPrefix { get; set; } = "lyo:lock:";

    /// <summary>True to skip <c>ToLowerInvariant()</c> on keys. Use when keys are already normalized (for example a fixed set) to avoid allocation.</summary>
    public bool SkipKeyNormalization { get; set; } = false;

    /// <summary>True to emit metrics for lock acquire, release, and execute.</summary>
    public bool EnableMetrics { get; set; } = false;
}