using System.Collections.Concurrent;
using Lyo.Exceptions;
using Lyo.Lock.Abstractions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Lock;

/// <summary>
/// In-memory exclusive lock: one holder per key within the current process. Uses a <see cref="SemaphoreSlim"/> per normalized key.
/// </summary>
/// <remarks>
/// Not suitable for coordination across machines or processes; use a distributed <see cref="ILockService"/> for that.
/// <see cref="LockOptions.DefaultLockDuration"/> is ignored (no TTL).
/// </remarks>
public sealed class LocalLockService : ILockService
{
    private readonly ConcurrentDictionary<string, SemaphoreEntry> _locks = new();
    private readonly ILogger<LocalLockService> _logger;
    private readonly IMetrics _metrics;
    private readonly LockOptions _options;

    /// <param name="logger">Optional logger for acquire failures and release anomalies.</param>
    /// <param name="options">Timeouts, key normalization, metrics toggles.</param>
    /// <param name="metrics">When <see cref="LockOptions.EnableMetrics"/> is true and this is non-null, timings and counters are emitted.</param>
    public LocalLockService(ILogger<LocalLockService>? logger = null, LockOptions? options = null, IMetrics? metrics = null)
    {
        _logger = logger ?? NullLogger<LocalLockService>.Instance;
        _options = options ?? new LockOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    /// <inheritdoc />
    public async ValueTask<ILockHandle?> AcquireAsync(string key, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var normalizedKey = _options.SkipKeyNormalization ? key : key.ToLowerInvariant();
        var entry = _locks.GetOrAdd(normalizedKey, _ => new());
        var effectiveTimeout = timeout ?? _options.DefaultAcquireTimeout;
        var tags = new[] { (Constants.Metrics.Tags.Key, key) };
        using (_metrics.StartTimer(Constants.Metrics.AcquireDuration, tags)) {
            var acquired = await entry.Semaphore.WaitAsync(effectiveTimeout, ct).ConfigureAwait(false);
            if (acquired) {
                Interlocked.Increment(ref entry.RefCount);
                _metrics.IncrementCounter(Constants.Metrics.AcquireSuccess, 1, tags);
                return new LocalLockHandle(_locks, entry, normalizedKey, _logger, _metrics, key);
            }
        }

        _metrics.IncrementCounter(Constants.Metrics.AcquireFailure, 1, tags);
        _logger.LogDebug("Failed to acquire lock for key {LockKey} within {Timeout}", key, effectiveTimeout);
        return null;
    }

    /// <inheritdoc />
    public async Task ExecuteWithLockAsync(
        string key,
        Func<CancellationToken, Task> action,
        TimeSpan? timeout = null,
        TimeSpan? lockDuration = null,
        CancellationToken ct = default)
    {
        using (_metrics.StartTimer(Constants.Metrics.ExecuteDuration, [(Constants.Metrics.Tags.Key, key)])) {
            var handle = await AcquireAsync(key, timeout, lockDuration, ct).ConfigureAwait(false);
            if (handle == null)
                throw new TimeoutException($"Could not acquire lock for key '{key}' within {timeout ?? _options.DefaultAcquireTimeout}.");

            try {
                await action(ct).ConfigureAwait(false);
            }
            finally {
                await handle.ReleaseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteWithLockAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        TimeSpan? timeout = null,
        TimeSpan? lockDuration = null,
        CancellationToken ct = default)
    {
        using (_metrics.StartTimer(Constants.Metrics.ExecuteDuration, [(Constants.Metrics.Tags.Key, key)])) {
            var handle = await AcquireAsync(key, timeout, lockDuration, ct).ConfigureAwait(false);
            if (handle == null)
                throw new TimeoutException($"Could not acquire lock for key '{key}' within {timeout ?? _options.DefaultAcquireTimeout}.");

            try {
                return await action(ct).ConfigureAwait(false);
            }
            finally {
                await handle.ReleaseAsync().ConfigureAwait(false);
            }
        }
    }
}