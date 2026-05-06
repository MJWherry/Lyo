using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;

namespace Lyo.Lock;

/// <summary>Releases one permit on <see cref="SemaphoreEntry.Semaphore"/> and drops the keyed entry when ref-count reaches zero.</summary>
internal sealed class LocalPermitHandle(LocalKeyedSemaphoreService owner, SemaphoreEntry entry, string key, ILogger logger, IMetrics metrics, string keyForMetrics) : IPermitHandle
{
    private int _released;

    public ValueTask ReleaseAsync()
    {
        if (Interlocked.Exchange(ref _released, 1) != 0)
            return default;

        using (metrics.StartTimer(Constants.SemaphoreMetrics.ReleaseDuration, [(Constants.SemaphoreMetrics.Tags.Key, keyForMetrics)])) {
            try {
                entry.Semaphore.Release();
            }
            catch (SemaphoreFullException ex) {
                logger.LogWarning(ex, "Semaphore permit for key {SemaphoreKey} was already released", key);
            }
        }

        owner.ReleaseEntryReference(key, entry);
        return default;
    }

    public void Dispose() => ReleaseAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
}