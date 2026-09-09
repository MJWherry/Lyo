namespace Lyo.Lock;

/// <summary>Per-key semaphore state: a counting semaphore with <see cref="MaxConcurrency" /> initial permits and the same ceiling.</summary>
internal sealed class SemaphoreEntry(int maxConcurrency = 1)
{
    /// <summary>Lets up to <see cref="MaxConcurrency" /> holders proceed without blocking. Exclusive locks set <see cref="MaxConcurrency" /> to 1.</summary>
    public readonly SemaphoreSlim Semaphore = new(maxConcurrency, maxConcurrency);

    public int RefCount;

    public int MaxConcurrency { get; } = maxConcurrency;
}