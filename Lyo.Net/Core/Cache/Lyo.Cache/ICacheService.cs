using Lyo.Health;

namespace Lyo.Cache;

/// <summary>Cache service contract for get-or-set, invalidation, and tag-based operations.</summary>
public interface ICacheService : IHealth
{
    /// <summary>Gets a read-only snapshot of all cached items. Thread-safe.</summary>
    IReadOnlyCollection<CacheItem> Items { get; }

    Task InvalidateCacheItem(string key);

    Task InvalidateCacheItemByTag(string tag);

    Task InvalidateQueryCacheAsync<TDb>()
        where TDb : class;

    Task InvalidateCacheByTypeAsync(string fullTypeName);

    Task InvalidateCacheByTypeAsync(Type type);

    Task InvalidateCacheByTypeAsync<T>();

    Task InvalidateAllCachedQueriesAsync();

    /// <summary>Gets or sets a cached value. Factory returns value only; use extraTags for tags.</summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(string key, Func<CancellationToken, Task<TValue?>> factory, IEnumerable<string>? extraTags = null, CancellationToken token = default);

    /// <summary>Gets or sets a cached value with custom duration.</summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets a cached value. Factory returns (value, tags); tags are merged with extraTags.</summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets a cached value with type-specific expiration.</summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        Type type,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets a cached value (sync). Factory returns value only.</summary>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets a cached value (sync) with custom duration.</summary>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets a cached value (sync). Factory returns (value, tags).</summary>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, (TValue? value, string[]? tags)> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets a cached value (sync) with type-specific expiration.</summary>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, Type type, IEnumerable<string>? extraTags = null);

    TValue? GetOrSet<TValue>(string key, TValue value, Action<ICacheEntryOptions>? setupAction = null, IEnumerable<string>? tags = null);

    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        TValue value,
        Action<ICacheEntryOptions>? setupAction = null,
        IEnumerable<string>? tags = null,
        CancellationToken token = default);

    void Set<T>(string key, T obj, IEnumerable<string>? tags = null);

    /// <summary>Tries to read a cached value without invoking a factory. Returns false when the key is missing, expired, or cache is disabled.</summary>
    bool TryGetValue<T>(string key, out T? value);
}