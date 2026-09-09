using Lyo.Cache;
using Lyo.Metrics;
using Lyo.Query.Services.ValueConversion;

namespace Lyo.Query.Services.WhereClause;

/// <summary>
/// Hosted <see cref="IWhereClauseService" />. Wraps the <see cref="WhereClauseEvaluator" /> expression engine and stores include paths derived from clause shape in
/// <see cref="ICacheService" />.
/// </summary>
/// <remarks>
/// Register through <c>AddLyoQueryServices</c>, which needs an <see cref="ICacheService" /> and <see cref="CacheOptions" /> in the container (for example from
/// <c>AddFusionCache</c> or <c>AddLocalCache</c>). Call <see cref="WhereClauseEvaluator" /> directly when a shared cache is not wanted. Metrics under
/// <see cref="Lyo.Query.Constants.Metrics" /> live on the evaluator, so they work either way.
/// <para>
/// Compiled matchers and predicate trees stay on the base class's process-local memoization on purpose. A compiled delegate cannot be serialized. Sending it through a cache
/// that might talk to a distributed backplane would silently recompile on every call via the swallowed-exception fallback, with no signal.
/// </para>
/// </remarks>
public class BaseWhereClauseService : WhereClauseEvaluator
{
    /// <summary>
    /// Scope tag for entries that come only from clause shape. This is not an <c>entity:</c> tag. Those fire on every insert, update, and delete, and which navigations a
    /// clause walks does not depend on row data. Sharing that tag meant every write dropped these entries and the next query rebuilt them from scratch.
    /// </summary>
    public const string CompiledArtifactScopeTag = "querypredicate";

    /// <summary>Cache that stores include paths derived from clause shape.</summary>
    protected readonly ICacheService Cache;

    /// <summary>Expiration and feature flags that govern <see cref="Cache" /> entries.</summary>
    protected readonly CacheOptions CacheOptions;

    /// <summary>Builds a <see cref="BaseWhereClauseService" />.</summary>
    /// <param name="cache">Cache for include paths derived from clause shape.</param>
    /// <param name="cacheOptions">Expiration and feature flags, including type-specific expirations.</param>
    /// <param name="valueConversion">Converts filter literal values to property types.</param>
    /// <param name="metrics">
    /// Optional metrics. Recording runs only when <paramref name="cacheOptions" />.<see cref="CacheOptions.EnableMetrics" /> is on and this is non-null.
    /// </param>
    public BaseWhereClauseService(ICacheService cache, CacheOptions cacheOptions, IValueConversionService valueConversion, IMetrics? metrics = null)
        : base(valueConversion, cacheOptions.EnableMetrics ? metrics : null)
    {
        Cache = cache;
        CacheOptions = cacheOptions;
    }

    /// <summary>
    /// Include paths are plain strings, so they survive any cache backend. Tagged with <see cref="CompiledArtifactScopeTag" /> instead of the entity's data tags, because a
    /// row write cannot change which navigations a clause walks.
    /// </summary>
    protected override IReadOnlyList<string> GetIncludePaths<TEntity>(Models.Common.WhereClause queryNode)
        => Cache.GetOrSet<IReadOnlyList<string>>(
            GenerateIncludePathsCacheKey<TEntity>(queryNode),
            _ => ComputeIncludePaths<TEntity>(queryNode),
            CacheOptions.DefaultExpiration,
            [CompiledArtifactScopeTag]) ?? [];
}
