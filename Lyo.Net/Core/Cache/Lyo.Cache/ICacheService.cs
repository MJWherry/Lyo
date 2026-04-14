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

    /// <summary>Gets or sets framed byte payload (see <see cref="CacheOptions.Payload"/>). Requires <see cref="ICachePayloadCodec"/> registration.</summary>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets framed byte payload with a custom duration.</summary>
    ValueTask<CacheEntryEnvelope?> GetOrSetPayloadAsync(
        string key,
        Func<CancellationToken, Task<byte[]?>> factory,
        TimeSpan? duration,
        IEnumerable<string>? extraTags = null,
        CancellationToken token = default);

    /// <summary>Gets or sets framed byte payload; factory returns plaintext bytes and tags (merged with <paramref name="extraTags"/>).</summary>
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

    /// <summary>
    /// Serializes <typeparamref name="TValue"/> with <see cref="ICachePayloadSerializer"/>, frames with <see cref="ICachePayloadCodec"/>, and returns the deserialized value.
    /// </summary>
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

    /// <summary>Like the byte-tuple payload overload, but serializes <typeparamref name="TValue"/> and merges tags.</summary>
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
    void SetPayload(string key, ReadOnlySpan<byte> plaintext, IEnumerable<string>? tags = null);

    /// <summary>Tries to read and decode a framed payload. Returns false when missing, disabled, or decode fails.</summary>
    bool TryGetPayload(string key, out CacheEntryEnvelope? envelope);
}