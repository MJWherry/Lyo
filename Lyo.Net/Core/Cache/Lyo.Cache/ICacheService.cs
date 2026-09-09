using Lyo.Health;

namespace Lyo.Cache;

/// <summary>Get-or-set, invalidation, and tag operations for a cache.</summary>
public interface ICacheService : IHealth
{
    /// <summary>
    /// This process's in-memory (L1) key and tag list. Thread-safe. Redis (L2) keys written by other processes are not listed until this process loads them.
    /// Payload entries include <see cref="CacheItem.Encrypted" />, <see cref="CacheItem.Compressed" />, and <see cref="CacheItem.SizeBytes" />.
    /// Keys also carry <see cref="CacheItem.Expires" /> and <see cref="CacheItem.Tags" />.
    /// </summary>
    IReadOnlyCollection<CacheItem> Items { get; }

    /// <summary>Drops the entry for <paramref name="key" /> and updates tag indexes.</summary>
    /// <param name="key">Logical cache key (normalized by the implementation, for example lower-invariant).</param>
    Task InvalidateCacheItem(string key);

    /// <summary>Drops every entry tagged with <paramref name="tag" />.</summary>
    /// <param name="tag">Tag string (for example <c>entity:order</c>, <c>queries</c>).</param>
    Task InvalidateCacheItemByTag(string tag);

    /// <summary>Invalidates cached queries tagged for entity type <typeparamref name="TDb" /> (usually <c>entity:&lt;name&gt;</c>).</summary>
    Task InvalidateQueryCacheAsync<TDb>()
        where TDb : class;

    /// <summary>Invalidates entries tagged for the given CLR type name (usually <c>type:&lt;full name&gt;</c>).</summary>
    /// <param name="fullTypeName">Full name of the type whose entries should be cleared; null or whitespace is ignored by typical implementations.</param>
    Task InvalidateCacheByTypeAsync(string fullTypeName);

    /// <summary>Overload of <see cref="InvalidateCacheByTypeAsync(string)" /> using <paramref name="type" />.<see cref="Type.FullName" />.</summary>
    Task InvalidateCacheByTypeAsync(Type type);

    /// <summary>Overload of <see cref="InvalidateCacheByTypeAsync(Type)" />.</summary>
    Task InvalidateCacheByTypeAsync<T>();

    /// <summary>Drops every entry tagged for general list/query caching (implementation-defined tag, for example <c>queries</c>).</summary>
    Task InvalidateAllCachedQueriesAsync();

    /// <summary>Clears every cached entry and tag index for this process. Does not dump or enumerate a remote Redis store.</summary>
    Task ClearAsync();

    /// <summary>Reads or stores a cached value. Factory returns value only; use <paramref name="extraTags" /> for tags.</summary>
    /// <typeparam name="TValue">Type of the stored value.</typeparam>
    /// <param name="key">Lookup key.</param>
    /// <param name="factory">Builds the value on miss.</param>
    /// <param name="extraTags">Tags merged into the entry for invalidation-by-tag.</param>
    /// <param name="token">Token forwarded to <paramref name="factory" />.</param>
    ValueTask<TValue?> GetOrSetAsync<TValue>(string key, Func<CancellationToken, Task<TValue?>> factory, IEnumerable<string>? extraTags = null, CancellationToken token = default);

    /// <summary>Reads or stores a cached value with a caller-chosen duration.</summary>
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

    /// <summary>Reads or stores a cached value. Factory returns (value, tags); those tags are merged with <paramref name="extraTags" />.</summary>
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

    /// <summary>Reads or stores a cached value using the type-specific TTL from <see cref="CacheOptions.TypeExpirations" /> for <paramref name="type" />.</summary>
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

    /// <summary>
    /// Reads or stores a cached value. <paramref name="setupAction" /> stamps duration and
    /// <see cref="ICacheEntryOptions.ExpirationMode" /> on miss. Hits of sliding entries reset TTL from the stored policy.
    /// </summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Reads or stores a cached value (synchronous). Factory returns value only.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss (receives <see cref="CancellationToken.None" />).</param>
    /// <param name="extraTags">Optional tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Reads or stores a cached value (synchronous) with a caller-chosen duration.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="duration">Entry lifetime; null uses default expiration.</param>
    /// <param name="extraTags">Optional tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null);

    /// <summary>Reads or stores a cached value (synchronous). Factory returns (value, tags).</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value and tags on miss.</param>
    /// <param name="extraTags">Optional extra tags merged with factory tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, (TValue? value, string[]? tags)> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Reads or stores a cached value (synchronous) with a type-specific TTL.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="factory">Produces the value on miss.</param>
    /// <param name="type">Type used to resolve expiration.</param>
    /// <param name="extraTags">Optional tags.</param>
    TValue? GetOrSet<TValue>(string key, Func<CancellationToken, TValue?> factory, Type type, IEnumerable<string>? extraTags = null);

