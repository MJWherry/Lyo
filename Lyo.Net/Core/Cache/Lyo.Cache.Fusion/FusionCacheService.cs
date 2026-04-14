using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Health;
using Lyo.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZiggyCreatures.Caching.Fusion;

namespace Lyo.Cache.Fusion;

/// <summary>FusionCache-based implementation of ICacheService with optional Redis backplane support.</summary>
public sealed class FusionCacheService : ICacheService
{
    private const string TagPrefix = "__fc:t:";

    private readonly bool _enabled;
    private readonly IFusionCache _fusionCache;
    private readonly ConcurrentDictionary<CacheItem, byte> _items = new();
    private readonly ILogger<FusionCacheService> _logger;
    private readonly IMetrics _metrics;
    private readonly CacheOptions _options;
    private readonly ICachePayloadCodec? _payloadCodec;
    private readonly ICachePayloadSerializer? _payloadSerializer;

    public FusionCacheService(
        IFusionCache fusionCache,
        ILogger<FusionCacheService>? logger = null,
        CacheOptions? options = null,
        IMetrics? metrics = null,
        ICachePayloadCodec? payloadCodec = null,
        ICachePayloadSerializer? payloadSerializer = null)
    {
        _fusionCache = fusionCache ?? throw new ArgumentNullException(nameof(fusionCache));
        _logger = logger ?? NullLogger<FusionCacheService>.Instance;
        _options = options ?? new CacheOptions();
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _payloadCodec = payloadCodec;
        _payloadSerializer = payloadSerializer;
        _enabled = _options.Enabled;
        if (!_enabled)
            return;

        _fusionCache.DefaultEntryOptions.Priority = CacheItemPriority.Normal;
        _fusionCache.DefaultEntryOptions.Duration = _options.DefaultExpiration;
        _fusionCache.DefaultEntryOptions.IsFailSafeEnabled = true;
        _fusionCache.DefaultEntryOptions.FailSafeMaxDuration = TimeSpan.FromHours(24);
        _fusionCache.DefaultEntryOptions.FailSafeThrottleDuration = TimeSpan.FromSeconds(30);
        if (_fusionCache.HasBackplane && !_fusionCache.HasDistributedCache)
            _fusionCache.DefaultEntryOptions.SkipBackplaneNotifications = true;

        _fusionCache.Events.Set += (_, args) => {
            try {
                if (args.Key.StartsWith(TagPrefix + TagPrefix))
                    return;

                var item = args.Key.StartsWith(TagPrefix) ? CacheItem.Tag(args.Key) : CacheItem.Key(args.Key);
                _items.TryAdd(item, 0);
                _logger.LogDebug("Added {CacheType} {CacheKey}", item.Type, item.Name);
                _metrics.RecordGauge(Constants.Metrics.CacheSize, _items.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error handling cache Set event for key {CacheKey}", args.Key);
                _metrics.RecordError(Constants.Metrics.SetDuration, ex, [(Constants.Metrics.Tags.Operation, "SetEvent"), (Constants.Metrics.Tags.Key, args.Key)]);
            }
        };

        _fusionCache.Events.Remove += (_, args) => {
            try {
                var item = args.Key.StartsWith(TagPrefix) ? CacheItem.Tag(args.Key) : CacheItem.Key(args.Key);
                _items.TryRemove(item, out var _);
                _logger.LogDebug("Removed {CacheType} {CacheKey}", item.Type, item.Name);
                _metrics.RecordGauge(Constants.Metrics.CacheSize, _items.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error handling cache Remove event for key {CacheKey}", args.Key);
                _metrics.RecordError(Constants.Metrics.RemoveDuration, ex, [(Constants.Metrics.Tags.Operation, "RemoveEvent"), (Constants.Metrics.Tags.Key, args.Key)]);
            }
        };

        _fusionCache.Events.Expire += (_, args) => {
            try {
                var item = args.Key.StartsWith(TagPrefix) ? CacheItem.Tag(args.Key) : CacheItem.Key(args.Key);
                _items.TryRemove(item, out var _);
                _logger.LogDebug("Expired {CacheType} {CacheKey}", item.Type, item.Name);
                _metrics.RecordGauge(Constants.Metrics.CacheSize, _items.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error handling cache Expire event for key {CacheKey}", args.Key);
                _metrics.RecordError(Constants.Metrics.RemoveDuration, ex, [(Constants.Metrics.Tags.Operation, "ExpireEvent"), (Constants.Metrics.Tags.Key, args.Key)]);
            }
        };

        _fusionCache.Events.RemoveByTag += (_, args) => {
            try {
                var item = CacheItem.Tag(args.Tag);
                _items.TryRemove(item, out var _);
                _logger.LogDebug("Removed tag {CacheTag}", args.Tag);
                _metrics.RecordGauge(Constants.Metrics.CacheSize, _items.Count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error handling cache RemoveByTag event for tag {CacheTag}", args.Tag);
                _metrics.RecordError(Constants.Metrics.RemoveByTagDuration, ex, [(Constants.Metrics.Tags.Operation, "RemoveByTagEvent"), (Constants.Metrics.Tags.Tag, args.Tag)]);
            }
        };

        _fusionCache.Events.Clear += (_, _) => {
            try {
                var count = _items.Count;
                _items.Clear();
                _logger.LogInformation("Cleared {CacheKeyCount} cache items", count);
                _metrics.RecordGauge(Constants.Metrics.CacheSize, 0);
                _metrics.IncrementCounter(Constants.Metrics.ClearSuccess, count);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error handling cache Clear event");
                _metrics.RecordError(Constants.Metrics.ClearSuccess, ex, [(Constants.Metrics.Tags.Operation, "ClearEvent")]);
            }
        };
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
            await _fusionCache.RemoveAsync(key.ToLowerInvariant()).ConfigureAwait(false);
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
        try {
            var beforeCount = _items.Count;
            await _fusionCache.RemoveByTagAsync(tag.ToLowerInvariant()).ConfigureAwait(false);
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
    }

    public async Task InvalidateQueryCacheAsync<TDb>()
        where TDb : class
    {
        if (!_enabled)
            return;

        var entityTag = $"entity:{typeof(TDb).Name.ToLowerInvariant()}";
        await InvalidateCacheItemByTag(entityTag).ConfigureAwait(false);
    }

    public async Task InvalidateCacheByTypeAsync(string fullTypeName)
    {
        if (!_enabled)
            return;

        if (string.IsNullOrWhiteSpace(fullTypeName)) {
            _logger.LogWarning("Attempted to invalidate cache for null or empty type name");
            return;
        }

        var tag = $"type:{fullTypeName.ToLowerInvariant()}";
        var stopwatch = Stopwatch.StartNew();
        try {
            var beforeCount = _items.Count;
            await _fusionCache.RemoveByTagAsync(tag).ConfigureAwait(false);
            stopwatch.Stop();
            var itemsRemoved = Math.Max(0, beforeCount - _items.Count);
            var tags = new[] { (Constants.Metrics.Tags.Tag, tag) };
            _metrics.RecordTiming(Constants.Metrics.RemoveByTagDuration, stopwatch.Elapsed, tags);
            _metrics.IncrementCounter(Constants.Metrics.RemoveByTagSuccess, 1, tags);
            _metrics.RecordGauge(Constants.Metrics.RemoveByTagItemsRemoved, itemsRemoved, tags);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating cache for type {FullTypeName}", fullTypeName);
            _metrics.RecordError(Constants.Metrics.RemoveByTagDuration, ex, [(Constants.Metrics.Tags.Operation, "InvalidateCacheByTypeAsync"), (Constants.Metrics.Tags.Tag, tag)]);
            throw;
        }
    }

    public Task InvalidateCacheByTypeAsync(Type type) => InvalidateCacheByTypeAsync(type.FullName ?? type.Name);

    public Task InvalidateCacheByTypeAsync<T>() => InvalidateCacheByTypeAsync(typeof(T));

    public async Task InvalidateAllCachedQueriesAsync()
    {
        if (!_enabled)
            return;

        const string tag = "queries";
        var stopwatch = Stopwatch.StartNew();
        try {
            var beforeCount = _items.Count;
            await _fusionCache.RemoveByTagAsync(tag).ConfigureAwait(false);
            stopwatch.Stop();
            var itemsRemoved = Math.Max(0, beforeCount - _items.Count);
            var tags = new[] { (Constants.Metrics.Tags.Tag, tag) };
            _metrics.RecordTiming(Constants.Metrics.RemoveByTagDuration, stopwatch.Elapsed, tags);
            _metrics.IncrementCounter(Constants.Metrics.RemoveByTagSuccess, 1, tags);
            _metrics.RecordGauge(Constants.Metrics.RemoveByTagItemsRemoved, itemsRemoved, tags);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error invalidating all cached queries");
            _metrics.RecordError(
                Constants.Metrics.RemoveByTagDuration, ex, [(Constants.Metrics.Tags.Operation, "InvalidateAllCachedQueriesAsync"), (Constants.Metrics.Tags.Tag, tag)]);

            throw;
        }
    }

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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    key.ToLowerInvariant(), async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, (Action<FusionCacheEntryOptions>?)null, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    key.ToLowerInvariant(), async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, opts => opts.Duration = effectiveDuration, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    key.ToLowerInvariant(), async (ctx, ct) => {
                        factoryCalled = true;
                        var (value, factoryTags) = await factory(ct).ConfigureAwait(false);
                        var merged = MergeTags(factoryTags, extraTags);
                        ctx.Tags = merged ?? [];
                        return value;
                    }, (Action<FusionCacheEntryOptions>?)null, null, token)
                .ConfigureAwait(false);

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    key.ToLowerInvariant(), async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, opts => opts.Duration = typeExpiration, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = _fusionCache.GetOrSet<TValue?>(
                key.ToLowerInvariant(), (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, (Action<FusionCacheEntryOptions>?)null, extraTags?.Select(i => i.ToLowerInvariant()));

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = _fusionCache.GetOrSet<TValue?>(
                key.ToLowerInvariant(), (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, opts => opts.Duration = effectiveDuration, extraTags?.Select(i => i.ToLowerInvariant()));

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = _fusionCache.GetOrSet<TValue?>(
                key.ToLowerInvariant(), (ctx, ct) => {
                    factoryCalled = true;
                    var (value, factoryTags) = factory(ct);
                    var merged = MergeTags(factoryTags, extraTags);
                    ctx.Tags = merged ?? [];
                    return value;
                }, (Action<FusionCacheEntryOptions>?)null);

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var result = _fusionCache.GetOrSet<TValue?>(
                key.ToLowerInvariant(), (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, opts => opts.Duration = typeExpiration, extraTags?.Select(i => i.ToLowerInvariant()));

            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
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

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            var cachedValue = _fusionCache.TryGet<TValue>(normalizedKey);
            if (cachedValue.HasValue) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cachedValue.Value;
            }

            var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
            setupAction?.Invoke(new FusionCacheEntryOptionsAdapter(opts));
            _fusionCache.Set(normalizedKey, value, opts, tags?.Select(i => i.ToLowerInvariant()));
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

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            var cachedValue = await _fusionCache.TryGetAsync<TValue>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cachedValue.Value;
            }

            var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
            setupAction?.Invoke(new FusionCacheEntryOptionsAdapter(opts));
            await _fusionCache.SetAsync(normalizedKey, value, opts, tags?.Select(i => i.ToLowerInvariant()), token).ConfigureAwait(false);
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
            _fusionCache.Set(key.ToLowerInvariant(), obj, null, tags?.Select(i => i.ToLowerInvariant()));
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
            var cachedValue = _fusionCache.TryGet<T>(normalizedKey);
            if (!cachedValue.HasValue)
                return false;

            value = cachedValue.Value;
            return true;
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
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddFusionCache which registers it).");

        if (!_enabled)
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetPayloadAsync called with null or empty key, falling back to factory");
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }

        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = await _fusionCache.TryGetAsync<byte[]>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
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
            var opts = new FusionCacheEntryOptions { Duration = effectiveDuration };
            await _fusionCache.SetAsync(normalizedKey, framed, opts, extraTags?.Select(i => i.ToLowerInvariant()), token).ConfigureAwait(false);
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
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddFusionCache which registers it).");

        if (!_enabled)
            return await PayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetPayloadAsync called with null or empty key, falling back to factory");
            return await PayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }

        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = await _fusionCache.TryGetAsync<byte[]>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
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
            var opts = new FusionCacheEntryOptions { Duration = effectiveDuration };
            var mergedTags = MergeTags(factoryTags, extraTags);
            await _fusionCache.SetAsync(normalizedKey, framed, opts, mergedTags, token).ConfigureAwait(false);
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
        if (_payloadCodec == null || _payloadSerializer == null)
            throw new InvalidOperationException("Typed payload cache requires ICachePayloadCodec and ICachePayloadSerializer (use AddFusionCache which registers both).");

        if (!_enabled)
            return await SerializedPayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetPayloadAsync called with null or empty key, falling back to factory");
            return await SerializedPayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }

        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = await _fusionCache.TryGetAsync<byte[]>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                CacheEntryEnvelope? decoded = null;
                try {
                    decoded = _payloadCodec.Decode(cachedValue.Value);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to decode payload cache for key {CacheKey}; removing entry", key);
                    await InvalidateCacheItem(key).ConfigureAwait(false);
                }

