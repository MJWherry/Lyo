using System.Linq.Expressions;
using Lyo.Cache;
using Lyo.Metrics;
using Lyo.Query.Services.ValueConversion;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Postgres;

/// <summary>
/// <see cref="BaseWhereClauseService" /> that rewrites Fuzzy predicates to Npgsql trigram methods so they run as SQL <c>similarity()</c>. In-memory <c>EnumerableQuery</c>
/// sources keep the CLR <see cref="FuzzyMatch.Similarity" /> body.
/// </summary>
public sealed class PostgresWhereClauseService(ICacheService cache, CacheOptions cacheOptions, IValueConversionService valueConversion, IMetrics? metrics = null)
    : BaseWhereClauseService(cache, cacheOptions, valueConversion, metrics)
{
    /// <inheritdoc />
    protected override Expression<Func<TEntity, bool>> RewriteQueryablePredicate<TEntity>(Expression<Func<TEntity, bool>> expression, IQueryProvider provider)
    {
        if (provider is EnumerableQuery)
            return expression;

        return (Expression<Func<TEntity, bool>>)FuzzyToNpgsqlRewriter.Instance.Visit(expression);
    }
}