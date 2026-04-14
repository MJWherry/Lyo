using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Services.Crud.Read.Project;

/// <summary>Filter conditions extracted from a WhereClause for MatchedOnly mode, with the group operator (And/Or) that combines them.</summary>
public sealed record ProjectedFilterConditions(IReadOnlyList<ProjectedFilterCondition> Conditions, GroupOperatorEnum Operator = GroupOperatorEnum.And);

/// <summary>Resolves projection specs, builds projection expressions, and projects entities to selected fields.</summary>
public interface IProjectionService
{
    /// <summary>Resolves and normalizes requested field paths for an entity type.</summary>
    /// <returns>Normalized specs, or an empty spec list with <paramref name="PathErrors" /> when one or more paths fail normalization (all failures are collected).</returns>
    (IReadOnlyList<ProjectedFieldSpec> Specs, IReadOnlyList<ApiError> PathErrors) ResolveProjectedFields<TDbModel>(
        IEnumerable<string> requestedFields,
        bool allowSelectWildcards = true,
        QueryPathValidationCache? pathCache = null)
        where TDbModel : class;

    /// <summary>Normalizes a field path (e.g. "contactAddresses.address.street") for a root type.</summary>
    string NormalizeFieldPath(Type rootType, string path);

    /// <summary>Gets include paths derived from projected field specs (for loading navigation properties needed by Select).</summary>
    IEnumerable<string> GetDerivedIncludes(Type rootType, IReadOnlyList<ProjectedFieldSpec> specs);

    /// <summary>
    /// Builds an Expression for SQL-level projection. Sibling paths under the same collection share one navigation when possible (single join).
    /// Returns a null projection when any path has wildcard or cannot be resolved.
    /// </summary>
    /// <param name="projectionPathsAlreadyValidated">
    /// When <c>true</c>, skips an internal <see cref="CollectProjectionFieldIssues{TDbModel}" /> pass (callers such as <c>QueryProjectedCore</c> already validated specs).
    /// </param>
    SqlProjectionBuildResult<TDbModel> TryBuildSqlProjectionExpression<TDbModel>(IReadOnlyList<ProjectedFieldSpec> specs, bool projectionPathsAlreadyValidated = false)
        where TDbModel : class;

    /// <summary>Non-empty when any projected path is empty, wildcard-only, uses <c>count</c> incorrectly, or references an unknown segment.</summary>
    IReadOnlyList<ApiError> CollectProjectionFieldIssues<TDbModel>(IReadOnlyList<ProjectedFieldSpec> specs, bool allowSelectWildcards = true)
        where TDbModel : class;

    /// <summary>Same as the generic overload, using an explicit root CLR type (e.g. for non-generic callers).</summary>
    IReadOnlyList<ApiError> CollectProjectionFieldIssues(Type rootType, IReadOnlyList<ProjectedFieldSpec> specs, bool allowSelectWildcards = true);

    /// <summary>Non-empty when any computed field has a missing name, empty template, or invalid SmartFormat syntax. Requires <see cref="IFormatterService" />; returns empty when the formatter is not registered.</summary>
    IReadOnlyList<ApiError> ValidateComputedFieldTemplates(IReadOnlyList<ComputedField> computedFields);

    /// <summary>Projects entities to selected fields (in-memory).</summary>
    IReadOnlyList<object?> ProjectEntities<TDbModel>(
        IReadOnlyList<TDbModel> items,
        IReadOnlyList<ProjectedFieldSpec> specs,
        QueryIncludeFilterMode includeFilterMode,
        ProjectedFilterConditions filterConditions)
        where TDbModel : class;

    /// <summary>Converts raw SQL projection results (tuple/anonymous) to dictionary format.</summary>
    IReadOnlyList<object?> ConvertSqlProjectedResults(IReadOnlyList<object?> raw, IReadOnlyList<ProjectedFieldSpec> specs, SqlProjectionConversionPlan? conversionPlan = null);

