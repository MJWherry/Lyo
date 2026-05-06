using System.Collections.Concurrent;
using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Lock;

/// <summary>Releases the per-key <see cref="SemaphoreSlim"/> for <see cref="LocalLockService"/> and removes the dictionary entry when the last reference ends.</summary>
internal sealed class LocalLockHandle(ConcurrentDictionary<string, SemaphoreEntry> locks, SemaphoreEntry entry, string key, ILogger logger, IMetrics metrics, string keyForMetrics)
    : ILockHandle
{
    private int _released;

    public ValueTask ReleaseAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return default;

        using (metrics.StartTimer(Constants.Metrics.ReleaseDuration, [(Constants.Metrics.Tags.Key, keyForMetrics)])) {
            try {
                entry.Semaphore.Release();
            }
            catch (SemaphoreFullException ex) {
                logger.LogWarning(ex, "Lock for key {LockKey} was already released", key);
            }
        }

        if (Interlocked.Decrement(ref entry.RefCount) == 0)
            TryRemoveSemaphoreEntry(locks, key, entry);
        return default;
    }

    public void Dispose() => ReleaseAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
    
    // Conditional remove: ICollection<KeyValuePair<>>.Remove is available on netstandard2.0; TryRemove(KeyValuePair<>) was added in netstandard2.1.
    private static bool TryRemoveSemaphoreEntry(ConcurrentDictionary<string, SemaphoreEntry> locks, string key, SemaphoreEntry entry) =>
        ((ICollection<KeyValuePair<string, SemaphoreEntry>>)locks).Remove(new(key, entry));
}