                if (decoded != null) {
                    try {
                        var deserialized = _payloadSerializer.Deserialize<TValue>(PayloadBytes(decoded.Payload));
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
            var opts = new FusionCacheEntryOptions { Duration = effectiveDuration };
            var mergedTags = MergeTags(factoryTags, extraTags);
            await _fusionCache.SetAsync(normalizedKey, framed, opts, mergedTags, token).ConfigureAwait(false);
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
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddFusionCache which registers it).");

        if (!_enabled)
            return PayloadFactoryOnlySync(factory);

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("GetOrSetPayload called with null or empty key, falling back to factory");
            return PayloadFactoryOnlySync(factory);
        }

        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = _fusionCache.TryGet<byte[]>(normalizedKey);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
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

            var plain = factory(default);
            if (plain == null)
                return null;

            var (framed, envelope) = _payloadCodec.EncodeReturningEnvelope(plain);
            var opts = new FusionCacheEntryOptions { Duration = effectiveDuration };
            _fusionCache.Set(normalizedKey, framed, opts, extraTags?.Select(i => i.ToLowerInvariant()));
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
        if (_payloadCodec == null)
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddFusionCache which registers it).");

        if (!_enabled)
            return;

        if (string.IsNullOrWhiteSpace(key)) {
            _logger.LogWarning("Attempted to set payload cache with null or empty key");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try {
            var framed = _payloadCodec.Encode(plaintext);
            _fusionCache.Set(key.ToLowerInvariant(), framed, null, tags?.Select(i => i.ToLowerInvariant()));
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
            throw new InvalidOperationException("Payload cache requires ICachePayloadCodec (use AddFusionCache which registers it).");

        if (!_enabled || string.IsNullOrWhiteSpace(key))
            return false;

        try {
            var normalizedKey = key.ToLowerInvariant();
            var cachedValue = _fusionCache.TryGet<byte[]>(normalizedKey);
            if (!cachedValue.HasValue)
                return false;

            envelope = _payloadCodec.Decode(cachedValue.Value);
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

        return new CacheEntryEnvelope { Payload = plain };
    }

    private static async Task<T?> SerializedPayloadTupleFactoryOnlyAsync<T>(
        Func<CancellationToken, Task<(T? value, string[]? tags)>> factory,
        CancellationToken token)
    {
        var (value, _) = await factory(token).ConfigureAwait(false);
        return value;
    }

    private static ReadOnlySpan<byte> PayloadBytes(IReadOnlyList<byte> payload)
        => payload is byte[] b ? b : payload.ToArray();

    private static async Task<CacheEntryEnvelope?> PayloadTupleFactoryOnlyAsync(
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        CancellationToken token)
    {
        var (plain, _) = await factory(token).ConfigureAwait(false);
        if (plain == null)
            return null;

        return new CacheEntryEnvelope { Payload = plain };
    }

    private static CacheEntryEnvelope? PayloadFactoryOnlySync(Func<CancellationToken, byte[]?> factory)
    {
        var plain = factory(default);
        if (plain == null)
            return null;

        return new CacheEntryEnvelope { Payload = plain };
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

    private void RecordGetOrSetMetrics(string key, bool factoryCalled, TimeSpan elapsed)
    {
        if (factoryCalled) {
            _metrics.RecordTiming(Constants.Metrics.MissDuration, elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        else {
            _metrics.RecordTiming(Constants.Metrics.HitDuration, elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
    }
}