namespace Lyo.Lock;

/// <summary>Options for lock service behavior.</summary>
public class LockOptions
{
    public const string SectionName = "LockOptions";

    /// <summary>Default timeout when waiting to acquire a lock.</summary>
    public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Default lock duration for distributed locks (auto-release if process crashes).</summary>
    public TimeSpan DefaultLockDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Key prefix for distributed locks (e.g. "lyo:lock:").</summary>
    public string KeyPrefix { get; set; } = "lyo:lock:";

    /// <summary>When true, skips ToLowerInvariant() on keys. Use when keys are already normalized (e.g. from a fixed set) to avoid allocation.</summary>
    public bool SkipKeyNormalization { get; set; } = false;

    /// <summary>Enables metrics collection for lock operations (acquire, release, execute).</summary>
    public bool EnableMetrics { get; set; } = false;
}