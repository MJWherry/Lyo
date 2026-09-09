using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Cache;
using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Invalidates list-query and GET cache entries that carry <see cref="QueryCacheTagBuilder" /> tags.</summary>
public static class QueryCacheInvalidation
{
    /// <summary>
    /// Use after creates or when new rows are not represented in existing <c>entity:&lt;type&gt;:&lt;id&gt;</c> tags. Removes every cached list query and type-scoped GET entry
    /// via tag <c>entity:&lt;type&gt;</c>.
    /// </summary>
    public static Task InvalidateQueryCachesForBroadEntityTypeAsync<TDb>(ICacheService cache, CancellationToken ct = default, ILogger? logger = null)
        where TDb : class
    {
        _ = ct;
        return SafeAsync(() => cache.InvalidateQueryCacheAsync<TDb>(), logger, $"broad type sweep for {typeof(TDb).FullName}");
    }

    /// <summary>
    /// Invalidates the broad type caches for a child entity and its parent together, and accepts a null <paramref name="cache" />.
    /// </summary>
    /// <remarks>
    /// Definition grids commonly project a child column (a job's last run, a report's last generation) onto the parent row without carrying the child's
    /// <c>entity:&lt;child&gt;</c> tag, so writing a child has to clear the parent's cached queries as well.
    /// </remarks>
    /// <typeparam name="TChild">The entity being written.</typeparam>
    /// <typeparam name="TParent">The entity whose projected queries include <typeparamref name="TChild" /> columns.</typeparam>
    /// <param name="cache">The cache. Null is a no-op, so callers with optional caching need no guard.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="logger">Receives invalidation failures, which are logged rather than thrown.</param>
    public static Task InvalidateQueryCachesForBroadEntityTypeAsync<TChild, TParent>(ICacheService? cache, CancellationToken ct = default, ILogger? logger = null)
        where TChild : class where TParent : class
        => cache is null
            ? Task.CompletedTask
            : Task.WhenAll(
                InvalidateQueryCachesForBroadEntityTypeAsync<TChild>(cache, ct, logger), InvalidateQueryCachesForBroadEntityTypeAsync<TParent>(cache, ct, logger));

    /// <summary>
    /// Invalidates cache entries tagged for the given primary keys. When <see cref="CacheOptions.QueryCacheTagGranularity" /> is <see cref="QueryCacheTagGranularity.Broad" />,
    /// only <see cref="QueryCacheTagBuilder.EntityTypeTag" /> is invalidated (no per-id work). When granular, when the distinct key count exceeds
    /// <see cref="CacheOptions.MaxBulkQueryInvalidationByIdCount" />, falls back to <see cref="QueryCacheTagBuilder.EntityTypeTag" />.
    /// </summary>
    public static Task InvalidateQueryCachesForEntityKeysAsync(
        ICacheService cache,
        CacheOptions cacheOptions,
        Type entityClrType,
        IEnumerable<IReadOnlyList<object?>> affectedPrimaryKeys,
        CancellationToken ct = default,
        ILogger? logger = null)
    {
        ArgumentHelpers.ThrowIfNull(cache);
        ArgumentHelpers.ThrowIfNull(cacheOptions);
        ArgumentHelpers.ThrowIfNull(entityClrType);
        ArgumentHelpers.ThrowIfNull(affectedPrimaryKeys);
        return SafeAsync(
            () => InvalidateQueryCachesForEntityKeysCoreAsync(cache, cacheOptions, entityClrType, affectedPrimaryKeys), logger,
            $"key invalidation for {entityClrType.FullName}");
    }

    private static async Task InvalidateQueryCachesForEntityKeysCoreAsync(
        ICacheService cache,
        CacheOptions cacheOptions,
        Type entityClrType,
        IEnumerable<IReadOnlyList<object?>> affectedPrimaryKeys)
    {
        if (cacheOptions.QueryCacheTagGranularity == QueryCacheTagGranularity.Broad) {
            await cache.InvalidateCacheItemByTag(QueryCacheTagBuilder.EntityTypeTag(entityClrType)).ConfigureAwait(false);
            return;
        }

        var distinctKeySets = new List<IReadOnlyList<object?>>();
        var seenTags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var keys in affectedPrimaryKeys) {
            if (keys is not { Count: > 0 })
                continue;

            var tag = QueryCacheTagBuilder.EntityInstanceTag(entityClrType, keys);
            if (seenTags.Add(tag))
                distinctKeySets.Add(keys);
        }

        if (distinctKeySets.Count == 0)
            return;

        if (distinctKeySets.Count > cacheOptions.MaxBulkQueryInvalidationByIdCount) {
            await cache.InvalidateCacheItemByTag(QueryCacheTagBuilder.EntityTypeTag(entityClrType)).ConfigureAwait(false);
            return;
        }

        foreach (var keys in distinctKeySets) {
            await cache.InvalidateCacheItemByTag(QueryCacheTagBuilder.EntityInstanceTag(entityClrType, keys)).ConfigureAwait(false);
            // Direct key removal: canonical GET entries (same base key as the instance tag, plus optional :raw).
            await cache.InvalidateCacheItem(QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(entityClrType, keys)).ConfigureAwait(false);
            await cache.InvalidateCacheItem(QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(entityClrType, keys, null, true)).ConfigureAwait(false);
        }

        // QueryProject rows are not always dictionary-shaped (for example a single Select to a collection leaf), so cascade instance tags may be missing.
        // Those entries still carry QueryProjectReferencedEntityTag. Bust them without invalidating EntityTypeTag (that would clear unrelated list queries).
        await cache.InvalidateCacheItemByTag(QueryCacheTagBuilder.QueryProjectReferencedEntityTag(entityClrType)).ConfigureAwait(false);
    }

    /// <summary>
    /// Invalidates cached SQL-projected query pages that share the same projection shape tag (<see cref="QueryCacheTagBuilder.FormatProjShapeTag" />). Use when a schema or
    /// convention change affects every grid with a given select/computed/zip fingerprint without touching entity instance tags.
    /// </summary>
    public static Task InvalidateProjectedQueriesByProjShapeAsync(
        ICacheService cache,
        IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs,
        IReadOnlyList<ComputedField> computedFields,
        bool zipSiblingCollectionSelections,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(cache);
        ArgumentHelpers.ThrowIfNull(projectedFieldSpecs);
        ArgumentHelpers.ThrowIfNull(computedFields);
        _ = ct;
        var tag = QueryCacheTagBuilder.FormatProjShapeTag(projectedFieldSpecs, computedFields, zipSiblingCollectionSelections);
        return cache.InvalidateCacheItemByTag(tag);
    }

    /// <summary>
    /// Runs an invalidation and logs, rather than rethrows, anything that throw produces.
    /// </summary>
    /// <remarks>
    /// Invalidation happens after the write has already committed. Letting a cache or backplane outage propagate turned a successful write into a 500, so the caller retried and
    /// wrote again. A stale cache entry is the lesser failure, and it expires on its own.
    /// </remarks>
    private static async Task SafeAsync(Func<Task> invalidate, ILogger? logger, string description)
    {
        try {
            await invalidate().ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger?.LogError(ex, "Cache invalidation failed ({Description}); the write succeeded and stale entries will expire on their own.", description);
        }
    }
}