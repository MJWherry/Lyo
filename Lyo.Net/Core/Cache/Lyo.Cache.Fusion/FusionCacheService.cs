using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Cache;
using Lyo.Exceptions;
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
    private const string MsgPayloadCodecRequired = "Payload cache requires ICachePayloadCodec (use AddFusionCache which registers it).";
    private const string MsgTypedPayloadCodecRequired = "Typed payload cache requires ICachePayloadCodec (use AddFusionCache which registers both).";
    private const string MsgTypedPayloadSerializerRequired = "Typed payload cache requires ICachePayloadSerializer (use AddFusionCache which registers both).";

    private readonly bool _enabled;
    private readonly IFusionCache _fusionCache;
    private readonly ConcurrentDictionary<CacheItem, CacheItem> _items = new();
    private readonly ConcurrentDictionary<string, CacheItem> _storageHints = new(StringComparer.Ordinal);
    private readonly ILogger<FusionCacheService> _logger;
    private readonly IMetrics _metrics;
    private readonly CacheOptions _options;
    private readonly ICachePayloadCodec? _payloadCodec;
    private readonly ICachePayloadSerializer? _payloadSerializer;
    private readonly ConcurrentDictionary<string, EntryPolicy> _entryPolicies = new();

    public FusionCacheService(
        IFusionCache fusionCache,
        ILogger<FusionCacheService>? logger = null,
        CacheOptions? options = null,
        IMetrics? metrics = null,
        ICachePayloadCodec? payloadCodec = null,
        ICachePayloadSerializer? payloadSerializer = null)
    {
        ArgumentHelpers.ThrowIfNull(fusionCache);
        _fusionCache = fusionCache;
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

                CacheItem item;
                var expires = ExpiresFromPolicy(args.Key);
                if (args.Key.StartsWith(TagPrefix))
                    item = CacheItem.Tag(args.Key);
                else if (_storageHints.TryGetValue(args.Key, out var hint))
                    item = hint with { Expires = expires ?? hint.Expires };
                else
                    item = CacheItem.Key(args.Key, encrypted: false, compressed: false, expires: expires);

                UpsertItem(item);
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
                ForgetPolicy(args.Key);
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
                ForgetPolicy(args.Key);
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
                ForgetPoliciesByTag(args.Tag);
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
                _entryPolicies.Clear();
                _storageHints.Clear();
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

    public IReadOnlyCollection<CacheItem> Items => _items.Values.ToList().AsReadOnly();

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

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            ForgetPolicy(normalizedKey);
            await _fusionCache.RemoveAsync(normalizedKey).ConfigureAwait(false);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.RemoveDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.RemoveSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            RecordInvalidateKeyFailure(stopwatch, ex, key);
            throw;
        }
    }

    public async Task InvalidateCacheItemByTag(string tag)
    {
        if (!_enabled)
            return;

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag);
        var stopwatch = Stopwatch.StartNew();
        try {
            var beforeCount = _items.Count;
            var normalizedTag = tag.ToLowerInvariant();
            ForgetPoliciesByTag(normalizedTag);
            await _fusionCache.RemoveByTagAsync(normalizedTag).ConfigureAwait(false);
            stopwatch.Stop();
            var itemsRemoved = Math.Max(0, beforeCount - _items.Count);
            var tags = new[] { (Constants.Metrics.Tags.Tag, tag) };
            _metrics.RecordTiming(Constants.Metrics.RemoveByTagDuration, stopwatch.Elapsed, tags);
            _metrics.IncrementCounter(Constants.Metrics.RemoveByTagSuccess, 1, tags);
            _metrics.RecordGauge(Constants.Metrics.RemoveByTagItemsRemoved, itemsRemoved, tags);
        }
        catch (Exception ex) {
            RecordInvalidateByTagOperationFailure(
                stopwatch, ex, "InvalidateCacheItemByTag", tag, e => _logger.LogError(e, "Error invalidating cache items by tag {CacheTag}", tag));

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
            RecordInvalidateByTagOperationFailure(
                stopwatch, ex, "InvalidateCacheByTypeAsync", tag, e => _logger.LogError(e, "Error invalidating cache for type {FullTypeName}", fullTypeName));

            throw;
        }
    }

    public Task InvalidateCacheByTypeAsync(Type type) => InvalidateCacheByTypeAsync(type.FullName ?? type.Name);

    public Task InvalidateCacheByTypeAsync<T>() => InvalidateCacheByTypeAsync(typeof(T));

    public async Task InvalidateAllCachedQueriesAsync()
    {
        if (!_enabled)
            return;

        await InvalidateAllCachedQueriesByTagAsync("queries").ConfigureAwait(false);
    }

    public async Task ClearAsync()
    {
        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        try {
            await _fusionCache.ClearAsync().ConfigureAwait(false);
            stopwatch.Stop();
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _logger.LogError(ex, "Error clearing cache");
            _metrics.RecordError(Constants.Metrics.ClearSuccess, ex, [(Constants.Metrics.Tags.Operation, "ClearAsync")]);
            throw;
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    normalizedKey, async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, ((Action<FusionCacheEntryOptions>?)null)!, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            AfterGetOrSet(normalizedKey, result, factoryCalled, _options.DefaultExpiration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSetAsync", key);
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    normalizedKey, async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, opts => opts.Duration = effectiveDuration, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            AfterGetOrSet(normalizedKey, result, factoryCalled, effectiveDuration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSetAsync", key);
            return await factory(token).ConfigureAwait(false);
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled) {
            var (value, _) = await factory(token).ConfigureAwait(false);
            return value;
        }

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            string[]? mergedTags = null;
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    normalizedKey, async (ctx, ct) => {
                        factoryCalled = true;
                        var (value, factoryTags) = await factory(ct).ConfigureAwait(false);
                        mergedTags = MergeTags(factoryTags, extraTags);
                        ctx.Tags = mergedTags ?? [];
                        return value;
                    }, ((Action<FusionCacheEntryOptions>?)null)!, null, token)
                .ConfigureAwait(false);

            AfterGetOrSet(normalizedKey, result, factoryCalled, _options.DefaultExpiration, CacheExpirationMode.Absolute, mergedTags ?? extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSetAsync", key);
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var typeExpiration = _options.GetExpirationForType(type);
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    normalizedKey, async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, opts => opts.Duration = typeExpiration, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            AfterGetOrSet(normalizedKey, result, factoryCalled, typeExpiration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSetAsync", key);
            return await factory(token).ConfigureAwait(false);
        }
    }

    public async ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(setupAction);
        if (!_enabled)
            return await factory(token).ConfigureAwait(false);

        var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
        var adapter = new FusionCacheEntryOptionsAdapter(opts);
        setupAction(adapter);
        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = await _fusionCache.GetOrSetAsync<TValue?>(
                    normalizedKey, async (_, ct) => {
                        factoryCalled = true;
                        return await factory(ct).ConfigureAwait(false);
                    }, o => o.Duration = opts.Duration, extraTags?.Select(i => i.ToLowerInvariant()), token)
                .ConfigureAwait(false);

            AfterGetOrSet(normalizedKey, result, factoryCalled, opts.Duration, adapter.ExpirationMode, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSetAsync", key);
            return await factory(token).ConfigureAwait(false);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return factory(CancellationToken.None);

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = _fusionCache.GetOrSet<TValue?>(
                normalizedKey, (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, ((Action<FusionCacheEntryOptions>?)null)!, extraTags?.Select(i => i.ToLowerInvariant()));

            AfterGetOrSet(normalizedKey, result, factoryCalled, _options.DefaultExpiration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSet", key);
            return factory(CancellationToken.None);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        if (!_enabled)
            return factory(CancellationToken.None);

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = _fusionCache.GetOrSet<TValue?>(
                normalizedKey, (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, opts => opts.Duration = effectiveDuration, extraTags?.Select(i => i.ToLowerInvariant()));

            AfterGetOrSet(normalizedKey, result, factoryCalled, effectiveDuration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSet", key);
            return factory(CancellationToken.None);
        }
    }

    public TValue? GetOrSet<TValue>(
        string key,
        Func<CancellationToken, TValue?> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(setupAction);
        if (!_enabled)
            return factory(CancellationToken.None);

        var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
        var adapter = new FusionCacheEntryOptionsAdapter(opts);
        setupAction(adapter);
        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = _fusionCache.GetOrSet<TValue?>(
                normalizedKey, (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, o => o.Duration = opts.Duration, extraTags?.Select(i => i.ToLowerInvariant()));

            AfterGetOrSet(normalizedKey, result, factoryCalled, opts.Duration, adapter.ExpirationMode, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSet", key);
            return factory(CancellationToken.None);
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, (TValue? value, string[]? tags)> factory, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled) {
            var (value, _) = factory(CancellationToken.None);
            return value;
        }

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            string[]? mergedTags = null;
            var result = _fusionCache.GetOrSet<TValue?>(
                normalizedKey, (ctx, ct) => {
                    factoryCalled = true;
                    var (value, factoryTags) = factory(ct);
                    mergedTags = MergeTags(factoryTags, extraTags);
                    ctx.Tags = mergedTags ?? [];
                    return value;
                }, ((Action<FusionCacheEntryOptions>?)null)!);

            AfterGetOrSet(normalizedKey, result, factoryCalled, _options.DefaultExpiration, CacheExpirationMode.Absolute, mergedTags ?? extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSet", key);
            var (v, _) = factory(CancellationToken.None);
            return v;
        }
    }

    public TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, Type type, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        var typeExpiration = _options.GetExpirationForType(type);
        if (!_enabled)
            return factory(CancellationToken.None);

        var stopwatch = Stopwatch.StartNew();
        var factoryCalled = false;
        try {
            var normalizedKey = key.ToLowerInvariant();
            var result = _fusionCache.GetOrSet<TValue?>(
                normalizedKey, (_, ct) => {
                    factoryCalled = true;
                    return factory(ct);
                }, opts => opts.Duration = typeExpiration, extraTags?.Select(i => i.ToLowerInvariant()));

            AfterGetOrSet(normalizedKey, result, factoryCalled, typeExpiration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            RecordGetOrSetMetrics(key, factoryCalled, stopwatch.Elapsed);
            return result;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSet", key);
            return factory(CancellationToken.None);
        }
    }

    public TValue GetOrSet<TValue>(string key, TValue value, Action<ICacheEntryOptions>? setupAction = null, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return value;

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            var cachedValue = _fusionCache.TryGet<TValue>(normalizedKey);
            if (cachedValue.HasValue) {
                RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cachedValue.Value;
            }

            var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
            var adapter = new FusionCacheEntryOptionsAdapter(opts);
            setupAction?.Invoke(adapter);
            _fusionCache.Set(normalizedKey, value, opts, tags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, opts.Duration, adapter.ExpirationMode, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return value;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSet", key);
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return value;

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            var cachedValue = await _fusionCache.TryGetAsync<TValue>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
                stopwatch.Stop();
                _metrics.RecordTiming(Constants.Metrics.HitDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
                _metrics.IncrementCounter(Constants.Metrics.HitSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
                return cachedValue.Value;
            }

            var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
            var adapter = new FusionCacheEntryOptionsAdapter(opts);
            setupAction?.Invoke(adapter);
            await _fusionCache.SetAsync(normalizedKey, value, opts, tags?.Select(i => i.ToLowerInvariant()), token).ConfigureAwait(false);
            RememberPolicy(normalizedKey, opts.Duration, adapter.ExpirationMode, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return value;
        }
        catch (Exception ex) {
            RecordGetOrSetFailure(stopwatch, ex, "GetOrSetAsync", key);
            return value;
        }
    }

    public void Set<T>(string key, T obj, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        try {
            var normalizedKey = key.ToLowerInvariant();
            _fusionCache.Set(normalizedKey, obj, null, tags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, _options.DefaultExpiration, CacheExpirationMode.Absolute, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.SetDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.SetSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            RecordSetFailure(stopwatch, ex, key, false);
            throw;
        }
    }

    /// <inheritdoc />
    public void Set<T>(string key, T obj, TimeSpan duration, IEnumerable<string>? tags = null)
        => Set(key, obj, o => o.SetAbsoluteExpiration(duration), tags);

    /// <inheritdoc />
    public void Set<T>(string key, T obj, Action<ICacheEntryOptions> setupAction, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(setupAction);
        if (!_enabled)
            return;

        var stopwatch = Stopwatch.StartNew();
        try {
            var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
            var adapter = new FusionCacheEntryOptionsAdapter(opts);
            setupAction(adapter);
            var normalizedKey = key.ToLowerInvariant();
            _fusionCache.Set(normalizedKey, obj, opts, tags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, opts.Duration, adapter.ExpirationMode, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.SetDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.SetSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            RecordSetFailure(stopwatch, ex, key, false);
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
            if (!cachedValue.HasValue) {
                ForgetPolicy(normalizedKey);
                return false;
            }

            value = cachedValue.Value;
            RefreshSlidingIfNeeded(normalizedKey, value);
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
        => await GetOrSetPayloadAsync(key, factory, (TimeSpan?)null, extraTags, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = await _fusionCache.TryGetAsync<byte[]>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
                    TrackStoredBytes(normalizedKey, cachedValue.Value);
                    RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
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
            await SetFramedAsync(normalizedKey, framed, opts, extraTags?.Select(i => i.ToLowerInvariant()), token).ConfigureAwait(false);
            RememberPolicy(normalizedKey, effectiveDuration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            RecordPayloadOuterFailure(stopwatch, ex, "GetOrSetPayloadAsync", key);
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => await GetOrSetPayloadAsync(key, factory, (TimeSpan?)null, extraTags, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return await PayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = await _fusionCache.TryGetAsync<byte[]>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
                    TrackStoredBytes(normalizedKey, cachedValue.Value);
                    RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
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
            await SetFramedAsync(normalizedKey, framed, opts, mergedTags, token).ConfigureAwait(false);
            RememberPolicy(normalizedKey, effectiveDuration, CacheExpirationMode.Absolute, mergedTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            RecordPayloadOuterFailure(stopwatch, ex, "GetOrSetPayloadAsync", key);
            return await PayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => GetOrSetPayloadAsync(key, factory, (TimeSpan?)null, extraTags, token);

    /// <inheritdoc />
    public ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        async Task<(TValue? value, string[]? tags)> AsTuple(CancellationToken ct) => (await factory(ct).ConfigureAwait(false), null);

        return GetOrSetPayloadAsync(key, AsTuple, duration, extraTags, token);
    }

    /// <inheritdoc />
    public ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
        => GetOrSetPayloadAsync(key, factory, (TimeSpan?)null, extraTags, token);

    /// <inheritdoc />
    public async ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return await SerializedPayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgTypedPayloadCodecRequired);
        OperationHelpers.ThrowIfNull(_payloadSerializer, MsgTypedPayloadSerializerRequired);
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
                        var deserialized = _payloadSerializer.Deserialize<TValue>(decoded.Payload);
                        TrackStoredBytes(normalizedKey, cachedValue.Value);
                        RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
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
            await SetFramedAsync(normalizedKey, framed, opts, mergedTags, token).ConfigureAwait(false);
            RememberPolicy(normalizedKey, effectiveDuration, CacheExpirationMode.Absolute, mergedTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return value;
        }
        catch (Exception ex) {
            RecordPayloadOuterFailure(stopwatch, ex, "GetOrSetPayloadAsync", key);
            return await SerializedPayloadTupleFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, IEnumerable<string>? extraTags = null)
        => GetOrSetPayload(key, factory, (TimeSpan?)null, extraTags);

    /// <inheritdoc />
    public CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return PayloadFactoryOnlySync(factory);

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var normalizedKey = key.ToLowerInvariant();
        var effectiveDuration = duration ?? _options.DefaultExpiration;
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = _fusionCache.TryGet<byte[]>(normalizedKey);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
                    TrackStoredBytes(normalizedKey, cachedValue.Value);
                    RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
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
            var opts = new FusionCacheEntryOptions { Duration = effectiveDuration };
            SetFramed(normalizedKey, framed, opts, extraTags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, effectiveDuration, CacheExpirationMode.Absolute, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            RecordPayloadOuterFailure(stopwatch, ex, "GetOrSetPayload", key);
            return PayloadFactoryOnlySync(factory);
        }
    }

    /// <inheritdoc />
    public async ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(setupAction);
        if (!_enabled)
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
        var adapter = new FusionCacheEntryOptionsAdapter(opts);
        setupAction(adapter);
        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = await _fusionCache.TryGetAsync<byte[]>(normalizedKey, token: token).ConfigureAwait(false);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
                    TrackStoredBytes(normalizedKey, cachedValue.Value);
                    RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
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
            await SetFramedAsync(normalizedKey, framed, opts, extraTags?.Select(i => i.ToLowerInvariant()), token).ConfigureAwait(false);
            RememberPolicy(normalizedKey, opts.Duration, adapter.ExpirationMode, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            RecordPayloadOuterFailure(stopwatch, ex, "GetOrSetPayloadAsync", key);
            return await PayloadFactoryOnlyAsync(factory, token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public CacheEntryEnvelope? GetOrSetPayload(
        string key,
        Func<CancellationToken, byte[]?> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(setupAction);
        if (!_enabled)
            return PayloadFactoryOnlySync(factory);

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
        var adapter = new FusionCacheEntryOptionsAdapter(opts);
        setupAction(adapter);
        var normalizedKey = key.ToLowerInvariant();
        var stopwatch = Stopwatch.StartNew();
        try {
            var cachedValue = _fusionCache.TryGet<byte[]>(normalizedKey);
            if (cachedValue.HasValue) {
                try {
                    var decoded = _payloadCodec.Decode(cachedValue.Value);
                    TrackStoredBytes(normalizedKey, cachedValue.Value);
                    RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
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
            SetFramed(normalizedKey, framed, opts, extraTags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, opts.Duration, adapter.ExpirationMode, extraTags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.MissDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.MissSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
            return envelope;
        }
        catch (Exception ex) {
            RecordPayloadOuterFailure(stopwatch, ex, "GetOrSetPayload", key);
            return PayloadFactoryOnlySync(factory);
        }
    }

    /// <inheritdoc />
    public void SetPayload(string key, ReadOnlySpan<byte> plaintext, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (!_enabled)
            return;

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var stopwatch = Stopwatch.StartNew();
        try {
            var framed = _payloadCodec.Encode(plaintext);
            var normalizedKey = key.ToLowerInvariant();
            SetFramed(normalizedKey, framed, null, tags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, _options.DefaultExpiration, CacheExpirationMode.Absolute, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.SetDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.SetSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            RecordSetFailure(stopwatch, ex, key, true);
            throw;
        }
    }

    /// <inheritdoc />
    public void SetPayload(string key, ReadOnlySpan<byte> plaintext, TimeSpan duration, IEnumerable<string>? tags = null)
        => SetPayload(key, plaintext, o => o.SetAbsoluteExpiration(duration), tags);

    /// <inheritdoc />
    public void SetPayload(string key, ReadOnlySpan<byte> plaintext, Action<ICacheEntryOptions> setupAction, IEnumerable<string>? tags = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        ArgumentHelpers.ThrowIfNull(setupAction);
        if (!_enabled)
            return;

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        var stopwatch = Stopwatch.StartNew();
        try {
            var opts = new FusionCacheEntryOptions { Duration = _options.DefaultExpiration };
            var adapter = new FusionCacheEntryOptionsAdapter(opts);
            setupAction(adapter);
            var framed = _payloadCodec.Encode(plaintext);
            var normalizedKey = key.ToLowerInvariant();
            SetFramed(normalizedKey, framed, opts, tags?.Select(i => i.ToLowerInvariant()));
            RememberPolicy(normalizedKey, opts.Duration, adapter.ExpirationMode, tags);
            stopwatch.Stop();
            _metrics.RecordTiming(Constants.Metrics.SetDuration, stopwatch.Elapsed, [(Constants.Metrics.Tags.Key, key)]);
            _metrics.IncrementCounter(Constants.Metrics.SetSuccess, 1, [(Constants.Metrics.Tags.Key, key)]);
        }
        catch (Exception ex) {
            RecordSetFailure(stopwatch, ex, key, true);
            throw;
        }
    }

    /// <inheritdoc />
    public bool TryGetPayload(string key, out CacheEntryEnvelope? envelope)
    {
        envelope = null;
        if (!_enabled || string.IsNullOrWhiteSpace(key))
            return false;

        OperationHelpers.ThrowIfNull(_payloadCodec, MsgPayloadCodecRequired);
        try {
            var normalizedKey = key.ToLowerInvariant();
            var cachedValue = _fusionCache.TryGet<byte[]>(normalizedKey);
            if (!cachedValue.HasValue) {
                ForgetPolicy(normalizedKey);
                return false;
            }

            envelope = _payloadCodec.Decode(cachedValue.Value);
            TrackStoredBytes(normalizedKey, cachedValue.Value);
            RefreshSlidingIfNeeded(normalizedKey, cachedValue.Value);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error decoding payload cache for key {CacheKey}", key);
            return false;
        }
    }

    private async Task InvalidateAllCachedQueriesByTagAsync(string tag)
    {
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
            RecordInvalidateByTagOperationFailure(stopwatch, ex, "InvalidateAllCachedQueriesAsync", tag, e => _logger.LogError(e, "Error invalidating all cached queries"));
            throw;
        }
    }

    private static async Task<CacheEntryEnvelope?> PayloadFactoryOnlyAsync(Func<CancellationToken, Task<byte[]?>> factory, CancellationToken token)
    {
        var plain = await factory(token).ConfigureAwait(false);
        if (plain == null)
            return null;

        return new(plain);
    }

    private static async Task<T?> SerializedPayloadTupleFactoryOnlyAsync<T>(Func<CancellationToken, Task<(T? value, string[]? tags)>> factory, CancellationToken token)
    {
        var (value, _) = await factory(token).ConfigureAwait(false);
        return value;
    }

    private static async Task<CacheEntryEnvelope?> PayloadTupleFactoryOnlyAsync(Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory, CancellationToken token)
    {
        var (plain, _) = await factory(token).ConfigureAwait(false);
        if (plain == null)
            return null;

        return new(plain);
    }

    private static CacheEntryEnvelope? PayloadFactoryOnlySync(Func<CancellationToken, byte[]?> factory)
    {
        var plain = factory(CancellationToken.None);
        return plain is null ? null : new(plain);
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

    private void RecordInvalidateKeyFailure(Stopwatch stopwatch, Exception ex, string key)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "Error invalidating cache item with key {CacheKey}", key);
        _metrics.RecordError(Constants.Metrics.RemoveDuration, ex, [(Constants.Metrics.Tags.Operation, "InvalidateCacheItem"), (Constants.Metrics.Tags.Key, key)]);
    }

    private void RecordInvalidateByTagOperationFailure(Stopwatch stopwatch, Exception ex, string operationName, string tagMetricValue, Action<Exception> logError)
    {
        stopwatch.Stop();
        logError(ex);
        _metrics.RecordError(Constants.Metrics.RemoveByTagDuration, ex, [(Constants.Metrics.Tags.Operation, operationName), (Constants.Metrics.Tags.Tag, tagMetricValue)]);
    }

    private void RecordGetOrSetFailure(Stopwatch stopwatch, Exception ex, string operationName, string cacheKey)
    {
        stopwatch.Stop();
        _logger.LogError(ex, "Error getting or setting cache value for key {CacheKey}", cacheKey);
        _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, operationName), (Constants.Metrics.Tags.Key, cacheKey)]);
    }

    private void RecordPayloadOuterFailure(Stopwatch stopwatch, Exception ex, string operationName, string cacheKey)
    {
        stopwatch.Stop();
        var asyncOp = operationName.EndsWith("Async", StringComparison.Ordinal);
        _logger.LogError(ex, asyncOp ? "Error in GetOrSetPayloadAsync for key {CacheKey}" : "Error in GetOrSetPayload for key {CacheKey}", cacheKey);
        _metrics.RecordError(Constants.Metrics.MissDuration, ex, [(Constants.Metrics.Tags.Operation, operationName), (Constants.Metrics.Tags.Key, cacheKey)]);
    }

    private void RecordSetFailure(Stopwatch stopwatch, Exception ex, string cacheKey, bool isPayload)
    {
        stopwatch.Stop();
        _logger.LogError(ex, isPayload ? "Error setting payload cache for key {CacheKey}" : "Error setting cache value for key {CacheKey}", cacheKey);
        var operation = isPayload ? "SetPayload" : "Set";
        _metrics.RecordError(Constants.Metrics.SetDuration, ex, [(Constants.Metrics.Tags.Operation, operation), (Constants.Metrics.Tags.Key, cacheKey)]);
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

    private readonly record struct EntryPolicy(TimeSpan Duration, CacheExpirationMode Mode, string[] Tags);

    private static string[] NormalizeTags(IEnumerable<string>? tags)
        => tags?.Select(static t => t.ToLowerInvariant()).Distinct().ToArray() ?? [];

    private void RememberPolicy(string normalizedKey, TimeSpan duration, CacheExpirationMode mode, IEnumerable<string>? tags)
    {
        _entryPolicies[normalizedKey] = new(duration, mode, NormalizeTags(tags));
        StampExpires(normalizedKey, duration);
    }

    private DateTime? ExpiresFromPolicy(string key)
        => _entryPolicies.TryGetValue(key, out var policy) ? DateTime.UtcNow.Add(policy.Duration) : null;

    private void StampExpires(string normalizedKey, TimeSpan duration)
    {
        var expires = DateTime.UtcNow.Add(duration);
        if (_items.TryGetValue(CacheItem.Key(normalizedKey), out var existing)) {
            UpsertItem(existing with { Expires = expires });
            return;
        }

        if (_storageHints.TryGetValue(normalizedKey, out var hint)) {
            UpsertItem(hint with { Expires = expires });
            return;
        }

        UpsertItem(CacheItem.Key(normalizedKey, encrypted: false, compressed: false, expires: expires));
    }

    private void UpsertItem(CacheItem item)
        => _items.AddOrUpdate(item, item, (_, existing) => item with { Created = existing.Created });

    private void RememberStoredBytes(string normalizedKey, byte[] stored)
        => _storageHints[normalizedKey] = CacheItem.FromStoredBytes(normalizedKey, stored);

    private void TrackStoredBytes(string normalizedKey, byte[] stored)
    {
        RememberStoredBytes(normalizedKey, stored);
        var expires = ExpiresFromPolicy(normalizedKey);
        UpsertItem(CacheItem.FromStoredBytes(normalizedKey, stored, expires: expires));
    }

    private void SetFramed(string normalizedKey, byte[] framed, FusionCacheEntryOptions? opts, IEnumerable<string>? tags)
    {
        TrackStoredBytes(normalizedKey, framed);
        _fusionCache.Set(normalizedKey, framed, opts, tags);
    }

    private ValueTask SetFramedAsync(string normalizedKey, byte[] framed, FusionCacheEntryOptions? opts, IEnumerable<string>? tags, CancellationToken token)
    {
        TrackStoredBytes(normalizedKey, framed);
        return _fusionCache.SetAsync(normalizedKey, framed, opts, tags, token);
    }

    private void ForgetPolicy(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var normalized = key.ToLowerInvariant();
        _entryPolicies.TryRemove(normalized, out var _);
        _storageHints.TryRemove(normalized, out var _);
    }

    private void ForgetPoliciesByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        var normalizedTag = tag.ToLowerInvariant();
        foreach (var kvp in _entryPolicies) {
            if (kvp.Value.Tags.Contains(normalizedTag, StringComparer.Ordinal))
                _entryPolicies.TryRemove(kvp.Key, out var _);
        }
    }

    private void AfterGetOrSet<T>(string normalizedKey, T? value, bool factoryCalled, TimeSpan duration, CacheExpirationMode mode, IEnumerable<string>? tags)
    {
        if (factoryCalled)
            RememberPolicy(normalizedKey, duration, mode, tags);
        else
            RefreshSlidingIfNeeded(normalizedKey, value);
    }

    private void RefreshSlidingIfNeeded<T>(string normalizedKey, T? value)
    {
        if (!_entryPolicies.TryGetValue(normalizedKey, out var policy) || policy.Mode != CacheExpirationMode.Sliding)
            return;

        try {
            _fusionCache.Set(normalizedKey, value, new FusionCacheEntryOptions { Duration = policy.Duration }, policy.Tags);
            StampExpires(normalizedKey, policy.Duration);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error refreshing sliding cache entry for key {CacheKey}", normalizedKey);
            _metrics.RecordError(
                Constants.Metrics.SetDuration, ex, [(Constants.Metrics.Tags.Operation, "RefreshSliding"), (Constants.Metrics.Tags.Key, normalizedKey)]);
        }
    }
}