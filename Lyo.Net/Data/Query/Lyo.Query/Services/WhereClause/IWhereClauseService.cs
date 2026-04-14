using System.Linq.Expressions;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common;

namespace Lyo.Query.Services.WhereClause;

public interface IWhereClauseService
{
    /// <param name="includeSubClauses">When false, excludes SubQuery from the expression (used for root-only DB phase). Default true.</param>
    IQueryable<TEntity> ApplyWhereClause<TEntity>(IQueryable<TEntity> source, Models.Common.WhereClause? whereClause, bool includeSubClauses = true);

    IQueryable<TEntity> SortByProperty<TEntity>(IQueryable<TEntity> source, string propertyName, SortDirection? direction = null);

    IQueryable<TEntity> ApplyOrdering<TEntity>(
        IQueryable<TEntity> queryable,
        IEnumerable<SortBy> sortByProps,
        Expression<Func<TEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection);

    bool MatchesWhereClause<TEntity>(TEntity entity, Models.Common.WhereClause? whereClause);

    /// <summary>
    /// Explains whether an entity matches a where clause and records pass/fail per AST node (field predicates, groups, <see cref="Lyo.Query.Models.Common.WhereClause.SubClause"/> chains).
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException"/>: in-memory evaluation against a loaded entity is supported by
    /// <see cref="BaseWhereClauseService"/>; where clauses translated to SQL (e.g. PostgreSQL) are not explained in-process.
    /// </remarks>
    WhereClauseExplainResult ExplainMatch<TEntity>(TEntity entity, Models.Common.WhereClause? whereClause) =>
        throw new NotImplementedException(
            "ExplainMatch is only supported for in-memory evaluation against entity instances. "
            + "Where clauses executed as SQL (e.g. PostgreSQL) do not implement explanation.");

    /// <summary>
    /// Returns collection include paths referenced in the query node (e.g. "DocketCharges" for "DocketCharges.Code"). Used to load navigations before in-memory SubQuery
    /// filtering.
    /// </summary>
    IEnumerable<string> GetCollectionIncludePathsForWhereClause<TEntity>(Models.Common.WhereClause? whereClause);

    /// <summary>Whether <paramref name="propertyName" /> is a valid dotted path for SortBy and Where clause fields on <typeparamref name="TEntity" />.</summary>
    bool TryValidatePropertyPath<TEntity>(string propertyName, out string? errorMessage);
}