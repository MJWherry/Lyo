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
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _tagToKeys = new();

    public LocalCacheService(IMemoryCache memoryCache, ILogger<LocalCacheService>? logger = null, CacheOptions? options = null, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(memoryCache, nameof(memoryCache));
        _memoryCache = memoryCache;
        _logger = logger ?? NullLogger<LocalCacheService>.Instance;
        _options = options ?? new CacheOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _enabled = _options.Enabled;
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
        if (!_enabled)
            return;

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("Attempted to invalidate cache item with null or empty key");
            return;
        }

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
        if (!_enabled)
            return;

        if (string.IsNullOrWhiteSpace(tag)) {
            _logger.LogWarning("Attempted to invalidate cache items with null or empty tag");
            return;
        }

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
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetAsync called with null or empty key, falling back to factory");
            return await factory(token).ConfigureAwait(false);
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
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetAsync called with null or empty key, falling back to factory");
            return await factory(token).ConfigureAwait(false);
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
        if (!_enabled) {
            var (value, _) = await factory(token).ConfigureAwait(false);
            return value;
        }

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetAsync called with null or empty key, falling back to factory");
            var (v, _) = await factory(token).ConfigureAwait(false);
            return v;
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
        var typeExpiration = _options.GetExpirationForType(type);
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetAsync called with null or empty key, falling back to factory");
            return await factory(token).ConfigureAwait(false);
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
        if (!_enabled)
            return factory(default);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSet called with null or empty key, falling back to factory");
            return factory(default);
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

            var result = factory(default);
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
            return factory(default);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null)
    {
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return factory(default);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSet called with null or empty key, falling back to factory");
            return factory(default);
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

            var result = factory(default);
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
            return factory(default);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, (TValue? value, string[]? tags)> factory, IEnumerable<string>? extraTags = null)
    {
        if (!_enabled) {
            var (value, _) = factory(default);
            return value;
        }

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSet called with null or empty key, falling back to factory");
            var (v, _) = factory(default);
            return v;
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

            var (result, factoryTags) = factory(default);
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
            var (v, _) = factory(default);
            return v;
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, Type type, IEnumerable<string>? extraTags = null)
    {
        var typeExpiration = _options.GetExpirationForType(type);
        if (!_enabled)
            return factory(default);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSet called with null or empty key, falling back to factory");
            return factory(default);
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

            var result = factory(default);
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
            return factory(default);
        }
    }

    public TValue? GetOrSet<TValue>(string key, TValue value, Action<ICacheEntryOptions>? setupAction = null, IEnumerable<string>? tags = null)
    {
        if (!_enabled)
            return value;

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSet called with null or empty key");
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
        if (!_enabled)
            return value;

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetAsync called with null or empty key");
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
        if (!_enabled)
            return;

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("Attempted to set cache value with null or empty key");
            return;
        }

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