namespace Lyo.Query.Services.WhereClause;

/// <summary>
/// Key prefixes for reflection metadata memoized by <see cref="SharedEntityMetadataCache" />.
/// </summary>
/// <remarks>
/// Duplicated from <c>Lyo.Cache.Constants.EntityMetadata</c> on purpose: evaluation must not depend on the cache stack. The strings stay byte-identical so keys remain
/// stable for hosts that project them into a shared cache.
/// </remarks>
internal static class MetadataCacheKeys
{
    /// <summary>Cache-key prefix for resolved <see cref="System.Reflection.PropertyInfo" /> per type and name.</summary>
    public const string PropertyPrefix = "entity_property:";

    /// <summary>Cache-key prefix for parsed dotted property paths.</summary>
    public const string PropertyPathPrefix = "entity_property_path:";

    /// <summary>Cache-key prefix for normalized projection field paths.</summary>
    public const string NormalizedPathPrefix = "entity_normalized_path:";

    /// <summary>Cache-key prefix for comparison-strategy metadata per operator and CLR type.</summary>
    public const string ComparisonMetadataPrefix = "comparison_metadata:";

    /// <summary>Cache-key prefix for reflected <see cref="System.Reflection.MethodInfo" /> instances (OrderBy, Contains, etc.).</summary>
    public const string ReflectedMethodPrefix = "reflected_method:";

    /// <summary>Cache-key prefix for compiled Queryable order-method closures.</summary>
    public const string OrderMethodPrefix = "order_method:";

    /// <summary>Cache-key prefix for sort-key selector lambdas per entity type and path.</summary>
    public const string SortKeyLambdaPrefix = "sort_key_lambda:";
}
