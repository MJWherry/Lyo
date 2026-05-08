namespace Lyo.Cache;

/// <summary>Consolidated constants for the Cache library.</summary>
public static class Constants
{
    /// <summary>Cache key prefixes for entity metadata shared by query and projection services.</summary>
    public static class EntityMetadata
    {
        /// <summary>Cache key prefix for resolved <see cref="System.Reflection.PropertyInfo" /> per type and name.</summary>
        public const string PropertyPrefix = "entity_property:";

        /// <summary>Cache key prefix for parsed dotted property paths.</summary>
        public const string PropertyPathPrefix = "entity_property_path:";

        /// <summary>Cache key prefix for normalized projection field paths.</summary>
        public const string NormalizedPathPrefix = "entity_normalized_path:";

        /// <summary>Reserved prefix for collection element type lookups (legacy/auxiliary).</summary>
        public const string CollectionElementPrefix = "entity_collection_element:";

        /// <summary>Cache key prefix for comparison strategy metadata per operator and CLR type.</summary>
        public const string ComparisonMetadataPrefix = "comparison_metadata:";

        /// <summary>Cache key prefix for reflected <see cref="System.Reflection.MethodInfo" /> instances (OrderBy, Contains, etc.).</summary>
        public const string ReflectedMethodPrefix = "reflected_method:";

        /// <summary>Cache key prefix for collection-vs-scalar adjustment metadata.</summary>
        public const string CollectionAdjustmentPrefix = "collection_adjustment:";

        /// <summary>Cache key prefix for compiled Queryable order method closures.</summary>
        public const string OrderMethodPrefix = "order_method:";

        /// <summary>Cache key prefix for sort key selector lambdas per entity type and path.</summary>
        public const string SortKeyLambdaPrefix = "sort_key_lambda:";
    }

    /// <summary>Constants for cache service metric names and tags.</summary>
    public static class Metrics
    {
        /// <summary>Timer for successful cache reads (hits).</summary>
        public const string HitDuration = "cache.hit.duration";

        /// <summary>Counter for cache hits.</summary>
        public const string HitSuccess = "cache.hit.success";

        /// <summary>Timer covering miss path including factory execution and store.</summary>
        public const string MissDuration = "cache.miss.duration";

        /// <summary>Counter incremented when a miss is populated successfully.</summary>
        public const string MissSuccess = "cache.miss.success";

        /// <summary>Timer for explicit <c>Set</c> operations.</summary>
        public const string SetDuration = "cache.set.duration";

        /// <summary>Counter for successful sets.</summary>
        public const string SetSuccess = "cache.set.success";

        /// <summary>Timer for single-key removal.</summary>
        public const string RemoveDuration = "cache.remove.duration";

        /// <summary>Counter for successful single-key removals.</summary>
        public const string RemoveSuccess = "cache.remove.success";

        /// <summary>Timer for tag-based mass removal.</summary>
        public const string RemoveByTagDuration = "cache.remove_by_tag.duration";

        /// <summary>Counter for tag invalidation calls.</summary>
        public const string RemoveByTagSuccess = "cache.remove_by_tag.success";

        /// <summary>Approximate number of entries removed in the last tag invalidation.</summary>
        public const string RemoveByTagItemsRemoved = "cache.remove_by_tag.items_removed";

        /// <summary>Entry count gauge (implementation-defined).</summary>
        public const string CacheSize = "cache.size";

        /// <summary>Counter for bulk clear operations (if implemented).</summary>
        public const string ClearSuccess = "cache.clear.success";

        /// <summary>Common tag keys for cache metrics.</summary>
        public static class Tags
        {
            public const string Key = "key";

            public const string Tag = "tag";

            public const string Operation = "operation";
        }
    }
}