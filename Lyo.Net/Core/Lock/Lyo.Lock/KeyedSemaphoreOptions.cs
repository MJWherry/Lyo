using Lyo.Exceptions;

namespace Lyo.Lock;

/// <summary>Settings for <see cref="LocalKeyedSemaphoreService" />.</summary>
public class KeyedSemaphoreOptions
{
    /// <summary>Configuration section name used by <see cref="LockServiceExtensions" /> bind helpers.</summary>
    public const string SectionName = "KeyedSemaphoreOptions";

    /// <summary>How long to wait for a permit when the caller omits a timeout.</summary>
    public TimeSpan DefaultAcquireTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>True to skip <c>ToLowerInvariant()</c> on keys. Use when keys are already normalized to avoid allocation.</summary>
    public bool SkipKeyNormalization { get; set; }

    /// <summary>True to emit metrics for semaphore acquire, release, and execute.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>Throws when <see cref="DefaultAcquireTimeout" /> is not positive.</summary>
    public void Validate() => ArgumentHelpers.ThrowIf(DefaultAcquireTimeout <= TimeSpan.Zero, "DefaultAcquireTimeout must be greater than zero.");
}