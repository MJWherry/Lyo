using Lyo.Health;

namespace Lyo.Cache;

/// <summary>Cache service contract for get-or-set, invalidation, and tag-based operations.</summary>
public interface ICacheService : IHealth
{
    /// <summary>Gets a read-only snapshot of all cached items. Thread-safe.</summary>
    IReadOnlyCollection<CacheItem> Items { get; }

    /// <summary>Removes the entry for <paramref name="key" /> from the cache and updates tag indexes.</summary>
    /// <param name="key">Logical cache key (normalized by the implementation, e.g. lower-invariant).</param>
    Task InvalidateCacheItem(string key);

    /// <summary>Removes every entry tagged with <paramref name="tag" />.</summary>
    /// <param name="tag">Tag string (e.g. <c>entity:order</c>, <c>queries</c>).</param>
    Task InvalidateCacheItemByTag(string tag);

    /// <summary>Invalidates cached queries tagged for entity type <typeparamref name="TDb" /> (typically <c>entity:&lt;name&gt;</c>).</summary>
    Task InvalidateQueryCacheAsync<TDb>()
        where TDb : class;

    /// <summary>Invalidates entries tagged for the given CLR type name (typically <c>type:&lt;full name&gt;</c>).</summary>
    /// <param name="fullTypeName">Full name of the type whose entries should be cleared; null/whitespace is ignored by typical implementations.</param>
    Task InvalidateCacheByTypeAsync(string fullTypeName);

    /// <summary>Convenience overload for <see cref="InvalidateCacheByTypeAsync(string)" /> using <paramref name="type" />.<see cref="Type.FullName" />.</summary>
    Task InvalidateCacheByTypeAsync(Type type);

    /// <summary>Convenience overload for <see cref="InvalidateCacheByTypeAsync(Type)" />.</summary>
    Task InvalidateCacheByTypeAsync<T>();

    /// <summary>Removes all entries tagged for general list/query caching (implementation-defined tag, e.g. <c>queries</c>).</summary>
    Task InvalidateAllCachedQueriesAsync();

    /// <summary>Gets or sets a cached value. Factory returns value only; use <paramref name="extraTags" /> for tags.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="extraTags">Optional tags merged into the entry for invalidation-by-tag.</param>
    /// <param name="token">Cancellation token passed to <paramref name="factory" />.</param>
    ValueTask<TValue?> GetOrSetAsync<TValue>(string key, Func<CancellationToken, Task<TValue?>> factory, IEnumerable<string>? extraTags = null, CancellationToken token = default);

    /// <summary>Gets or sets a cached value with custom duration.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="duration">Entry lifetime; null uses <see cref="CacheOptions.DefaultExpiration" />.</param>
    /// <param name="extraTags">Optional tags for invalidation.</param>
    /// <param name="token">Cancellation token.</param>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets a cached value. Factory returns (value, tags); tags are merged with <paramref name="extraTags" />.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value and per-entry tags on miss.</param>
    /// <param name="extraTags">Optional additional tags.</param>
    /// <param name="token">Cancellation token.</param>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets a cached value with type-specific expiration from <see cref="CacheOptions.TypeExpirations" /> for <paramref name="type" />.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="type">Type used to resolve expiration (typically the semantic type of the cached object).</param>
    /// <param name="extraTags">Optional tags.</param>
    /// <param name="token">Cancellation token.</param>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        Type type,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets a cached value (sync). Factory returns value only.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss (receives <see cref="CancellationToken.None" />).</param>
    /// <param name="extraTags">Optional tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets a cached value (sync) with custom duration.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="duration">Entry lifetime; null uses default expiration.</param>
    /// <param name="extraTags">Optional tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets a cached value (sync). Factory returns (value, tags).</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value and tags on miss.</param>
    /// <param name="extraTags">Optional extra tags merged with factory tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, (TValue? value, string[]? tags)> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets a cached value (sync) with type-specific expiration.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="type">Type used to resolve expiration.</param>
    /// <param name="extraTags">Optional tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, Type type, IEnumerable<string>? extraTags = null);

    /// <summary>If the key exists, returns the cached value; otherwise stores <paramref name="value" /> with optional <paramref name="setupAction" /> and tags.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to store on miss.</param>
    /// <param name="setupAction">Optional mutator for entry options (e.g. duration).</param>
    /// <param name="tags">Optional tags.</param>
    /// <returns>The existing or newly stored value.</returns>
    TValue? GetOrSet<TValue>(string key, TValue value, Action<ICacheEntryOptions>? setupAction = null, IEnumerable<string>? tags = null);

    /// <summary>Async variant of <see cref="GetOrSet{TValue}(string, TValue, Action{ICacheEntryOptions}?, IEnumerable{string}?)" />.</summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        TValue value,
        Action<ICacheEntryOptions>? setupAction = null,
        IEnumerable<string>? tags = null,
        CancellationToken token = default);

    /// <summary>Unconditionally sets a cache entry with default expiration unless overridden by options inside the implementation.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="obj">Value to store.</param>
    /// <param name="tags">Optional tags.</param>
    void Set<T>(string key, T obj, IEnumerable<string>? tags = null);

    /// <summary>Tries to read a cached value without invoking a factory. Returns false when the key is missing, expired, or cache is disabled.</summary>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>Gets or sets framed byte payload (see <see cref="CacheOptions.Payload" />). Requires <see cref="ICachePayloadCodec" /> registration.</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces plaintext bytes on miss (before codec framing).</param>
    /// <param name="extraTags">Optional tags.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Decoded envelope on hit or after miss; null when factory returns null.</returns>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets framed byte payload with a custom duration.</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces plaintext on miss.</param>
    /// <param name="duration">Entry lifetime; null uses default.</param>
    /// <param name="extraTags">Optional tags.</param>
    /// <param name="token">Cancellation token.</param>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets framed byte payload; factory returns plaintext bytes and tags (merged with <paramref name="extraTags" />).</summary>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets framed byte payload with a custom duration; factory returns plaintext bytes and tags.</summary>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Serializes <typeparamref name="TValue" /> with <see cref="ICachePayloadSerializer" />, frames with <see cref="ICachePayloadCodec" />, and returns the deserialized value.</summary>
    /// <typeparam name="TValue">Application object type.</typeparam>
    ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Same as the non-duration overload, with a custom entry duration.</summary>
    ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Like the byte-tuple payload overload, but serializes <typeparamref name="TValue" /> and merges tags.</summary>
    ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Same as the non-duration overload, with a custom entry duration.</summary>
    ValueTask<TValue?> GetOrSetPayloadAsync<TValue>(
        string key,
        Func<CancellationToken, Task<(TValue? value, string[]? tags)>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets framed byte payload (synchronous).</summary>
    CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Gets or sets framed byte payload with a custom duration (synchronous).</summary>
    CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null);

    /// <summary>Stores plaintext bytes using the payload codec (compress/encrypt per options).</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="plaintext">Application plaintext before framing.</param>
    /// <param name="tags">Optional tags.</param>
    void SetPayload(string key, ReadOnlySpan<byte> plaintext, IEnumerable<string>? tags = null);

    /// <summary>Tries to read and decode a framed payload. Returns false when missing, disabled, or decode fails.</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="envelope">Decoded plaintext envelope when the method returns true.</param>
    bool TryGetPayload(string key, out CacheEntryEnvelope? envelope);
}