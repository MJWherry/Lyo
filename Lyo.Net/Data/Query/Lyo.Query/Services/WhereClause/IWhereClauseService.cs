using System.Linq.Expressions;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common;

namespace Lyo.Query.Services.WhereClause;

/// <summary>
/// Applies <see cref="Lyo.Query.Models.Common.WhereClause" /> trees to <see cref="IQueryable{T}" /> and in-memory entities: filter expressions, sorting, ordering, optional
/// match explanation, and validation of dotted property paths.
/// </summary>
/// <remarks>
/// Implementations typically translate clauses to expression trees for EF Core and compile matchers for <see cref="MatchesWhereClause{TEntity}" />. Sub-clauses support
/// two-phase filtering (e.g. SQL for the root predicate, in-memory for <see cref="Lyo.Query.Models.Common.WhereClause.SubClause" />).
/// </remarks>
public interface IWhereClauseService
{
    /// <summary>Translates <paramref name="whereClause" /> into a predicate and applies it to <paramref name="source" />.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="source">The queryable to filter.</param>
    /// <param name="whereClause">The filter tree, or <c>null</c> to return <paramref name="source" /> unchanged.</param>
    /// <param name="includeSubClauses">
    /// When <c>false</c>, nested <see cref="Lyo.Query.Models.Common.WhereClause.SubClause" /> nodes are omitted from the expression (e.g. root-only database phase). When
    /// <c>true</c> (default), the full tree is translated.
    /// </param>
    /// <returns><paramref name="source" /> with <c>Where</c> applied when <paramref name="whereClause" /> is non-null; otherwise <paramref name="source" />.</returns>
    /// <exception cref="Lyo.Query.Models.Exceptions.InvalidQueryException">Thrown when the clause references invalid paths or unsupported operators for the entity type.</exception>
    IQueryable<TEntity> ApplyWhereClause<TEntity>(IQueryable<TEntity> source, Models.Common.WhereClause? whereClause, bool includeSubClauses = true);

    /// <summary>Orders <paramref name="source" /> by a single dotted property path.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="source">The queryable to order.</param>
    /// <param name="propertyName">Dotted path to a scalar or collection (ordering uses count for collections). Must not be null or empty.</param>
    /// <param name="direction">Sort direction; default is <see cref="SortDirection.Desc" /> when null.</param>
    /// <returns>The ordered queryable.</returns>
    /// <exception cref="Lyo.Query.Models.Exceptions.InvalidQueryException">Thrown when <paramref name="propertyName" /> is invalid for <typeparamref name="TEntity" />.</exception>
    IQueryable<TEntity> SortByProperty<TEntity>(IQueryable<TEntity> source, string propertyName, SortDirection? direction = null);

    /// <summary>
    /// Applies multiple <see cref="SortBy" /> entries in priority order, or uses <paramref name="defaultOrder" /> when no explicit sorts are provided.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="queryable">The queryable to order.</param>
    /// <param name="sortByProps">Zero or more <see cref="SortBy" /> entries. Order within the list is overridden by each item's <see cref="SortBy.Priority" /> when set.</param>
    /// <param name="defaultOrder">Fallback key selector when <paramref name="sortByProps" /> is empty.</param>
    /// <param name="defaultSortDirection">Direction for <paramref name="defaultOrder" /> when <paramref name="sortByProps" /> is empty.</param>
    /// <returns>The ordered queryable.</returns>
    IQueryable<TEntity> ApplyOrdering<TEntity>(
        IQueryable<TEntity> queryable,
        IEnumerable<SortBy> sortByProps,
        Expression<Func<TEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection);

    /// <summary>Evaluates whether <paramref name="entity" /> satisfies <paramref name="whereClause" /> in memory (compiled expression).</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity instance. If null, the result is <c>false</c> when <paramref name="whereClause" /> is non-null.</param>
    /// <param name="whereClause">The filter tree, or <c>null</c> for a vacuous match (returns <c>true</c> for non-null entity in typical implementations).</param>
    /// <returns><c>true</c> if the entity matches the clause; otherwise <c>false</c>.</returns>
    bool MatchesWhereClause<TEntity>(TEntity entity, Models.Common.WhereClause? whereClause);

    /// <summary>
    /// Explains whether an entity matches a where clause and records pass/fail per AST node (field predicates, groups,
    /// <see cref="Lyo.Query.Models.Common.WhereClause.SubClause" /> chains).
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotImplementedException" />: in-memory evaluation against a loaded entity is supported by <see cref="BaseWhereClauseService" />;
    /// where clauses translated to SQL (e.g. PostgreSQL) are not explained in-process.
    /// </remarks>
    WhereClauseExplainResult ExplainMatch<TEntity>(TEntity entity, Models.Common.WhereClause? whereClause)
        => throw new NotImplementedException(
            "ExplainMatch is only supported for in-memory evaluation against entity instances. " + "Where clauses executed as SQL (e.g. PostgreSQL) do not implement explanation.");

    /// <summary>
    /// Returns EF (or ORM) include paths for collection segments referenced in <paramref name="whereClause" /> (for example <c>DocketCharges</c> for <c>DocketCharges.Code</c>),
    /// so navigations are loaded before in-memory sub-clause evaluation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="whereClause">The filter tree, or <c>null</c> (returns an empty sequence).</param>
    /// <returns>
    /// Distinct include path strings (e.g. navigation names) needed for fields that traverse collections, ordered case-insensitively.
    /// </returns>
    IEnumerable<string> GetCollectionIncludePathsForWhereClause<TEntity>(Models.Common.WhereClause? whereClause);

    /// <summary>Whether <paramref name="propertyName" /> is a valid dotted path for <see cref="SortBy" /> and where-clause fields on <typeparamref name="TEntity" />.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="propertyName">The dotted property path to validate.</param>
    /// <param name="errorMessage">When the method returns <c>false</c>, a human-readable reason; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if the path resolves on <typeparamref name="TEntity" />; otherwise <c>false</c>.</returns>
    bool TryValidatePropertyPath<TEntity>(string propertyName, out string? errorMessage);
}