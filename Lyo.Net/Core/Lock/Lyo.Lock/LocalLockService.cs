using System.Collections.Concurrent;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Lock;

/// <summary>In-memory lock implementation using SemaphoreSlim per key. Suitable for single-process scenarios.</summary>
public sealed class LocalLockService : ILockService
{
    private readonly ConcurrentDictionary<string, SemaphoreEntry> _locks = new();
    private readonly ILogger<LocalLockService> _logger;
    private readonly IMetrics _metrics;
    private readonly LockOptions _options;

    public LocalLockService(ILogger<LocalLockService>? logger = null, LockOptions? options = null, IMetrics? metrics = null)
    {
        _logger = logger ?? NullLogger<LocalLockService>.Instance;
        _options = options ?? new LockOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    /// <inheritdoc />
    public async ValueTask<ILockHandle?> AcquireAsync(string key, TimeSpan? timeout = null, TimeSpan? lockDuration = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var normalizedKey = _options.SkipKeyNormalization ? key : key.ToLowerInvariant();
        var entry = _locks.GetOrAdd(normalizedKey, _ => new());
        var effectiveTimeout = timeout ?? _options.DefaultAcquireTimeout;
        var tags = new[] { (Constants.Metrics.Tags.Key, key) };
        using (_metrics.StartTimer(Constants.Metrics.AcquireDuration, tags)) {
            var acquired = await entry.Semaphore.WaitAsync(effectiveTimeout, ct).ConfigureAwait(false);
            if (acquired) {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
                Interlocked.Increment(ref entry.RefCount);
#endif
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

    private sealed class SemaphoreEntry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public int RefCount;
#endif
    }

    private sealed class LocalLockHandle : ILockHandle
    {
        private readonly SemaphoreEntry _entry;
        private readonly string _key;
        private readonly string _keyForMetrics;
        private readonly ConcurrentDictionary<string, SemaphoreEntry> _locks;
        private readonly ILogger _logger;
        private readonly IMetrics _metrics;
        private int _released;

        public LocalLockHandle(ConcurrentDictionary<string, SemaphoreEntry> locks, SemaphoreEntry entry, string key, ILogger logger, IMetrics metrics, string keyForMetrics)
        {
            _locks = locks;
            _entry = entry;
            _key = key;
            _logger = logger;
            _metrics = metrics;
            _keyForMetrics = keyForMetrics;
        }

        public ValueTask ReleaseAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
                return default;

            using (_metrics.StartTimer(Constants.Metrics.ReleaseDuration, [(Constants.Metrics.Tags.Key, _keyForMetrics)])) {
                try {
                    _entry.Semaphore.Release();
                }
                catch (SemaphoreFullException ex) {
                    _logger.LogWarning(ex, "Lock for key {LockKey} was already released", _key);
                }
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (Interlocked.Decrement(ref _entry.RefCount) == 0)
                _locks.TryRemove(new(_key, _entry));
#endif
            return default;
        }

        public void Dispose() => ReleaseAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
    }
}