    /// <summary>
    /// When <paramref name="zipSiblingCollectionSelections" /> is <c>true</c>, merges parallel collection projections (e.g. <c>items.a</c> and <c>items.b</c>) into a single array of objects under <c>items</c>.
    /// Call after <see cref="ApplyComputedFields" /> so templates still see flat keys. Run before stripping auto-derived dependency columns so merged shapes still see sibling values.
    /// No-op when <paramref name="zipSiblingCollectionSelections" /> is <c>false</c> or when the select list cannot produce such a merge.
    /// </summary>
    void MergeSiblingCollectionProjectionRows(IReadOnlyList<object?> items, Type entityType, IReadOnlyList<ProjectedFieldSpec> specs, bool zipSiblingCollectionSelections);

    /// <summary>
    /// Removes leaves from merged collection rows for paths that were only added to load computed-field templates (see <see cref="EnsureSelectIncludesComputedDependencies" />).
    /// Call after <see cref="MergeSiblingCollectionProjectionRows" />. Fields the client put in <c>Select</c> are not in that set—users who want dependency columns must select them explicitly.
    /// </summary>
    void StripAutoDerivedDependencyLeavesFromMergedCollections(IReadOnlyList<object?> items, IReadOnlyList<ProjectedFieldSpec> specs, IReadOnlyCollection<string> autoDerivedSelectPaths);

    /// <summary>Extracts filter conditions from a WhereClause for projection-level filtering (MatchedOnly mode).</summary>
    ProjectedFilterConditions GetProjectedFilterConditions<TDbModel>(WhereClause? queryNode)
        where TDbModel : class;

    /// <summary>Applies MatchedOnly filtering to collection includes on entities (mutates in place).</summary>
    void ApplyMatchedOnlyIncludes<TDbModel>(IReadOnlyList<TDbModel> entities, IEnumerable<string> includes, ProjectedFilterConditions filterConditions)
        where TDbModel : class;

    /// <summary>
    /// Applies computed fields to projected results. Each row must be a <see cref="Dictionary{TKey,TValue}" /> (multi-field) or scalar (single-field). For dictionary rows,
    /// computed fields are added as new keys. For single-field results, rows are promoted to dictionaries. Requires IFormatterService to be registered; returns items unchanged when no
    /// formatter is available.
    /// </summary>
    IReadOnlyList<object?> ApplyComputedFields(IReadOnlyList<object?> items, IReadOnlyList<ComputedField> computedFields, IReadOnlyList<ProjectedFieldSpec> specs);

    /// <summary>
    /// Extracts the field names referenced by computed field templates (SmartFormat placeholders). Returns the distinct union of all placeholder names across all templates.
    /// Returns empty when IFormatterService is not registered.
    /// </summary>
    IReadOnlyList<string> GetComputedFieldDependencies(IReadOnlyList<ComputedField> computedFields);

    /// <summary>
    /// Ensures <see cref="ProjectionQueryReq.Select" /> lists every path referenced by <see cref="ProjectionQueryReq.ComputedFields" /> templates (via <see cref="GetComputedFieldDependencies" />).
    /// Mutates <paramref name="queryRequest" />.Select. Returns paths that were appended only for template dependencies (for stripping from the response after projection).
    /// </summary>
    IReadOnlyList<string> EnsureSelectIncludesComputedDependencies(ProjectionQueryReq queryRequest);

    /// <summary>
    /// Distinct CLR type names for the root entity and every navigation (including collection elements) touched by <paramref name="specs" />
    /// and by computed-field template placeholder paths in <paramref name="computedFields" />. Used for <see cref="Lyo.Api.Models.Common.Response.ProjectedQueryRes{T}.EntityTypes" />.
    /// </summary>
    IReadOnlyList<string> GetProjectionEntityTypeNames<TDbModel>(IReadOnlyList<ProjectedFieldSpec> specs, IReadOnlyList<ComputedField> computedFields)
        where TDbModel : class;
}

/// <summary>Resolved field spec for projection.</summary>
public sealed record ProjectedFieldSpec(string RequestedPath, string NormalizedPath, string[] NormalizedParts);

/// <summary>Filter condition for projection-level filtering.</summary>
public sealed record ProjectedFilterCondition(string NormalizedField, ComparisonOperatorEnum Comparison, object? Value);