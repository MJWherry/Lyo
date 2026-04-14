namespace Lyo.Lock;

/// <summary>Options for keyed semaphore behavior.</summary>
public class KeyedSemaphoreOptions
{
    public const string SectionName = "KeyedSemaphoreOptions";

    /// <summary>Default timeout when waiting to acquire a permit.</summary>
    public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>When true, skips ToLowerInvariant() on keys. Use when keys are already normalized to avoid allocation.</summary>
    public bool SkipKeyNormalization { get; set; }

    /// <summary>Enables metrics collection for semaphore operations (acquire, release, execute).</summary>
    public bool EnableMetrics { get; set; }
}