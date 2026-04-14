using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Lock;

/// <summary>In-memory keyed semaphore implementation using SemaphoreSlim per key. Suitable for single-process scenarios.</summary>
public sealed class LocalKeyedSemaphoreService : IKeyedSemaphoreService
{
    private readonly Dictionary<string, SemaphoreEntry> _entries = [];
    private readonly object _entriesGate = new();
    private readonly ILogger<LocalKeyedSemaphoreService> _logger;
    private readonly IMetrics _metrics;
    private readonly KeyedSemaphoreOptions _options;

    public LocalKeyedSemaphoreService(ILogger<LocalKeyedSemaphoreService>? logger = null, KeyedSemaphoreOptions? options = null, IMetrics? metrics = null)
    {
        _logger = logger ?? NullLogger<LocalKeyedSemaphoreService>.Instance;
        _options = options ?? new KeyedSemaphoreOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    public async ValueTask<IPermitHandle?> AcquireAsync(string key, int maxConcurrency, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), maxConcurrency, "Max concurrency must be greater than zero.");

        var normalizedKey = _options.SkipKeyNormalization ? key : key.ToLowerInvariant();
        var effectiveTimeout = timeout ?? _options.DefaultAcquireTimeout;
        var entry = GetOrCreateEntry(normalizedKey, maxConcurrency);
        var tags = new[] { (Constants.SemaphoreMetrics.Tags.Key, key) };
        using (_metrics.StartTimer(Constants.SemaphoreMetrics.AcquireDuration, tags)) {
            try {
                var acquired = await entry.Semaphore.WaitAsync(effectiveTimeout, ct).ConfigureAwait(false);
                if (acquired) {
                    _metrics.IncrementCounter(Constants.SemaphoreMetrics.AcquireSuccess, 1, tags);
                    return new LocalPermitHandle(this, entry, normalizedKey, _logger, _metrics, key);
                }

                ReleaseEntryReference(normalizedKey, entry);
                _metrics.IncrementCounter(Constants.SemaphoreMetrics.AcquireFailure, 1, tags);
                _logger.LogDebug(
                    "Failed to acquire semaphore permit for key {SemaphoreKey} within {Timeout} (max concurrency {MaxConcurrency})", key, effectiveTimeout, maxConcurrency);

                return null;
            }
            catch {
                ReleaseEntryReference(normalizedKey, entry);
                throw;
            }
        }
    }

    public async Task ExecuteAsync(string key, int maxConcurrency, Func<CancellationToken, Task> action, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(action, nameof(action));
        using (_metrics.StartTimer(Constants.SemaphoreMetrics.ExecuteDuration, [(Constants.SemaphoreMetrics.Tags.Key, key)])) {
            var handle = await AcquireAsync(key, maxConcurrency, timeout, ct).ConfigureAwait(false);
            if (handle == null)
                throw new TimeoutException($"Could not acquire semaphore permit for key '{key}' within {timeout ?? _options.DefaultAcquireTimeout}.");

            try {
                await action(ct).ConfigureAwait(false);
            }
            finally {
                await handle.ReleaseAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task<T> ExecuteAsync<T>(string key, int maxConcurrency, Func<CancellationToken, Task<T>> action, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(action, nameof(action));
        using (_metrics.StartTimer(Constants.SemaphoreMetrics.ExecuteDuration, [(Constants.SemaphoreMetrics.Tags.Key, key)])) {
            var handle = await AcquireAsync(key, maxConcurrency, timeout, ct).ConfigureAwait(false);
            if (handle == null)
                throw new TimeoutException($"Could not acquire semaphore permit for key '{key}' within {timeout ?? _options.DefaultAcquireTimeout}.");

            try {
                return await action(ct).ConfigureAwait(false);
            }
            finally {
                await handle.ReleaseAsync().ConfigureAwait(false);
            }
        }
    }

    private SemaphoreEntry GetOrCreateEntry(string normalizedKey, int maxConcurrency)
    {
        lock (_entriesGate) {
            if (!_entries.TryGetValue(normalizedKey, out var entry)) {
                entry = new(maxConcurrency);
                _entries[normalizedKey] = entry;
            }
            else if (entry.MaxConcurrency != maxConcurrency)
                throw new InvalidOperationException($"Semaphore key '{normalizedKey}' is already active with max concurrency {entry.MaxConcurrency}, not {maxConcurrency}.");

            entry.RefCount++;
            return entry;
        }
    }

    private void ReleaseEntryReference(string normalizedKey, SemaphoreEntry entry)
    {
        lock (_entriesGate) {
            entry.RefCount--;
            if (entry.RefCount == 0 && _entries.TryGetValue(normalizedKey, out var current) && ReferenceEquals(current, entry))
                _entries.Remove(normalizedKey);
        }
    }

    private sealed class SemaphoreEntry
    {
        public int MaxConcurrency { get; }

        public int RefCount { get; set; }

        public SemaphoreSlim Semaphore { get; }

        public SemaphoreEntry(int maxConcurrency)
        {
            MaxConcurrency = maxConcurrency;
            Semaphore = new(maxConcurrency, maxConcurrency);
        }
    }

    private sealed class LocalPermitHandle : IPermitHandle
    {
        private readonly SemaphoreEntry _entry;
        private readonly string _key;
        private readonly string _keyForMetrics;
        private readonly ILogger _logger;
        private readonly IMetrics _metrics;
        private readonly LocalKeyedSemaphoreService _owner;
        private int _released;

        public LocalPermitHandle(LocalKeyedSemaphoreService owner, SemaphoreEntry entry, string key, ILogger logger, IMetrics metrics, string keyForMetrics)
        {
            _owner = owner;
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

            using (_metrics.StartTimer(Constants.SemaphoreMetrics.ReleaseDuration, [(Constants.SemaphoreMetrics.Tags.Key, _keyForMetrics)])) {
                try {
                    _entry.Semaphore.Release();
                }
                catch (SemaphoreFullException ex) {
                    _logger.LogWarning(ex, "Semaphore permit for key {SemaphoreKey} was already released", _key);
                }
            }

            _owner.ReleaseEntryReference(_key, _entry);
            return default;
        }

        public void Dispose() => ReleaseAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync() => await ReleaseAsync().ConfigureAwait(false);
    }
}