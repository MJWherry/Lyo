namespace Lyo.Lock;

/// <summary>Per-key semaphore state: counting semaphore with <see cref="MaxConcurrency"/> initial permits (and matching ceiling).</summary>
internal sealed class SemaphoreEntry(int maxConcurrency = 1)
{
    public int MaxConcurrency { get; } = maxConcurrency;

    /// <summary>
    /// Allows up to <see cref="MaxConcurrency"/> concurrent waiters without blocking.
    /// For exclusive locks, <see cref="MaxConcurrency"/> is 1.
    /// </summary>
    public readonly SemaphoreSlim Semaphore = new(maxConcurrency, maxConcurrency);

    public int RefCount;
}