namespace Lyo.Cache;

/// <summary>Shared tags, key prefixes, and metric names for the cache library.</summary>
public static class Constants
{
    /// <summary>
    /// Canonical entity-scoped cache tag. Producers (API tag builders) and consumers (<see cref="ICacheService.InvalidateQueryCacheAsync{TDb}" />) must
    /// agree byte-for-byte or invalidation silently stops working, so both sides resolve here rather than formatting the string themselves.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// Tag covering every cached entry scoped to one entity CLR type, for example <c>entity:acme.catalog.widget</c>. Built from <see cref="System.Type.FullName" />, not
        /// <see cref="System.Type.Name" />: two entities both named <c>Person</c> in different namespaces would otherwise share a tag, so writing either invalidated both.
        /// </summary>
        /// <param name="entityClrType">Entity CLR type.</param>
        public static string EntityType(Type entityClrType) => $"entity:{(entityClrType.FullName ?? entityClrType.Name).ToLowerInvariant()}";
    }

    /// <summary>Key prefixes for entity metadata shared by query and projection services.</summary>
    public static class EntityMetadata
    {
        /// <summary>Prefix for resolved <see cref="System.Reflection.PropertyInfo" /> per type and name.</summary>
        public const string PropertyPrefix = "entity_property:";

        /// <summary>Prefix for parsed dotted property paths.</summary>
        public const string PropertyPathPrefix = "entity_property_path:";

        /// <summary>Prefix for normalized projection field paths.</summary>
        public const string NormalizedPathPrefix = "entity_normalized_path:";

        /// <summary>Reserved prefix for collection element-type lookups (legacy/auxiliary).</summary>
        public const string CollectionElementPrefix = "entity_collection_element:";

        /// <summary>Prefix for comparison-strategy metadata per operator and CLR type.</summary>
        public const string ComparisonMetadataPrefix = "comparison_metadata:";

        /// <summary>Prefix for reflected <see cref="System.Reflection.MethodInfo" /> instances (OrderBy, Contains, and so on).</summary>
        public const string ReflectedMethodPrefix = "reflected_method:";

        /// <summary>Prefix for collection-vs-scalar adjustment metadata.</summary>
        public const string CollectionAdjustmentPrefix = "collection_adjustment:";

        /// <summary>Prefix for compiled Queryable order-method closures.</summary>
        public const string OrderMethodPrefix = "order_method:";

        /// <summary>Prefix for sort-key selector lambdas per entity type and path.</summary>
        public const string SortKeyLambdaPrefix = "sort_key_lambda:";
    }

    /// <summary>Metric names and tag keys used by the cache service.</summary>
    public static class Metrics
    {
        /// <summary>Timer covering successful cache reads (hits).</summary>
        public const string HitDuration = "cache.hit.duration";

        /// <summary>Hit counter.</summary>
        public const string HitSuccess = "cache.hit.success";

        /// <summary>Timer covering the miss path (factory plus store).</summary>
        public const string MissDuration = "cache.miss.duration";

        /// <summary>Counter incremented after a miss is populated.</summary>
        public const string MissSuccess = "cache.miss.success";

        /// <summary>Timer covering explicit <c>Set</c> calls.</summary>
        public const string SetDuration = "cache.set.duration";

        /// <summary>Successful-set counter.</summary>
        public const string SetSuccess = "cache.set.success";

        /// <summary>Timer covering single-key removal.</summary>
        public const string RemoveDuration = "cache.remove.duration";

        /// <summary>Successful single-key removal counter.</summary>
        public const string RemoveSuccess = "cache.remove.success";

        /// <summary>Timer covering tag-based mass removal.</summary>
        public const string RemoveByTagDuration = "cache.remove_by_tag.duration";

        /// <summary>Tag-invalidation call counter.</summary>
        public const string RemoveByTagSuccess = "cache.remove_by_tag.success";

        /// <summary>Approximate entries removed by the last tag invalidation.</summary>
        public const string RemoveByTagItemsRemoved = "cache.remove_by_tag.items_removed";

        /// <summary>Entry-count gauge (implementation-defined).</summary>
        public const string CacheSize = "cache.size";

        /// <summary>Bulk-clear counter (when implemented).</summary>
        public const string ClearSuccess = "cache.clear.success";

        /// <summary>Shared tag keys for cache metrics.</summary>
        public static class Tags
        {
            public const string Key = "key";

            public const string Tag = "tag";

            public const string Operation = "operation";
        }
    }
}