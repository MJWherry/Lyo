using Lyo.Cache;
using Lyo.Exceptions;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Invalidates list-query and GET cache entries that use <see cref="QueryCacheTagBuilder" /> tags.</summary>
public static class QueryCacheInvalidation
{
    /// <summary>
    /// Use after creates or when new rows are not represented in existing <c>entity:&lt;type&gt;:&lt;id&gt;</c> tags.
    /// Removes all cached list queries and type-scoped GET entries via tag <c>entity:&lt;type&gt;</c>.
    /// </summary>
    public static Task InvalidateQueryCachesForBroadEntityTypeAsync<TDb>(ICacheService cache, CancellationToken ct = default)
        where TDb : class
    {
        _ = ct;
        return cache.InvalidateQueryCacheAsync<TDb>();
    }

    /// <summary>
    /// Invalidates cache entries tagged for the given primary keys. When the distinct key count exceeds
    /// <see cref="CacheOptions.MaxBulkQueryInvalidationByIdCount"/>, falls back to <see cref="QueryCacheTagBuilder.EntityTypeTag" />.
    /// </summary>
    public static async Task InvalidateQueryCachesForEntityKeysAsync(
        ICacheService cache,
        CacheOptions cacheOptions,
        Type entityClrType,
        IEnumerable<IReadOnlyList<object?>> affectedPrimaryKeys,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(cache, nameof(cache));
        ArgumentHelpers.ThrowIfNull(cacheOptions, nameof(cacheOptions));
        ArgumentHelpers.ThrowIfNull(entityClrType, nameof(entityClrType));
        ArgumentHelpers.ThrowIfNull(affectedPrimaryKeys, nameof(affectedPrimaryKeys));

        var distinctInstanceTags = new HashSet<string>();
        foreach (var keys in affectedPrimaryKeys) {
            if (keys is not { Count: > 0 })
                continue;
            distinctInstanceTags.Add(QueryCacheTagBuilder.EntityInstanceTag(entityClrType, keys));
        }

        if (distinctInstanceTags.Count == 0)
            return;

        if (distinctInstanceTags.Count > cacheOptions.MaxBulkQueryInvalidationByIdCount) {
            await cache.InvalidateCacheItemByTag(QueryCacheTagBuilder.EntityTypeTag(entityClrType)).ConfigureAwait(false);
            return;
        }

        foreach (var tag in distinctInstanceTags)
            await cache.InvalidateCacheItemByTag(tag).ConfigureAwait(false);
    }
}
