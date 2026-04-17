using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Cache;
using Lyo.Exceptions;
using Lyo.Query.Models.Common.Request;

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
            // Direct key removal: canonical GET entries (same base key as instance tag, plus optional :raw).
            await cache.InvalidateCacheItem(QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(entityClrType, keys, includes: null, rawResponse: false)).ConfigureAwait(false);
            await cache.InvalidateCacheItem(QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(entityClrType, keys, includes: null, rawResponse: true)).ConfigureAwait(false);
        }

        // QueryProject rows are not always dictionary-shaped (e.g. single Select to a collection leaf), so cascade instance tags may be missing.
        // Those entries still carry QueryProjectReferencedEntityTag; bust them without invalidating EntityTypeTag (would clear unrelated list queries).
        await cache.InvalidateCacheItemByTag(QueryCacheTagBuilder.QueryProjectReferencedEntityTag(entityClrType)).ConfigureAwait(false);
    }

    /// <summary>
    /// Invalidates cached SQL-projected query pages that share the same projection shape tag
    /// (<see cref="QueryCacheTagBuilder.FormatProjShapeTag" />). Use when a schema or convention change affects
    /// all grids with a given select/computed/zip fingerprint without touching entity instance tags.
    /// </summary>
    public static Task InvalidateProjectedQueriesByProjShapeAsync(
        ICacheService cache,
        IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs,
        IReadOnlyList<ComputedField> computedFields,
        bool zipSiblingCollectionSelections,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(cache, nameof(cache));
        ArgumentHelpers.ThrowIfNull(projectedFieldSpecs, nameof(projectedFieldSpecs));
        ArgumentHelpers.ThrowIfNull(computedFields, nameof(computedFields));
        _ = ct;
        var tag = QueryCacheTagBuilder.FormatProjShapeTag(projectedFieldSpecs, computedFields, zipSiblingCollectionSelections);
        return cache.InvalidateCacheItemByTag(tag);
    }
}