    /// <summary>
    /// Reads or stores a cached value (sync). <paramref name="setupAction" /> stamps duration and
    /// <see cref="ICacheEntryOptions.ExpirationMode" /> on miss. Hits of sliding entries reset TTL from the stored policy.
    /// </summary>
    TValue? GetOrSet<TValue>(
        string key,
        Func<CancellationToken, TValue?> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null);

    /// <summary>Returns the cached value when the key exists; otherwise stores <paramref name="value" /> with optional <paramref name="setupAction" /> and tags.</summary>
    /// <typeparam name="TValue">Cached value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="value">Value to store on miss.</param>
    /// <param name="setupAction">Optional mutator for entry options (for example duration).</param>
    /// <param name="tags">Optional tags.</param>
    /// <returns>The existing or newly stored value.</returns>
    TValue? GetOrSet<TValue>(string key, TValue value, Action<ICacheEntryOptions>? setupAction = null, IEnumerable<string>? tags = null);

    /// <summary>Asynchronous variant of <see cref="GetOrSet{TValue}(string, TValue, Action{ICacheEntryOptions}?, IEnumerable{string}?)" />.</summary>
    ValueTask<TValue?> GetOrSetAsync<TValue>(
        string key,
        TValue value,
        Action<ICacheEntryOptions>? setupAction = null,
        IEnumerable<string>? tags = null,
        CancellationToken token = default);

    /// <summary>Always writes a cache entry; uses the default TTL unless the implementation overrides it.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="key">Cache key.</param>
    /// <param name="obj">Value to store.</param>
    /// <param name="tags">Optional tags.</param>
    void Set<T>(string key, T obj, IEnumerable<string>? tags = null);

    /// <summary>Always writes a cache entry with an absolute <paramref name="duration" />.</summary>
    void Set<T>(string key, T obj, TimeSpan duration, IEnumerable<string>? tags = null);

    /// <summary>Always writes a cache entry; <paramref name="setupAction" /> sets duration and expiration mode.</summary>
    void Set<T>(string key, T obj, Action<ICacheEntryOptions> setupAction, IEnumerable<string>? tags = null);

    /// <summary>Reads a cached value without running a factory. Returns false when the key is missing, expired, or the cache is disabled.</summary>
    bool TryGetValue<T>(string key, out T? value);

    /// <summary>Reads or stores a framed byte payload (see <see cref="CacheOptions.Payload" />). Requires <see cref="ICachePayloadCodec" /> registration.</summary>
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

    /// <summary>Reads or stores a framed byte payload with a custom duration.</summary>
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

    /// <summary>Reads or stores a framed byte payload; factory returns plaintext bytes and tags (merged with <paramref name="extraTags" />).</summary>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<(byte[]? plaintext, string[]? tags)>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Reads or stores a framed byte payload with a custom duration; factory returns plaintext bytes and tags.</summary>
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

    /// <summary>Reads or stores a framed byte payload (synchronous).</summary>
    CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, IEnumerable<string>? extraTags = null);

    /// <summary>Reads or stores a framed byte payload with a custom duration (synchronous).</summary>
    CacheEntryEnvelope? GetOrSetPayload(string key, Func<CancellationToken, byte[]?> factory, TimeSpan? duration, IEnumerable<string>? extraTags = null);

    /// <summary>
    /// Reads or stores a framed byte payload. <paramref name="setupAction" /> stamps duration and expiration mode on miss.
    /// Hits of sliding entries reset TTL from the stored policy.
    /// </summary>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Synchronous variant of the payload factory overload that accepts <see cref="ICacheEntryOptions" /> setup.</summary>
    CacheEntryEnvelope? GetOrSetPayload(
        string key,
        Func<CancellationToken, byte[]?> factory,
        Action<ICacheEntryOptions> setupAction,
        IEnumerable<string>? extraTags = null);

    /// <summary>Stores plaintext bytes using the payload codec (compress/encrypt per options).</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="plaintext">Application plaintext before framing.</param>
    /// <param name="tags">Optional tags.</param>
    void SetPayload(string key, ReadOnlySpan<byte> plaintext, IEnumerable<string>? tags = null);

    /// <summary>Stores plaintext bytes with an absolute <paramref name="duration" />.</summary>
    void SetPayload(string key, ReadOnlySpan<byte> plaintext, TimeSpan duration, IEnumerable<string>? tags = null);

    /// <summary>Stores plaintext bytes using <paramref name="setupAction" /> for duration and expiration mode.</summary>
    void SetPayload(string key, ReadOnlySpan<byte> plaintext, Action<ICacheEntryOptions> setupAction, IEnumerable<string>? tags = null);

    /// <summary>Tries to read and decode a framed payload. Returns false when missing, disabled, or decode fails.</summary>
    /// <param name="key">Cache key.</param>
    /// <param name="envelope">Decoded plaintext envelope when the method returns true.</param>
    bool TryGetPayload(string key, out CacheEntryEnvelope? envelope);
}