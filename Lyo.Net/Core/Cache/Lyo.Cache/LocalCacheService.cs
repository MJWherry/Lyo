using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Cache;

/// <summary>Local-only cache implementation using IMemoryCache. No distributed cache or FusionCache.</summary>
public sealed class LocalCacheService : ICacheService
{
    private const string TagPrefix = "__tag:";

    private readonly bool _enabled;
    private readonly ConcurrentDictionary<CacheItem, byte> _items = new();
    private readonly ConcurrentDictionary<string, string[]> _keyToTags = new();
    private readonly ILogger<LocalCacheService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IMetrics _metrics;
    private readonly CacheOptions _options;
    private readonly ICachePayloadCodec? _payloadCodec;
    private readonly ICachePayloadSerializer? _payloadSerializer;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagToKeys = new();

    public LocalCacheService(
        IMemoryCache memoryCache,
        ILogger<LocalCacheService>? logger = null,
        CacheOptions? options = null,
        IMetrics? metrics = null,
        ICachePayloadCodec? payloadCodec = null,
        ICachePayloadSerializer? payloadSerializer = null)
    {
        ArgumentHelpers.ThrowIfNull(memoryCache, nameof(memoryCache));
        _memoryCache = memoryCache;
        _logger = logger ?? NullLogger<LocalCacheService>.Instance;
        _options = options ?? new CacheOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _enabled = _options.Enabled;
        _payloadCodec = payloadCodec;
        _payloadSerializer = payloadSerializer;
    }

    public IReadOnlyCollection<CacheItem> Items => _items.Keys.ToList().AsReadOnly();

