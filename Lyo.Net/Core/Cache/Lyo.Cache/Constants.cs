namespace Lyo.Cache;

/// <summary>Consolidated constants for the Cache library.</summary>
public static class Constants
{
    /// <summary>Cache key prefixes for entity metadata shared by query and projection services.</summary>
    public static class EntityMetadata
    {
        public const string PropertyPrefix = "entity_property:";
        public const string PropertyPathPrefix = "entity_property_path:";
        public const string NormalizedPathPrefix = "entity_normalized_path:";
        public const string CollectionElementPrefix = "entity_collection_element:";
        public const string ComparisonMetadataPrefix = "comparison_metadata:";
        public const string ReflectedMethodPrefix = "reflected_method:";
        public const string CollectionAdjustmentPrefix = "collection_adjustment:";
        public const string OrderMethodPrefix = "order_method:";
    }

    /// <summary>Constants for cache service metric names and tags.</summary>
    public static class Metrics
    {
        public const string HitDuration = "cache.hit.duration";

        public const string HitSuccess = "cache.hit.success";

        public const string MissDuration = "cache.miss.duration";

        public const string MissSuccess = "cache.miss.success";

        public const string SetDuration = "cache.set.duration";

        public const string SetSuccess = "cache.set.success";

        public const string RemoveDuration = "cache.remove.duration";

        public const string RemoveSuccess = "cache.remove.success";

        public const string RemoveByTagDuration = "cache.remove_by_tag.duration";

        public const string RemoveByTagSuccess = "cache.remove_by_tag.success";

        public const string RemoveByTagItemsRemoved = "cache.remove_by_tag.items_removed";

        public const string CacheSize = "cache.size";

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