    /// <inheritdoc />
    public string HealthCheckName => "cache";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            var testKey = $"lyo-health-{Guid.NewGuid():N}";
            var testValue = "ok";
            Set(testKey, testValue, ["lyo-health-check"]);
            var fromCache = await GetOrSetAsync(testKey, _ => Task.FromResult<string?>(testValue), TimeSpan.FromSeconds(5), ["lyo-health-check"], ct).ConfigureAwait(false);
            await InvalidateCacheItem(testKey).ConfigureAwait(false);
            sw.Stop();
            var ok = fromCache == testValue;
            return ok
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["key"] = testKey })
                : HealthResult.Unhealthy(sw.Elapsed, "Cache read/write mismatch");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    public async Task InvalidateCacheItem(string key)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            RemoveKeyFromTagMappings(normalizedKey);
            _memoryCache.Remove(normalizedKey);
            _items.TryRemove(CacheItem.Key(normalizedKey), out var _);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.RemoveDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.RemoveSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating cache item with key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.RemoveDuration, ex, [(Constants.Metrics.Tags.Operation, "InvalidateCacheItem"), (Constants.Metrics.Tags.Key, key)]);
            throw;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task InvalidateCacheItemByTag(string tag)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag, nameof(tag));
        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        var normalizedTag = tag.ToLowerInvariant();
        try {
            var beforeCount = _items.Count;
            if (_tagToKeys.TryRemove(normalizedTag, out var keys)) {
                foreach (var kvp in keys) {
                    var key = kvp.Key;
                    RemoveKeyFromTagMappings(key);
                    _memoryCache.Remove(key);
                    _items.TryRemove(CacheItem.Key(key), out var _);
                }
            }

            _items.TryRemove(CacheItem.Tag(TagPrefix + normalizedTag), out var _);
            stopwatch.Stop();
            var itemsRemoved = Math.Max(0, beforeCount - _items.Count);
            var tags = new[] { (Constants.Metrics.Tags.Tag, tag) };
            _metrics.RecordTiming(Constants.Metrics.RemoveByTagDuration, stopwatch.Elapsed, tags);
            _metrics.IncrementCounter(Constants.Metrics.RemoveByTagSuccess, 1, tags);
            _metrics.RecordGauge(Constants.Metrics.RemoveByTagItemsRemoved, itemsRemoved, tags);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating cache items by tag {CacheTag}", tag);
            _metrics.RecordError(Constants.Metrics.RemoveByTagDuration, ex, [(Constants.Metrics.Tags.Operation, "InvalidateCacheItemByTag"), (Constants.Metrics.Tags.Tag, tag)]);
            throw;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public Task InvalidateQueryCacheAsync<TDb>()
        where TDb : class
    {
        var entityTag = $"entity:{typeof(TDb).Name.ToLowerInvariant()}";
        return InvalidateCacheItemByTag(entityTag);
    }

    public Task InvalidateCacheByTypeAsync(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName)) {
            _logger.LogWarning("Attempted to invalidate cache for null or empty type name");
            return Task.CompletedTask;
        }

        var tag = $"type:{fullTypeName.ToLowerInvariant()}";
        return InvalidateCacheItemByTag(tag);
    }

    public Task InvalidateCacheByTypeAsync(Type type) => InvalidateCacheByTypeAsync(type.FullName ?? type.Name);

    public Task InvalidateCacheByTypeAsync<T>() => InvalidateCacheByTypeAsync(typeof(T));

    public Task InvalidateAllCachedQueriesAsync() => InvalidateCacheItemByTag("queries");

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var result = await factory(token).ConfigureAwait(false);
            var opts = new CacheEntryOptions { Duration = _options.DefaultExpiration };
            SetInternal(normalizedKey, result!, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetAsync"), (Constants.Metrics.Tags.Key, key)]);
            return await factory(token).ConfigureAwait(false);
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var result = await factory(token).ConfigureAwait(false);
            var opts = new CacheEntryOptions { Duration = effectiveDuration };
            SetInternal(normalizedKey, result!, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetAsync"), (Constants.Metrics.Tags.Key, key)]);
            return await factory(token).ConfigureAwait(false);
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled) {
            var (value, _) = await factory(token).ConfigureAwait(false);
            return value;
        }

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var (result, factoryTags) = await factory(token).ConfigureAwait(false);
            var effectiveTags = MergeTags(factoryTags, extraTags);
            var opts = new CacheEntryOptions { Duration = _options.DefaultExpiration };
            SetInternal(normalizedKey, result!, opts.Duration, effectiveTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetAsync"), (Constants.Metrics.Tags.Key, key)]);
            var (v, _) = await factory(token).ConfigureAwait(false);
            return v;
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        Type type,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var typeExpiration = _options.GetExpirationForType(type);
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var result = await factory(token).ConfigureAwait(false);
            var opts = new CacheEntryOptions { Duration = typeExpiration };
            SetInternal(normalizedKey, result!, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetAsync"), (Constants.Metrics.Tags.Key, key)]);
            return await factory(token).ConfigureAwait(false);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled)
            return factory(CancellationToken.None);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var result = factory(CancellationToken.None);
            var opts = new CacheEntryOptions { Duration = _options.DefaultExpiration };
            SetInternal(normalizedKey, result!, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSet"), (Constants.Metrics.Tags.Key, key)]);
            return factory(CancellationToken.None);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return factory(CancellationToken.None);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var result = factory(CancellationToken.None);
            var opts = new CacheEntryOptions { Duration = effectiveDuration };
            SetInternal(normalizedKey, result!, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSet"), (Constants.Metrics.Tags.Key, key)]);
            return factory(CancellationToken.None);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, (TValue? value, string[]? tags)> factory, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled) {
            var (value, _) = factory(CancellationToken.None);
            return value;
        }

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var (result, factoryTags) = factory(CancellationToken.None);
            var effectiveTags = MergeTags(factoryTags, extraTags);
            var opts = new CacheEntryOptions { Duration = _options.DefaultExpiration };
            SetInternal(normalizedKey, result!, opts.Duration, effectiveTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSet"), (Constants.Metrics.Tags.Key, key)]);
            var (v, _) = factory(CancellationToken.None);
            return v;
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, Type type, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        var typeExpiration = _options.GetExpirationForType(type);
        if (!_enabled)
            return factory(CancellationToken.None);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var result = factory(CancellationToken.None);
            var opts = new CacheEntryOptions { Duration = typeExpiration };
            SetInternal(normalizedKey, result!, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return result;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSet"), (Constants.Metrics.Tags.Key, key)]);
            return factory(CancellationToken.None);
        }
    }

    public TValue GetOrSet<TValue>(string key, TValue value, Action<ICacheEntryOptions>? setupAction = null, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled)
            return value;

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var opts = new CacheEntryOptions { Duration = _options.DefaultExpiration };
            setupAction?.Invoke(opts);
            SetInternal(normalizedKey, value!, opts.Duration, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return value;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSet"), (Constants.Metrics.Tags.Key, key)]);
            return value;
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        TValue value,
        Action<ICacheEntryOptions>? setupAction = null,
        IEnumerable<string>? tags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled)
            return value;

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out TValue? cached) && cached != null) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cached;
            }

            var opts = new CacheEntryOptions { Duration = _options.DefaultExpiration };
            setupAction?.Invoke(opts);
            SetInternal(normalizedKey, value!, opts.Duration, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return value;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetAsync"), (Constants.Metrics.Tags.Key, key)]);
            return value;
        }
    }

    public void Set<T>(string key, T obj, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            SetInternal(normalizedKey, obj!, _options.DefaultExpiration, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.SetDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.SetSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting cache value for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.SetDuration, ex, [(Constants.Metrics.Tags.Operation, "Set"), (Constants.Metrics.Tags.Key, key)]);
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue<T>(string key, out T? value)
    {
        value = default;
        if (!_enabled || string.IsNullOrWhiteSpace(key))
            return false;

        try {
            var normalizedKey = key.ToLowerInvariant();
            return _memoryCache.TryGetValue(normalizedKey, out value);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error reading cache value for key {CacheKey}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => await GetOrSetPayloadAsync(key, factory, null, extraTags, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddLocalCache which registers it).");

        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out byte[]? cached) && cached != null) {
                try {
                    var decoded = _payloadCodec.Decode(cached);
                    stopwatch.Stop();
                    _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                    _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                    return decoded;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to decode payload cache for key {CacheKey}; removing entry", key);
                    await InvalidateCacheItem(key).ConfigureAwait(false);
                }
            }

            var plain = await factory(token).ConfigureAwait(false);
            if (plain == null)
                return null;

            var (framed, envelope) = _payloadCodec.EncodeReturningEnvelope(plain);
            var opts = new CacheEntryOptions { Duration = effectiveDuration };
            SetInternal(normalizedKey, framed, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in GetOrSetPayloadAsync for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetPayloadAsync"), (Constants.Metrics.Tags.Key, key)]);
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => await GetOrSetPayloadAsync(key, factory, null, extraTags, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddLocalCache which registers it).");

        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return await PayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out byte[]? cached) && cached != null) {
                try {
                    var decoded = _payloadCodec.Decode(cached);
                    stopwatch.Stop();
                    _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                    _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                    return decoded;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to decode payload cache for key {CacheKey}; removing entry", key);
                    await InvalidateCacheItem(key).ConfigureAwait(false);
                }
            }

            var (plain, factoryTags) = await factory(token).ConfigureAwait(false);
            if (plain == null)
                return null;

            var (framed, envelope) = _payloadCodec.EncodeReturningEnvelope(plain);
            var opts = new CacheEntryOptions { Duration = effectiveDuration };
            var mergedTags = MergeTags(factoryTags, extraTags);
            SetInternal(normalizedKey, framed, opts.Duration, mergedTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in GetOrSetPayloadAsync for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetPayloadAsync"), (Constants.Metrics.Tags.Key, key)]);
            return await PayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => GetOrSetPayloadAsync(key, factory, null, extraTags, token);

    /// <inheritdoc />
    public ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        async Task<(TValue? value, string[]? tags)> AsTuple(CancellationToken ct)
            => (await factory(ct).ConfigureAwait(false), null);

        return GetOrSetPayloadAsync(key, AsTuple, duration, extraTags, token);
    }

    /// <inheritdoc />
    public ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => GetOrSetPayloadAsync(key, factory, null, extraTags, token);

    /// <inheritdoc />
    public async ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (_payloadCodec == null || _payloadSerializer == null)
            throw new InvalidOperationException("Typed payload cache requires ICachePayloadCodec and ICachePayloadSerializer (use AddLocalCache which registers both).");

        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return await SerializedPayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out byte[]? cached) && cached != null) {
                CacheEntryEnvelope? decoded = null;
                try {
                    decoded = _payloadCodec.Decode(cached);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to decode payload cache for key {CacheKey}; removing entry", key);
                    await InvalidateCacheItem(key).ConfigureAwait(false);
                }

                if (decoded != null) {
                    try {
                        var deserialized = _payloadSerializer.Deserialize<TValue>(decoded.Payload);
                        stopwatch.Stop();
                        _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                        _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                        return deserialized;
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Failed to deserialize typed payload for key {CacheKey}; removing entry", key);
                        await InvalidateCacheItem(key).ConfigureAwait(false);
                    }
                }
            }

            var (value, factoryTags) = await factory(token).ConfigureAwait(false);
            if (value is null)
                return default;

            var plain = _payloadSerializer.Serialize(value);
            if (plain == null)
                return default;

            var framed = _payloadCodec.Encode(plain);
            var opts = new CacheEntryOptions { Duration = effectiveDuration };
            var mergedTags = MergeTags(factoryTags, extraTags);
            SetInternal(normalizedKey, framed, opts.Duration, mergedTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return value;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in GetOrSetPayloadAsync for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetPayloadAsync"), (Constants.Metrics.Tags.Key, key)]);
            return await SerializedPayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, IEnumerable<string>? extraTags = null)
        => GetOrSetPayload(key, factory, null, extraTags);

    /// <inheritdoc />
    public CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddLocalCache which registers it).");

        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return PayloadFactoryOnlySync(factory);

        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            if (_memoryCache.TryGetValue(normalizedKey, out byte[]? cached) && cached != null) {
                try {
                    var decoded = _payloadCodec.Decode(cached);
                    stopwatch.Stop();
                    _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                    _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                    return decoded;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to decode payload cache for key {CacheKey}; removing entry", key);
                    InvalidateCacheItem(key).GetAwaiter().GetResult();
                }
            }

            var plain = factory(CancellationToken.None);
            if (plain == null)
                return null;

            var (framed, envelope) = _payloadCodec.EncodeReturningEnvelope(plain);
            var opts = new CacheEntryOptions { Duration = effectiveDuration };
            SetInternal(normalizedKey, framed, opts.Duration, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in GetOrSetPayload for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, "GetOrSetPayload"), (Constants.Metrics.Tags.Key, key)]);
            return PayloadFactoryOnlySync(factory);
        }
    }

    /// <inheritdoc />
    public void SetPayload(string key, ReadOnlySpan<byte> plaintext, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key, nameof(key));
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddLocalCache which registers it).");

        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        try {
            var framed = _payloadCodec.Encode(plaintext);
            var normalizedKey = key.ToLowerInvariant();
            SetInternal(normalizedKey, framed, _options.DefaultExpiration, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.SetDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.SetSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error setting payload cache for key {CacheKey}", key);
            _metrics.RecordError(Constants.Metrics.SetDuration, ex, [(Constants.Metrics.Tags.Operation, "SetPayload"), (Constants.Metrics.Tags.Key, key)]);
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryGetPayload(string key, out CacheEntryEnvelope? envelope)
    {
        envelope = null;
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddLocalCache which registers it).");

        if (!_enabled || string.IsNullOrWhiteSpace(key))
            return false;

        try {
            var normalizedKey = key.ToLowerInvariant();
            if (!_memoryCache.TryGetValue(normalizedKey, out byte[]? raw) || raw == null)
                return false;

            envelope = _payloadCodec.Decode(raw);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error decoding payload cache for key {CacheKey}", key);
            return false;
        }
    }

    private static async Task<CacheEntryEnvelope?> PayloadFactoryOnlyAsync(Func<CancellationToken, Task<byte[]?>> factory, CancellationToken token)
    {
        var plain = await factory(token).ConfigureAwait(false);
        if (plain == null)
            return null;

        return new(plain);
    }

    private static async Task<CacheEntryEnvelope?> PayloadTupleFactoryOnlyAsync(
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        CancellationToken token)
    {
        var (plain, _) = await factory(token).ConfigureAwait(false);
        if (plain == null)
            return null;

        return new(plain);
    }

    private static CacheEntryEnvelope? PayloadFactoryOnlySync(Func<CancellationToken, byte[]?> factory)
    {
        var plain = factory(CancellationToken.None);
        if (plain == null)
            return null;

        return new(plain);
    }

    private static async Task<T?> SerializedPayloadTupleFactoryOnlyAsync<T>(
        Func<CancellationToken, Task<(T? value, string[]? tags)>> factory,
        CancellationToken token)
    {
        var (value, _) = await factory(token).ConfigureAwait(false);
        return value;
    }
    
    private static string[]? MergeTags(string[]? factoryTags, IEnumerable<string>? extraTags)
    {
        var hasFactory = factoryTags is { Length: > 0 };
        var hasExtra = extraTags != null;
        if (!hasFactory && !hasExtra)
            return null;

        if (!hasFactory)
            return extraTags!.Select(t => t.ToLowerInvariant()).ToArray();

        if (!hasExtra)
            return factoryTags!.Select(t => t.ToLowerInvariant()).ToArray();

        return factoryTags!.Concat(extraTags!).Select(t => t.ToLowerInvariant()).Distinct().ToArray();
    }

    private void SetInternal<T>(string normalizedKey, T value, TimeSpan duration, IEnumerable<string>? tags)
    {
        var tagList = tags?.Select(t => t.ToLowerInvariant()).ToArray() ?? [];
        RemoveKeyFromTagMappings(normalizedKey);
        var entryOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration };
        entryOptions.RegisterPostEvictionCallback((_, _, _, _) => {
            RemoveKeyFromTagMappings(normalizedKey);
            _items.TryRemove(CacheItem.Key(normalizedKey), out var _);
        });

        _memoryCache.Set(normalizedKey, value, entryOptions);
        _items.TryAdd(CacheItem.Key(normalizedKey), 0);
        _keyToTags[normalizedKey] = tagList;
        foreach (var tag in tagList) {
            var keys = _tagToKeys.GetOrAdd(tag, _ => new());
            keys[normalizedKey] = 0;
            _items.TryAdd(CacheItem.Tag(TagPrefix + tag), 0);
        }
    }

    private void RemoveKeyFromTagMappings(string normalizedKey)
    {
        if (!_keyToTags.TryRemove(normalizedKey, out var tags))
            return;

        foreach (var tag in tags) {
            if (_tagToKeys.TryGetValue(tag, out var keys)) {
                keys.TryRemove(normalizedKey, out var _);
                if (keys.IsEmpty)
                    _tagToKeys.TryRemove(tag, out var _);
            }

            _items.TryRemove(CacheItem.Tag(TagPrefix + tag), out var _);
        }
    }
}