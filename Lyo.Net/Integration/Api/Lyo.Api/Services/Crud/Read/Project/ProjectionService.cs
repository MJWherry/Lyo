using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Lyo.Api.Models;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Read.Project;

/// <summary>Resolves projection specs, builds projection expressions, and projects entities to selected fields.</summary>
public sealed class ProjectionService(IFormatterService? formatterService = null, ILogger<ProjectionService>? logger = null) : IProjectionService
{
    /// <summary>Bare dotted path used as template without braces, e.g. <c>contactaddresses.address.streetname</c>.</summary>
    private static readonly Regex BarePropertyPathRegex = new(
        @"^(?:[A-Za-z_][A-Za-z0-9_]*)(?:\.[A-Za-z_][A-Za-z0-9_]*)+$",
        RegexOptions.Compiled);

    /// <summary>SmartFormat-style <c>{token}</c> segments (no format spec); used when resolving projection entity types without <see cref="IFormatterService" />.</summary>
    private static readonly Regex TemplateDependencyBraceRegex = new(@"\{([^{}:]+)\}", RegexOptions.Compiled);

    private static readonly Type[] AnonymousProjectionTypes = [
        typeof(object), typeof(object), CreateAnonymousProjectionType(2), CreateAnonymousProjectionType(3), CreateAnonymousProjectionType(4), CreateAnonymousProjectionType(5),
        CreateAnonymousProjectionType(6), CreateAnonymousProjectionType(7), CreateAnonymousProjectionType(8)
    ];

    private static readonly PropertyInfo[][] AnonymousProjectionPropertyCache = Enumerable.Range(2, 7)
        .Select(n => AnonymousProjectionTypes[n].GetProperties().OrderBy(p => p.Name).ToArray())
        .ToArray();

    /// <summary>Compiled getters for anonymous projection shapes — avoids <see cref="PropertyInfo.GetValue(object?)" /> on hot paths.</summary>
    private static readonly Func<object, object?>[][] AnonymousProjectionFieldGetters =
        AnonymousProjectionPropertyCache.Select(propRow => propRow.Select(CompileAnonymousFieldGetter).ToArray()).ToArray();

    /// <summary>
    /// SmartFormat placeholders in braces, or a single bare dotted property path (no braces) so templates match projected keys and wildcard siblings.
    /// </summary>
    private IReadOnlyList<string> GetTemplatePlaceholders(string template)
    {
        if (string.IsNullOrWhiteSpace(template) || formatterService is null)
            return [];

        var fromFormatter = formatterService.GetPlaceholders(template);
        if (fromFormatter.Count > 0)
            return fromFormatter;

        var t = template.Trim();
        return BarePropertyPathRegex.IsMatch(t) ? [t] : [];
    }

    public (IReadOnlyList<ProjectedFieldSpec> Specs, IReadOnlyList<ApiError> PathErrors) ResolveProjectedFields<TDbModel>(
        IEnumerable<string> requestedFields,
        bool allowSelectWildcards = true,
        QueryPathValidationCache? pathCache = null)
        where TDbModel : class
    {
        var fields = requestedFields.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        ArgumentHelpers.ThrowIfZero(fields.Count, nameof(requestedFields));
        var rootType = typeof(TDbModel);
        var specs = new List<ProjectedFieldSpec>();
        var pathErrors = new List<ApiError>();
        foreach (var field in fields) {
            var ok = pathCache is not null 
                ? pathCache.TryNormalizeSelectPath<TDbModel>(field, out var normalized, out var errorMessage) 
                : SharedEntityMetadataCache.TryNormalizeFieldPath(rootType, field, out normalized, out errorMessage);

            if (!ok) {
                pathErrors.Add(new(Constants.ApiErrorCodes.InvalidSelectField, errorMessage ?? "Invalid field path."));
                continue;
            }

            if (!allowSelectWildcards && normalized!.Contains('*', StringComparison.Ordinal)) {
                pathErrors.Add(new(Constants.ApiErrorCodes.InvalidSelectField, "Select path wildcards are disabled by API query configuration."));
                continue;
            }

            specs.Add(new(field, normalized!, normalized!.Split('.', StringSplitOptions.RemoveEmptyEntries)));
        }

        if (pathErrors.Count > 0)
            return ([], pathErrors);

        return (specs, []);
    }

    public string NormalizeFieldPath(Type rootType, string path) => SharedEntityMetadataCache.NormalizeFieldPath(rootType, path);

    public IEnumerable<string> GetDerivedIncludes(Type rootType, IReadOnlyList<ProjectedFieldSpec> specs)
        => specs.Select(i => BuildIncludePath(rootType, i.NormalizedParts)).Where(i => !string.IsNullOrWhiteSpace(i)).Cast<string>();

    public SqlProjectionBuildResult<TDbModel> TryBuildSqlProjectionExpression<TDbModel>(IReadOnlyList<ProjectedFieldSpec> specs, bool projectionPathsAlreadyValidated = false)
        where TDbModel : class
    {
        if (specs.Count == 0
            || (!projectionPathsAlreadyValidated && CollectProjectionFieldIssues<TDbModel>(specs, allowSelectWildcards: true).Count > 0))
            return new(null, null);

        var plan = BuildSqlProjectionConversionPlan(typeof(TDbModel), specs);
        if (plan == null)
            return new(null, null);

        var parameter = Expression.Parameter(typeof(TDbModel), "e");
        var expressions = new List<Expression>(plan.Slots.Count);
        foreach (var slot in plan.Slots) {
            switch (slot) {
                case SqlProjectionSingleSlot(var specIndex): {
                    var spec = specs[specIndex];
                    if (spec.NormalizedParts.Length == 0 || spec.NormalizedParts[0] == "*")
                        return new(null, null);

                    var expr = BuildPropertyExpression(parameter, typeof(TDbModel), spec.NormalizedParts, 0);
                    if (expr == null)
                        return new(null, null);

                    expressions.Add(Expression.Convert(expr, typeof(object)));
                    break;
                }
                case SqlProjectionMergedCollectionSlot(var indices): {
                    var groupSpecs = indices.Select(i => specs[i]).ToList();
                    var merged = BuildMergedCollectionSqlProjection(parameter, typeof(TDbModel), groupSpecs);
                    if (merged == null)
                        return new(null, null);

                    expressions.Add(Expression.Convert(merged, typeof(object)));
                    break;
                }
                default:
                    return new(null, null);
            }
        }

        var body = expressions.Count == 1 ? expressions[0] : BuildMultiValueProjection(expressions);
        return new(Expression.Lambda<Func<TDbModel, object?>>(body, parameter), plan);
    }

    /// <summary>Groups sibling collection fields into one slot so EF emits a single join + Select.</summary>
    private static SqlProjectionConversionPlan? BuildSqlProjectionConversionPlan(Type entityType, IReadOnlyList<ProjectedFieldSpec> specs)
    {
        var mergeGroups = ProjectionCollectionZip.PlanMergeGroupsForSqlSlots(entityType, specs);
        var consumed = new bool[specs.Count];
        var slots = new List<SqlProjectionSlot>(specs.Count);

        for (var i = 0; i < specs.Count; i++) {
            if (consumed[i])
                continue;

            var group = mergeGroups.FirstOrDefault(g => g.Specs.Any(gs => gs.Equals(specs[i])) && g.Specs.Count >= 2);
            if (group != null) {
                var indices = group.Specs.Select(gs => IndexOfSpec(specs, gs)).Where(ix => ix >= 0).OrderBy(ix => ix).ToArray();
                if (indices.Length < 2)
                    slots.Add(new SqlProjectionSingleSlot(i));
                else {
                    foreach (var ix in indices)
                        consumed[ix] = true;

                    slots.Add(new SqlProjectionMergedCollectionSlot(indices));
                }
            }
            else
                slots.Add(new SqlProjectionSingleSlot(i));
        }

        if (slots.Count == 0 || slots.Count > 8)
            return null;

        return new(slots);
    }

    private static int IndexOfSpec(IReadOnlyList<ProjectedFieldSpec> specs, ProjectedFieldSpec target)
    {
        for (var i = 0; i < specs.Count; i++) {
            if (specs[i].Equals(target))
                return i;
        }

        return -1;
    }

    /// <summary>One <c>collection.Select(x =&gt; new {{ ... }})</c> for all sibling leaves (single join).</summary>
    private static Expression? BuildMergedCollectionSqlProjection(ParameterExpression parameter, Type rootType, IReadOnlyList<ProjectedFieldSpec> groupSpecs)
    {
        if (groupSpecs.Count < 2)
            return null;

        var mergePrefixParts = groupSpecs[0].NormalizedParts[..^1];
        if (mergePrefixParts.Length == 0)
            return null;

        // Select applies to the first ICollection in the shared prefix; nested navigations (e.g. contactaddresses → address → leaf) belong in the lambda.
        if (!TryFindMergedSqlOuterCollectionSegmentCount(rootType, mergePrefixParts, out var outerSegmentCount))
            return null;

        var outerPrefixParts = mergePrefixParts[..outerSegmentCount];
        if (!TryBuildExpressionToCollectionNavigation(parameter, rootType, outerPrefixParts, out var collectionExpr, out var elementType))
            return null;

        var innerStartPartIndex = outerSegmentCount;
        var innerParam = Expression.Parameter(elementType, "x");
        var leafExprs = new List<Expression>(groupSpecs.Count);
        foreach (var spec in groupSpecs) {
            var innerExpr = BuildPropertyExpression(innerParam, elementType, spec.NormalizedParts, innerStartPartIndex);
            if (innerExpr == null)
                return null;

            leafExprs.Add(Expression.Convert(innerExpr, typeof(object)));
        }

        var fieldCount = leafExprs.Count;
        if (fieldCount is < 2 or > 8)
            return null;

        var anonType = AnonymousProjectionTypes[fieldCount];
        var ctor = anonType.GetConstructors().First(c => c.GetParameters().Length == fieldCount);
        var newExpr = Expression.New(ctor, leafExprs);
        var delegateType = typeof(Func<,>).MakeGenericType(elementType, anonType);
        var selector = Expression.Lambda(delegateType, newExpr, innerParam);
        var selectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, anonType);

        return Expression.Call(selectMethod, collectionExpr, selector);
    }

    /// <summary>Index (1-based count) of segments from root through the property whose type is an ICollection (the merged Select target).</summary>
    private static bool TryFindMergedSqlOuterCollectionSegmentCount(Type rootType, string[] mergePrefixParts, out int outerSegmentCount)
    {
        outerSegmentCount = 0;
        var current = rootType;
        for (var i = 0; i < mergePrefixParts.Length; i++) {
            if (SharedEntityMetadataCache.IsCollectionType(current))
                current = SharedEntityMetadataCache.GetCollectionElementType(current);

            var property = SharedEntityMetadataCache.ResolveProperty(current, mergePrefixParts[i]);
            if (property is null)
                return false;

            if (SharedEntityMetadataCache.IsCollectionType(property.PropertyType)) {
                outerSegmentCount = i + 1;
                return true;
            }

            current = property.PropertyType;
        }

        return false;
    }

    /// <summary>Navigates <paramref name="rootExpr" /> along <paramref name="prefixParts" /> and stops at the collection property (no Select).</summary>
    private static bool TryBuildExpressionToCollectionNavigation(
        Expression rootExpr,
        Type rootType,
        string[] prefixParts,
        out Expression collectionExpr,
        out Type elementType)
    {
        collectionExpr = null!;
        elementType = null!;

        if (prefixParts.Length == 0)
            return false;

        Expression current = rootExpr;
        var currentType = rootType;
        foreach (var part in prefixParts) {
            if (SharedEntityMetadataCache.IsCollectionType(currentType))
                currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

            var property = SharedEntityMetadataCache.ResolveProperty(currentType, part);
            if (property == null)
                return false;

            current = Expression.Property(current, property);
            currentType = property.PropertyType;
        }

        if (!SharedEntityMetadataCache.IsCollectionType(currentType))
            return false;

        collectionExpr = current;
        elementType = SharedEntityMetadataCache.GetCollectionElementType(currentType);
        return true;
    }

    public IReadOnlyList<object?> ProjectEntities<TDbModel>(
        IReadOnlyList<TDbModel> items,
        IReadOnlyList<ProjectedFieldSpec> specs,
        QueryIncludeFilterMode includeFilterMode,
        ProjectedFilterConditions filterConditions)
        where TDbModel : class
    {
        var results = new List<object?>(items.Count);
        foreach (var item in items) {
            if (specs.Count == 1) {
                var only = specs[0];
                var singleValue = ExtractPathValue(item, only.NormalizedParts, 0);
                if (includeFilterMode == QueryIncludeFilterMode.MatchedOnly)
                    singleValue = ApplyMatchedOnlyProjectionFilter(singleValue, only, filterConditions);

                results.Add(singleValue);
                continue;
            }

            var row = new Dictionary<string, object?>(specs.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var spec in specs) {
                var value = ExtractPathValue(item, spec.NormalizedParts, 0);
                if (includeFilterMode == QueryIncludeFilterMode.MatchedOnly)
                    value = ApplyMatchedOnlyProjectionFilter(value, spec, filterConditions);

                if (spec.NormalizedParts.Length == 1 && spec.NormalizedParts[0] == "*" && value is IReadOnlyDictionary<string, object?> wildcardMap) {
                    foreach (var kvp in wildcardMap)
                        row[kvp.Key] = kvp.Value;
                }
                else
                    row[spec.RequestedPath] = value;
            }

            results.Add(row);
        }

        return results;
    }

    public IReadOnlyList<object?> ConvertSqlProjectedResults(IReadOnlyList<object?> raw, IReadOnlyList<ProjectedFieldSpec> specs, SqlProjectionConversionPlan? conversionPlan = null)
    {
        if (specs.Count == 1) {
            // Normalize collection results (e.g. List<string>) to List<object?> for consistency with in-memory projection
            var results = new List<object?>(raw.Count);
            foreach (var item in raw) {
                if (item is IEnumerable enumerable && item is not string and not byte[]) {
                    var list = new List<object?>();
                    foreach (var x in enumerable)
                        list.Add(x);

                    results.Add(list);
                }
                else
                    results.Add(item);
            }

            return results;
        }

        if (conversionPlan == null)
            return ConvertSqlProjectedResultsLegacy(raw, specs);

        var multiResults = new List<object?>(raw.Count);
        foreach (var item in raw) {
            if (item == null) {
                multiResults.Add(null);
                continue;
            }

            var dict = new Dictionary<string, object?>(specs.Count, StringComparer.OrdinalIgnoreCase);
            var slotCount = conversionPlan.Slots.Count;
            for (var slotIx = 0; slotIx < slotCount; slotIx++) {
                var slot = conversionPlan.Slots[slotIx];
                var slotValue = GetOuterProjectionSlotValue(item, slotIx, slotCount);
                switch (slot) {
                    case SqlProjectionSingleSlot(var specIndex):
                        dict[specs[specIndex].RequestedPath] = slotValue;
                        break;
                    case SqlProjectionMergedCollectionSlot(var indices):
                        ExpandMergedSqlCollectionSlot(dict, specs, slotValue, indices);
                        break;
                }
            }

            multiResults.Add(dict);
        }

        return multiResults;
    }

    private static IReadOnlyList<object?> ConvertSqlProjectedResultsLegacy(IReadOnlyList<object?> raw, IReadOnlyList<ProjectedFieldSpec> specs)
    {
        var multiResults = new List<object?>(raw.Count);
        foreach (var item in raw) {
            if (item == null) {
                multiResults.Add(null);
                continue;
            }

            var dict = new Dictionary<string, object?>(specs.Count, StringComparer.OrdinalIgnoreCase);
            if (item is ITuple tuple) {
                for (var i = 0; i < Math.Min(specs.Count, tuple.Length); i++)
                    dict[specs[i].RequestedPath] = tuple[i];
            }
            else {
                var n = Math.Min(specs.Count, 8);
                var getters = AnonymousProjectionFieldGetters[n - 2];
                for (var i = 0; i < Math.Min(specs.Count, getters.Length); i++)
                    dict[specs[i].RequestedPath] = getters[i](item);
            }

            multiResults.Add(dict);
        }

        return multiResults;
    }

    private static object? GetOuterProjectionSlotValue(object item, int slotIndex, int slotCount)
    {
        // Single-slot projection: body is one expression (e.g. merged collection Select) — no outer anonymous wrapper.
        if (slotCount == 1 && slotIndex == 0)
            return item;

        if (item is ITuple tuple && slotIndex < tuple.Length)
            return tuple[slotIndex];

        var getters = AnonymousProjectionFieldGetters[slotCount - 2];
        return getters[slotIndex](item);
    }

    private static void ExpandMergedSqlCollectionSlot(
        Dictionary<string, object?> dict,
        IReadOnlyList<ProjectedFieldSpec> specs,
        object? slotValue,
        IReadOnlyList<int> indices)
    {
        var n = indices.Count;
        var innerGetters = AnonymousProjectionFieldGetters[n - 2];
        var lists = new List<object?>[n];
        if (slotValue is IEnumerable enumerable && slotValue is not string and not byte[]) {
            var rowHint = enumerable is ICollection coll ? coll.Count : 0;
            for (var j = 0; j < n; j++)
                lists[j] = rowHint > 0 ? new List<object?>(rowHint) : [];

            foreach (var row in enumerable) {
                if (row == null)
                    continue;

                for (var j = 0; j < n; j++)
                    lists[j].Add(innerGetters[j](row));
            }
        }
        else {
            for (var j = 0; j < n; j++)
                lists[j] = [];
        }

        for (var j = 0; j < n; j++)
            dict[specs[indices[j]].RequestedPath] = lists[j];
    }

    /// <inheritdoc />
    public void MergeSiblingCollectionProjectionRows(IReadOnlyList<object?> items, Type entityType, IReadOnlyList<ProjectedFieldSpec> specs, bool zipSiblingCollectionSelections)
    {
        if (!zipSiblingCollectionSelections || items.Count == 0)
            return;

        var groups = ProjectionCollectionZip.PlanMergeGroups(entityType, specs, items);
        if (groups.Count == 0)
            return;

        // Shallower merge keys first (e.g. ContactAddresses before ContactAddresses.Address) so parallel columns
        // are zipped into the scope-wildcard row before any nested merge runs; nested merges under a parent A.*
        // are skipped separately via MergeKeyIsUnderParentScopeWildcard.
        var orderedGroups = groups
            .OrderBy(g => g.MergeKey.Split('.', StringSplitOptions.RemoveEmptyEntries).Length)
            .ToList();

        ProjectionCollectionZip.ApplyMergeGroupsToRows(items, orderedGroups, specs);
    }

    /// <inheritdoc />
    public void StripAutoDerivedDependencyLeavesFromMergedCollections(
        IReadOnlyList<object?> items,
        IReadOnlyList<ProjectedFieldSpec> specs,
        IReadOnlyCollection<string> autoDerivedSelectPaths)
    {
        if (autoDerivedSelectPaths.Count == 0 || items.Count == 0)
            return;

        foreach (var derivedPath in autoDerivedSelectPaths) {
            var spec = specs.FirstOrDefault(s => string.Equals(s.RequestedPath, derivedPath, StringComparison.OrdinalIgnoreCase));
            if (spec is null)
                continue;
            if (spec.NormalizedParts.Length < 2)
                continue;
            if (ProjectionCollectionZip.NormalizedPartsContainWildcard(spec.NormalizedParts))
                continue;

            var mergeKey = string.Join(".", spec.NormalizedParts[..^1]);
            var leaf = spec.NormalizedParts[^1];

            foreach (var item in items) {
                if (item is not Dictionary<string, object?> row)
                    continue;
                if (!row.TryGetValue(mergeKey, out var mergedVal) || mergedVal is null)
                    continue;
                if (mergedVal is not IList list)
                    continue;

                foreach (var el in list) {
                    if (el is Dictionary<string, object?> inner)
                        inner.Remove(leaf);
                }
            }
        }
    }


    private static int GetEnumerableLength(object? value)
    {
        if (value is null)
            return 0;

        if (value is ICollection collection)
            return collection.Count;

        if (value is IEnumerable enumerable and not string and not byte[]) {
            var n = 0;
            foreach (var _ in enumerable)
                n++;

            return n;
        }

        return 0;
    }

    private static object? GetElementAtIndex(object? value, int index)
    {
        if (value is null)
            return null;

        if (value is IList list)
            return index < list.Count ? list[index] : null;

        if (value is IEnumerable enumerable and not string and not byte[]) {
            var i = 0;
            foreach (var x in enumerable) {
                if (i == index)
                    return x;

                i++;
            }
        }

        return null;
    }

    public ProjectedFilterConditions GetProjectedFilterConditions<TDbModel>(WhereClause? queryNode)
        where TDbModel : class
    {
        var conditions = new List<ProjectedFilterCondition>();
        if (!WhereClauseUtils.TryExtractConditions(queryNode, out var queryConditions, out var op))
            return new(conditions, op);

        foreach (var condition in queryConditions) {
            try {
                var normalizedField = NormalizeFieldPath(typeof(TDbModel), condition.Field);
                conditions.Add(new(normalizedField, condition.Comparison, condition.Value));
            }
            catch {
                // Ignore fields that cannot be normalized for projection-level filtering.
            }
        }

        return new(conditions, op);
    }

    public void ApplyMatchedOnlyIncludes<TDbModel>(IReadOnlyList<TDbModel> entities, IEnumerable<string> includes, ProjectedFilterConditions filterConditions)
        where TDbModel : class
    {
        if (entities.Count == 0 || filterConditions.Conditions.Count == 0)
            return;

        foreach (var includePath in includes.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct(StringComparer.OrdinalIgnoreCase)) {
            var scope = TryBuildIncludeScope(typeof(TDbModel), includePath);
            if (scope == null)
                continue;

            var scopedConditions = filterConditions.Conditions.Where(i => i.NormalizedField.StartsWith(scope.CollectionPath + ".", StringComparison.OrdinalIgnoreCase))
                .Select(i => new ProjectedFilterCondition(i.NormalizedField[(scope.CollectionPath.Length + 1)..], i.Comparison, i.Value))
                .ToList();

            if (scopedConditions.Count == 0)
                continue;

            foreach (var entity in entities)
                ApplyMatchedOnlyIncludeScope(entity, scope, scopedConditions, filterConditions.Operator);
        }
    }

    public IReadOnlyList<object?> ApplyComputedFields(IReadOnlyList<object?> items, IReadOnlyList<ComputedField> computedFields, IReadOnlyList<ProjectedFieldSpec> specs)
    {
        if (computedFields.Count == 0 || items.Count == 0)
            return items;

        if (formatterService is null) {
            logger?.LogWarning("ComputedFields requested but IFormatterService is not registered; returning items unchanged");
            return items;
        }

        var results = new List<object?>(items.Count);
        foreach (var item in items) {
            if (item is null) {
                results.Add(null);
                continue;
            }

            var row = PromoteToDictionary(item, specs);
            foreach (var field in computedFields) {
                if (string.IsNullOrWhiteSpace(field.Name) || string.IsNullOrWhiteSpace(field.Template))
                    continue;

                if (TryApplyCollectionParallelComputed(row, field))
                    continue;

                row[field.Name] = FormatComputedValue(field.Template, row);
            }

            results.Add(row);
        }

        return results;
    }

    /// <summary>
    /// When every template placeholder is under the same collection path (e.g. <c>docketcharges.description</c> and <c>docketcharges.number</c>),
    /// format once per index and store a parallel column <c>{prefix}.{computedName}</c> so sibling merge zips it into each collection object.
    /// </summary>
    private bool TryApplyCollectionParallelComputed(Dictionary<string, object?> row, ComputedField field)
    {
        var placeholders = GetTemplatePlaceholders(field.Template);
        if (placeholders.Count == 0)
            return false;

        if (!TryGetUniformCollectionPlaceholderPrefix(placeholders, out var collPrefix))
            return false;

        var columns = new object?[placeholders.Count];
        for (var p = 0; p < placeholders.Count; p++) {
            if (!TryResolveRowValueForTemplate(row, placeholders[p], out var colVal))
                return false;
            if (colVal is string or byte[])
                return false;
            if (colVal is not IEnumerable)
                return false;

            columns[p] = colVal;
        }

        var maxLen = 0;
        foreach (var col in columns) {
            if (col is null)
                continue;

            maxLen = Math.Max(maxLen, GetEnumerableLength(col));
        }

        var formatted = new List<object?>(maxLen);
        for (var i = 0; i < maxLen; i++) {
            var miniRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var p = 0; p < placeholders.Count; p++)
                miniRow[placeholders[p]] = GetElementAtIndex(columns[p] as IEnumerable, i);

            formatted.Add(FormatComputedValue(field.Template, miniRow));
        }

        row[$"{collPrefix}.{field.Name}"] = formatted;
        return true;
    }

    /// <summary>All placeholders must be multi-segment paths sharing the same prefix (case-insensitive), e.g. <c>a.b</c> and <c>a.c</c> → <c>a</c>.</summary>
    private static bool TryGetUniformCollectionPlaceholderPrefix(IReadOnlyList<string> placeholders, out string collPrefix)
    {
        collPrefix = "";
        if (placeholders.Count == 0)
            return false;

        string? first = null;
        foreach (var ph in placeholders) {
            var idx = ph.LastIndexOf('.');
            if (idx <= 0)
                return false;

            var pfx = ph[..idx];
            first ??= pfx;
            if (!string.Equals(first, pfx, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        collPrefix = first!;
        return true;
    }

    private static string GetCollectionRequestedPathPrefix(string fullPath)
    {
        var i = fullPath.LastIndexOf('.');
        return i <= 0 ? fullPath : fullPath[..i];
    }

    public IReadOnlyList<string> GetComputedFieldDependencies(IReadOnlyList<ComputedField> computedFields)
    {
        if (formatterService is null || computedFields.Count == 0)
            return [];

        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cf in computedFields) {
            if (string.IsNullOrWhiteSpace(cf.Template))
                continue;

            foreach (var placeholder in GetTemplatePlaceholders(cf.Template))
                fields.Add(placeholder);
        }

        return [..fields];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> EnsureSelectIncludesComputedDependencies(ProjectionQueryReq queryRequest)
    {
        if (queryRequest.ComputedFields.Count == 0)
            return [];

        var derived = GetComputedFieldDependencies(queryRequest.ComputedFields);
        if (derived.Count == 0)
            return [];

        var added = new List<string>();
        foreach (var field in derived) {
            if (SelectListAlreadyContainsPath(queryRequest.Select, field))
                continue;

            var normalized = field.Trim();
            if (normalized.Length == 0)
                continue;

            queryRequest.Select.Add(normalized);
            added.Add(normalized);
        }

        return added;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetProjectionEntityTypeNames<TDbModel>(IReadOnlyList<ProjectedFieldSpec> specs, IReadOnlyList<ComputedField> computedFields)
        where TDbModel : class
    {
        var root = typeof(TDbModel);
        var set = new HashSet<string>(StringComparer.Ordinal) { root.Name };
        foreach (var spec in specs)
            AppendEntityTypesAlongPath(root, spec.NormalizedParts, set);

        foreach (var dep in EnumerateTemplateDependencyPaths(computedFields)) {
            string normalized;
            try {
                normalized = NormalizeFieldPath(root, dep);
            }
            catch {
                continue;
            }

            var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
            AppendEntityTypesAlongPath(root, parts, set);
        }

        return set.OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    /// <summary>Placeholder paths from computed templates — same sources as <see cref="GetComputedFieldDependencies" />, but works when <see cref="IFormatterService" /> is not registered.</summary>
    private IEnumerable<string> EnumerateTemplateDependencyPaths(IReadOnlyList<ComputedField> computedFields)
    {
        foreach (var cf in computedFields) {
            if (string.IsNullOrWhiteSpace(cf.Template))
                continue;

            if (formatterService is not null) {
                var fromFormatter = formatterService.GetPlaceholders(cf.Template);
                if (fromFormatter.Count > 0) {
                    foreach (var p in fromFormatter)
                        yield return p;

                    continue;
                }
            }

            var trimmed = cf.Template.Trim();
            if (BarePropertyPathRegex.IsMatch(trimmed)) {
                yield return trimmed;
                continue;
            }

            foreach (Match m in TemplateDependencyBraceRegex.Matches(cf.Template))
                yield return m.Groups[1].Value.Trim();
        }
    }

    /// <summary>Records CLR names for entity classes reached when walking <paramref name="parts" /> from <paramref name="rootType" />.</summary>
    private static void AppendEntityTypesAlongPath(Type rootType, IReadOnlyList<string> parts, HashSet<string> set)
    {
        if (parts.Count == 0)
            return;

        var currentType = rootType;
        for (var i = 0; i < parts.Count; i++) {
            var part = parts[i];
            if (part == "*")
                break;

            if (SharedEntityMetadataCache.IsCollectionType(currentType))
                currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

            if (part.Equals("count", StringComparison.OrdinalIgnoreCase))
                break;

            var property = SharedEntityMetadataCache.ResolveProperty(currentType, part);
            if (property == null)
                break;

            var propType = property.PropertyType;
            if (SharedEntityMetadataCache.IsCollectionType(propType)) {
                var elementType = SharedEntityMetadataCache.GetCollectionElementType(propType);
                if (IsProjectionEntityClrType(elementType))
                    set.Add(elementType.Name);

                currentType = elementType;
            }
            else {
                if (IsProjectionEntityClrType(propType))
                    set.Add(propType.Name);

                currentType = propType;
            }
        }
    }

    private static bool IsProjectionEntityClrType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsClass && !IsSimpleType(t);
    }

    /// <summary>Trim + case-insensitive match so template placeholders align with client <see cref="ProjectionQueryReq.Select" /> entries.</summary>
    private static bool SelectListAlreadyContainsPath(IReadOnlyList<string> select, string fieldPath)
    {
        var want = fieldPath.Trim();
        if (want.Length == 0)
            return true;

        foreach (var existing in select) {
            if (string.IsNullOrWhiteSpace(existing))
                continue;
            var e = existing.Trim();
            if (string.Equals(e, want, StringComparison.OrdinalIgnoreCase))
                return true;

            // e.g. "contactaddresses.address.*" covers "contactaddresses.address.streetname" (one leaf under that scope).
            // "contactaddresses.*" covers only direct fields on each collection element (one segment after the scope), not
            // nested paths like "contactaddresses.address.streettype" — those must be added explicitly for projection.
            if (e.EndsWith(".*", StringComparison.OrdinalIgnoreCase) && e.Length > 2) {
                var basePath = e[..^2];
                if (want.Length <= basePath.Length
                    || !want.StartsWith(basePath + ".", StringComparison.OrdinalIgnoreCase))
                    continue;

                var rest = want[(basePath.Length + 1)..];
                if (!rest.Contains('.'))
                    return true;
            }
        }

        return false;
    }

    private string? BuildIncludePath(Type rootType, IReadOnlyList<string> normalizedParts)
    {
        var includeSegments = new List<string>(normalizedParts.Count);
        var currentType = rootType;
        foreach (var part in normalizedParts) {
            if (part == "*")
                break;

            if (SharedEntityMetadataCache.IsCollectionType(currentType))
                currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

            var property = ResolvePropertyCached(currentType, part);
            if (property == null)
                break;

            var propertyType = property.PropertyType;
            if (SharedEntityMetadataCache.IsCollectionType(propertyType) || IsNavigationType(propertyType))
                includeSegments.Add(property.Name);

            currentType = propertyType;
            if (!SharedEntityMetadataCache.IsCollectionType(currentType) && !IsNavigationType(currentType))
                break;
        }

        return includeSegments.Count == 0 ? null : string.Join(".", includeSegments);
    }

    private IncludeScopeSpec? TryBuildIncludeScope(Type rootType, string includePath)
    {
        var normalizedInclude = NormalizeFieldPath(rootType, includePath);
        var parts = normalizedInclude.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        var currentType = rootType;
        var traversed = new List<string>(parts.Length);
        foreach (var part in parts) {
            if (SharedEntityMetadataCache.IsCollectionType(currentType))
                currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

            var property = ResolvePropertyCached(currentType, part);
            if (property == null)
                return null;

            traversed.Add(property.Name);
            if (SharedEntityMetadataCache.IsCollectionType(property.PropertyType))
                return new(string.Join(".", traversed), traversed.Take(traversed.Count - 1).ToArray(), property);

            currentType = property.PropertyType;
        }

        return null;
    }

    private static void ApplyMatchedOnlyIncludeScope(object entity, IncludeScopeSpec scope, IReadOnlyList<ProjectedFilterCondition> scopedConditions, GroupOperatorEnum op)
    {
        var owner = NavigateToObject(entity, scope.PrefixParts);
        if (owner == null)
            return;

        var collectionValue = scope.CollectionProperty.GetValue(owner);
        if (collectionValue is not IEnumerable enumerable || collectionValue is string or byte[])
            return;

        var filteredItems = new List<object?>();
        foreach (var item in enumerable) {
            if (item == null)
                continue;

            var isMatch = op == GroupOperatorEnum.Or
                ? scopedConditions.Any(c => EvaluateComparisonOnPossibleCollection(GetNestedProjectedValue(item, c.NormalizedField), c.Comparison, c.Value))
                : scopedConditions.All(c => EvaluateComparisonOnPossibleCollection(GetNestedProjectedValue(item, c.NormalizedField), c.Comparison, c.Value));

            if (isMatch)
                filteredItems.Add(item);
        }

        SetCollectionProperty(owner, scope.CollectionProperty, filteredItems);
    }

    private static object? NavigateToObject(object root, IReadOnlyList<string> parts)
    {
        var current = root;
        foreach (var part in parts) {
            var property = SharedEntityMetadataCache.ResolveProperty(current.GetType(), part);
            if (property == null)
                return null;

            var next = property.GetValue(current);
            if (next == null)
                return null;

            current = next;
        }

        return current;
    }

    private static void SetCollectionProperty(object owner, PropertyInfo collectionProperty, IReadOnlyList<object?> filteredItems)
    {
        var elementType = SharedEntityMetadataCache.GetCollectionElementType(collectionProperty.PropertyType);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var replacement = (IList)Activator.CreateInstance(listType, filteredItems.Count)!;
        foreach (var item in filteredItems) {
            if (item == null && elementType.IsValueType && Nullable.GetUnderlyingType(elementType) == null)
                continue;

            replacement.Add(item);
        }

        if (collectionProperty.CanWrite && collectionProperty.PropertyType.IsAssignableFrom(listType)) {
            collectionProperty.SetValue(owner, replacement);
            return;
        }

        var existing = collectionProperty.GetValue(owner);
        if (existing is IList existingList && !existingList.IsReadOnly) {
            existingList.Clear();
            foreach (var item in replacement)
                existingList.Add(item);
        }
    }

    private static object? GetNestedProjectedValue(object? value, string path)
    {
        if (value is null || string.IsNullOrWhiteSpace(path))
            return value;

        var current = value;
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts) {
            if (current is IReadOnlyDictionary<string, object?> readonlyDict) {
                if (!readonlyDict.TryGetValue(part, out current))
                    return null;
            }
            else if (current is IDictionary<string, object?> dict) {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else {
                var property = SharedEntityMetadataCache.ResolveProperty(current!.GetType(), part);
                if (property == null)
                    return null;

                current = property.GetValue(current);
            }
        }

        return current;
    }

    private static bool EvaluateComparisonOnPossibleCollection(object? actualValue, ComparisonOperatorEnum comparator, object? expectedValue)
        => ComparisonEvaluator.Evaluate(actualValue, comparator, expectedValue);

    private static object? ApplyMatchedOnlyProjectionFilter(object? value, ProjectedFieldSpec spec, ProjectedFilterConditions filterConditions)
    {
        var conditions = filterConditions.Conditions;
        if (value == null || conditions.Count == 0)
            return value;

        if (spec.NormalizedParts.Length > 0 && spec.NormalizedParts[^1] == "*" && value is List<object?> objectList) {
            var wildcardRoot = spec.NormalizedPath[..^2];
            var scopedConditions = conditions.Where(i => i.NormalizedField.StartsWith(wildcardRoot + ".", StringComparison.OrdinalIgnoreCase))
                .Select(i => new ProjectedFilterCondition(i.NormalizedField[(wildcardRoot.Length + 1)..], i.Comparison, i.Value))
                .ToList();

            if (scopedConditions.Count == 0)
                return value;

            var matchScoped = filterConditions.Operator == GroupOperatorEnum.Or
                ? (Func<object?, bool>)(item => scopedConditions.Any(c => ComparisonEvaluator.Evaluate(GetNestedProjectedValue(item, c.NormalizedField), c.Comparison, c.Value)))
                : item => scopedConditions.All(c => ComparisonEvaluator.Evaluate(GetNestedProjectedValue(item, c.NormalizedField), c.Comparison, c.Value));

            return objectList.Where(item => matchScoped(item)).ToList();
        }

        if (value is List<object?> scalarList) {
            var exactConditions = conditions.Where(i => string.Equals(i.NormalizedField, spec.NormalizedPath, StringComparison.OrdinalIgnoreCase)).ToList();
            if (exactConditions.Count == 0)
                return value;

            var matchScalar = filterConditions.Operator == GroupOperatorEnum.Or
                ? (Func<object?, bool>)(v => exactConditions.Any(c => ComparisonEvaluator.Evaluate(v, c.Comparison, c.Value)))
                : v => exactConditions.All(c => ComparisonEvaluator.Evaluate(v, c.Comparison, c.Value));

            return scalarList.Where(matchScalar).ToList();
        }

        return value;
    }

    private static object? ExtractPathValue(object? current, IReadOnlyList<string> parts, int index)
    {
        if (current == null)
            return null;

        if (index >= parts.Count)
            return ToTerminalValue(current);

        var part = parts[index];
        if (part.Equals("count", StringComparison.OrdinalIgnoreCase) && current is IEnumerable countEnumerable && current is not string and not byte[])
            return countEnumerable.Cast<object?>().Count();

        if (part == "*") {
            if (current is not IEnumerable wildcardEnumerable || current is string or byte[])
                return ProjectWildcard(current);

            var wildcardList = new List<object?>();
            foreach (var item in wildcardEnumerable)
                wildcardList.Add(item == null ? null : ProjectWildcard(item));

            return wildcardList;
        }

        if (current is IEnumerable enumerable && current is not string and not byte[]) {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(ExtractPathValue(item, parts, index));

            return list;
        }

        var property = SharedEntityMetadataCache.ResolveProperty(current.GetType(), part);
        if (property == null)
            return null;

        var next = property.GetValue(current);
        return ExtractPathValue(next, parts, index + 1);
    }

    private static object? ToTerminalValue(object? value)
    {
        if (value == null)
            return null;

        if (IsSimpleType(value.GetType()))
            return value;

        if (value is not (IEnumerable enumerable and not string and not byte[]))
            return ProjectWildcard(value);

        var list = new List<object?>();
        foreach (var item in enumerable)
            list.Add(ToTerminalValue(item));

        return list;
    }

    private static IReadOnlyDictionary<string, object?> ProjectWildcard(object value)
    {
        var valueType = value.GetType();
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in valueType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(i => i.CanRead && i.GetIndexParameters().Length == 0)) {
            object? propertyValue;
            try {
                propertyValue = property.GetValue(value);
            }
            catch {
                propertyValue = null;
            }

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (IsSimpleType(propertyType))
                dictionary[property.Name] = propertyValue;
        }

        return dictionary;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private PropertyInfo? ResolvePropertyCached(Type type, string name) => SharedEntityMetadataCache.ResolveProperty(type, name);

    /// <inheritdoc />
    public IReadOnlyList<ApiError> CollectProjectionFieldIssues<TDbModel>(IReadOnlyList<ProjectedFieldSpec> specs, bool allowSelectWildcards = true)
        where TDbModel : class
        => CollectProjectionFieldIssues(typeof(TDbModel), specs, allowSelectWildcards);

    /// <inheritdoc />
    public IReadOnlyList<ApiError> CollectProjectionFieldIssues(Type rootType, IReadOnlyList<ProjectedFieldSpec> specs, bool allowSelectWildcards = true)
    {
        var list = CollectProjectionFieldIssuesList(rootType, specs, allowSelectWildcards);
        return list.Count == 0 ? Array.Empty<ApiError>() : list;
    }

    /// <inheritdoc />
    public IReadOnlyList<ApiError> ValidateComputedFieldTemplates(IReadOnlyList<ComputedField> computedFields)
    {
        if (computedFields.Count == 0 || formatterService is null)
            return [];

        var errors = new List<ApiError>();
        foreach (var cf in computedFields) {
            if (string.IsNullOrWhiteSpace(cf.Name)) {
                errors.Add(new(Constants.ApiErrorCodes.InvalidComputedField, "A computed field name is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(cf.Template)) {
                errors.Add(new(Constants.ApiErrorCodes.InvalidComputedField, $"Computed field {ValidationFieldFormatter.Quote(cf.Name)} has an empty template."));
                continue;
            }

            if (!formatterService.TryValidateTemplate(cf.Template, out var message))
                errors.Add(new(Constants.ApiErrorCodes.InvalidComputedField, $"Computed field {ValidationFieldFormatter.Quote(cf.Name)}: {message}"));
        }

        return errors.Count == 0 ? [] : errors;
    }

    private static List<ApiError> CollectProjectionFieldIssuesList(Type rootType, IReadOnlyList<ProjectedFieldSpec> specs, bool allowSelectWildcards)
    {
        var list = new List<ApiError>();
        foreach (var spec in specs) {
            if (!allowSelectWildcards && spec.NormalizedParts.Any(p => p == "*")) {
                list.Add(new(Constants.ApiErrorCodes.InvalidSelectField,
                    $"Projection path '{spec.RequestedPath}' uses wildcards but wildcard selection is disabled in API query configuration."));
                continue;
            }

            if (spec.NormalizedParts.Length == 0) {
                list.Add(new(Constants.ApiErrorCodes.InvalidSelectField,
                    $"Projection path '{spec.RequestedPath}' is not valid (empty)."));
                continue;
            }

            if (spec.NormalizedParts.Length == 1 && spec.NormalizedParts[0] == "*") {
                if (!allowSelectWildcards) {
                    list.Add(new(Constants.ApiErrorCodes.InvalidSelectField,
                        $"Projection path '{spec.RequestedPath}' is not valid (wildcard-only)."));
                }

                continue;
            }

            var currentType = rootType;
            for (var i = 0; i < spec.NormalizedParts.Length; i++) {
                var part = spec.NormalizedParts[i];
                if (part == "*" && i == spec.NormalizedParts.Length - 1)
                    break;

                if (part.Equals("count", StringComparison.OrdinalIgnoreCase) && i > 0) {
                    if (!SharedEntityMetadataCache.IsCollectionType(currentType)) {
                        list.Add(new(Constants.ApiErrorCodes.InvalidSelectField,
                            $"Segment 'count' in path '{spec.RequestedPath}' is invalid: type '{GetProjectionEntityDisplayName(currentType)}' is not a collection."));
                    }

                    break;
                }

                if (SharedEntityMetadataCache.IsCollectionType(currentType))
                    currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

                var property = SharedEntityMetadataCache.ResolveProperty(currentType, part);
                if (property == null) {
                    list.Add(new(Constants.ApiErrorCodes.InvalidSelectField,
                        $"Entity '{GetProjectionEntityDisplayName(currentType)}' does not have field '{part}' (in path '{spec.RequestedPath}')."));
                    break;
                }

                currentType = property.PropertyType;
            }
        }

        return list;
    }

    private static string GetProjectionEntityDisplayName(Type type) => type.Name;

    private static Expression? BuildPropertyExpression(ParameterExpression parameter, Type rootType, string[] parts, int partIndex)
    {
        if (partIndex >= parts.Length)
            return null;

        var currentType = rootType;
        if (SharedEntityMetadataCache.IsCollectionType(currentType))
            currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

        var property = SharedEntityMetadataCache.ResolveProperty(currentType, parts[partIndex]);
        if (property == null)
            return null;

        var propExpr = Expression.Property(parameter, property);
        var propType = property.PropertyType;
        if (!SharedEntityMetadataCache.IsCollectionType(propType))
            return BuildPropertyExpressionRecursive(propExpr, propType, parts, partIndex + 1);

        if (partIndex + 1 < parts.Length && parts[partIndex + 1].Equals("count", StringComparison.OrdinalIgnoreCase)) {
            var countElementType = SharedEntityMetadataCache.GetCollectionElementType(propType);
            var asQueryableMethod = SharedEntityMetadataCache.GetProjectionAsQueryableMethod(countElementType);
            var queryableExpr = Expression.Call(asQueryableMethod, propExpr);
            var countMethod = SharedEntityMetadataCache.GetProjectionQueryableCountMethod(countElementType);
            var countExpr = Expression.Call(countMethod, queryableExpr);
            return Expression.Convert(countExpr, typeof(object));
        }

        var elementType = SharedEntityMetadataCache.GetCollectionElementType(propType);
        var innerParam = Expression.Parameter(elementType, "x");
        var innerExpr = BuildPropertyExpression(innerParam, elementType, parts, partIndex + 1);
        if (innerExpr == null)
            return null;

        var delegateType = typeof(Func<,>).MakeGenericType(elementType, innerExpr.Type);
        var selector = Expression.Lambda(delegateType, innerExpr, innerParam);
        var selectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, innerExpr.Type);

        return Expression.Call(selectMethod, propExpr, selector);
    }

    private static Expression? BuildPropertyExpressionRecursive(Expression current, Type currentType, string[] parts, int partIndex)
    {
        if (partIndex >= parts.Length)
            return current;

        if (SharedEntityMetadataCache.IsCollectionType(currentType))
            currentType = SharedEntityMetadataCache.GetCollectionElementType(currentType);

        var property = SharedEntityMetadataCache.ResolveProperty(currentType, parts[partIndex]);
        if (property == null)
            return null;

        var propExpr = Expression.Property(current, property);
        var propType = property.PropertyType;
        if (!SharedEntityMetadataCache.IsCollectionType(propType))
            return BuildPropertyExpressionRecursive(propExpr, propType, parts, partIndex + 1);

        if (partIndex + 1 < parts.Length && parts[partIndex + 1].Equals("count", StringComparison.OrdinalIgnoreCase)) {
            var countElementType = SharedEntityMetadataCache.GetCollectionElementType(propType);
            var asQueryableMethod = SharedEntityMetadataCache.GetProjectionAsQueryableMethod(countElementType);
            var queryableExpr = Expression.Call(asQueryableMethod, propExpr);
            var countMethod = SharedEntityMetadataCache.GetProjectionQueryableCountMethod(countElementType);
            var countExpr = Expression.Call(countMethod, queryableExpr);
            return Expression.Convert(countExpr, typeof(object));
        }

        var elementType = SharedEntityMetadataCache.GetCollectionElementType(propType);
        var innerParam = Expression.Parameter(elementType, "x");
        var innerExpr = BuildPropertyExpressionRecursive(innerParam, elementType, parts, partIndex + 1);
        if (innerExpr == null)
            return null;

        var delegateType = typeof(Func<,>).MakeGenericType(elementType, innerExpr.Type);
        var selector = Expression.Lambda(delegateType, innerExpr, innerParam);
        var selectMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, innerExpr.Type);

        return Expression.Call(selectMethod, propExpr, selector);
    }

    private static Func<object, object?> CompileAnonymousFieldGetter(PropertyInfo p)
    {
        var declaring = p.DeclaringType!;
        var param = Expression.Parameter(typeof(object), "o");
        var cast = Expression.Convert(param, declaring);
        var access = Expression.Property(cast, p);
        var body = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(body, param).Compile();
    }

    private static Type CreateAnonymousProjectionType(int fieldCount)
        => fieldCount switch {
            2 => new { V0 = (object?)null, V1 = (object?)null }.GetType(),
            3 => new { V0 = (object?)null, V1 = (object?)null, V2 = (object?)null }.GetType(),
            4 => new {
                V0 = (object?)null,
                V1 = (object?)null,
                V2 = (object?)null,
                V3 = (object?)null
            }.GetType(),
            5 => new {
                V0 = (object?)null,
                V1 = (object?)null,
                V2 = (object?)null,
                V3 = (object?)null,
                V4 = (object?)null
            }.GetType(),
            6 => new {
                V0 = (object?)null,
                V1 = (object?)null,
                V2 = (object?)null,
                V3 = (object?)null,
                V4 = (object?)null,
                V5 = (object?)null
            }.GetType(),
            7 => new {
                V0 = (object?)null,
                V1 = (object?)null,
                V2 = (object?)null,
                V3 = (object?)null,
                V4 = (object?)null,
                V5 = (object?)null,
                V6 = (object?)null
            }.GetType(),
            8 => new {
                V0 = (object?)null,
                V1 = (object?)null,
                V2 = (object?)null,
                V3 = (object?)null,
                V4 = (object?)null,
                V5 = (object?)null,
                V6 = (object?)null,
                V7 = (object?)null
            }.GetType(),
            var _ => throw new ArgumentOutOfRangeException(nameof(fieldCount), fieldCount, "Must be 2-8")
        };

    private static Expression BuildMultiValueProjection(IReadOnlyList<Expression> expressions)
    {
        if (expressions.Count == 1)
            return expressions[0];

        var n = Math.Min(expressions.Count, 8);
        var anonType = AnonymousProjectionTypes[n];
        var ctor = anonType.GetConstructors().First(c => c.GetParameters().Length == n);
        var args = expressions.Take(n).ToArray();
        var newExpr = Expression.New(ctor, args);
        return Expression.Convert(newExpr, typeof(object));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNavigationType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return !IsSimpleType(t) && !SharedEntityMetadataCache.IsCollectionType(t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSimpleType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid) || t == typeof(DateTime) || t == typeof(DateTimeOffset) ||
            t == typeof(TimeSpan) || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(byte[]);
    }

    private static Dictionary<string, object?> PromoteToDictionary(object item, IReadOnlyList<ProjectedFieldSpec> specs)
    {
        if (item is Dictionary<string, object?> dict)
            return dict;

        if (item is IReadOnlyDictionary<string, object?> roDict)
            return new(roDict, StringComparer.OrdinalIgnoreCase);

        // Single-field projection: wrap the scalar value with the spec's requested path as key
        var result = new Dictionary<string, object?>(1, StringComparer.OrdinalIgnoreCase);
        if (specs.Count == 1)
            result[specs[0].RequestedPath] = item;

        return result;
    }

    private string FormatComputedValue(string template, Dictionary<string, object?> row)
    {
        if (formatterService is null || string.IsNullOrWhiteSpace(template))
            return template ?? string.Empty;
        
        var placeholders = GetTemplatePlaceholders(template);
        if (placeholders.Count == 0)
            return template;

        var trimmed = template.Trim();

        // Entire template is a bare dotted path (no braces), e.g. contactaddresses.address.streetname
        if (placeholders.Count == 1 && string.Equals(placeholders[0], trimmed, StringComparison.OrdinalIgnoreCase)) {
            if (TryResolveRowValueForTemplate(row, placeholders[0], out var val))
                return FormatResolvedValueThroughFormatter(val, optionalFormatSuffix: "");

            return string.Empty;
        }

        // No '.' in placeholder names: SmartFormat resolves placeholders from the row as a flat dictionary (incl. format specs like {CreatedAt:yyyy-MM-dd}).
        if (placeholders.All(p => !p.Contains('.')))
            return formatterService.Format(trimmed, row);

        // SmartFormat treats "a.b" in `{a.b}` as nested selectors, not a flat dictionary key. Resolve each segment with
        // <see cref="TryResolveRowValueForTemplate" />, then format through <see cref="IFormatterService" /> using a synthetic key so dots in paths are not parsed as nesting.
        var result = trimmed;
        foreach (var ph in placeholders.OrderByDescending(p => p.Length)) {
            result = Regex.Replace(result, @"\{\s*" + Regex.Escape(ph) + @"(\:[^}]+)?\s*\}", m => {
                if (!TryResolveRowValueForTemplate(row, ph, out var val))
                    return string.Empty;

                var fmtSuffix = m.Groups[1].Success ? m.Groups[1].Value : "";
                return FormatResolvedValueThroughFormatter(val, fmtSuffix);
            }, RegexOptions.IgnoreCase);
        }

        return result;
    }

    /// <summary>Formats a resolved value with <see cref="IFormatterService" /> (SmartFormat). <paramref name="optionalFormatSuffix" /> is the optional piece after the placeholder name, e.g. <c>:yyyy-MM-dd</c>, or empty.</summary>
    private string FormatResolvedValueThroughFormatter(object? val, string optionalFormatSuffix)
    {
        if (val is null)
            return string.Empty;

        const string syntheticKey = "__p";
        var miniTemplate = "{" + syntheticKey + optionalFormatSuffix + "}";
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { [syntheticKey] = val };
        return formatterService!.Format(miniTemplate, ctx);
    }

    /// <summary>Letters/digits only, lowercased — aligns tokens like {lastname} with projected keys LastName.</summary>
    private static string CompactAlphanumericLower(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        Span<char> buffer = stackalloc char[key.Length];
        var j = 0;
        foreach (var c in key) {
            if (char.IsLetterOrDigit(c))
                buffer[j++] = char.ToLowerInvariant(c);
        }

        return j == 0 ? "" : new(buffer[..j]);
    }

    private static bool TryResolveRowValueForTemplate(Dictionary<string, object?> row, string token, out object? val)
    {
        if (row.TryGetValue(token, out val))
            return true;

        var tokenNorm = CompactAlphanumericLower(token);
        if (tokenNorm.Length == 0) {
            val = null;
            return false;
        }

        foreach (var kv in row) {
            if (CompactAlphanumericLower(kv.Key) == tokenNorm) {
                val = kv.Value;
                return true;
            }
        }

        if (TryResolveTokenFromCollectionScopeWildcard(row, token, out val))
            return true;

        return TryResolveTokenFromWildcardSiblingColumn(row, token, out val);
    }

    /// <summary>
    /// Template path <c>contactaddresses.address.streettype</c> when the row only has <c>contactaddresses.*</c>: walk each element with segments after the collection.
    /// </summary>
    private static bool TryResolveTokenFromCollectionScopeWildcard(Dictionary<string, object?> row, string token, out object? val)
    {
        val = null;
        var tokenParts = token.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokenParts.Length < 3)
            return false;

        var scopeWildcardKey = string.Concat(tokenParts[0], ".*");
        if (!row.TryGetValue(scopeWildcardKey, out var collObj) || collObj is string or byte[] || collObj is not IEnumerable enumerable)
            return false;

        var relativeParts = tokenParts.Skip(1).ToArray();
        var list = new List<object?>();
        foreach (var el in enumerable)
            list.Add(GetNestedValueFromProjectedElement(el, relativeParts));

        val = list;
        return true;
    }

    private static object? GetNestedValueFromProjectedElement(object? el, string[] relativeParts)
    {
        object? current = el;
        foreach (var seg in relativeParts) {
            if (current is null)
                return null;

            current = GetLeafFromProjectedElement(current, seg);
        }

        return current;
    }

    /// <summary>
    /// When the row has <c>prefix.*</c> (wildcard projection) but the template references <c>prefix.leaf</c>, build a parallel list of leaf values per element.
    /// </summary>
    private static bool TryResolveTokenFromWildcardSiblingColumn(Dictionary<string, object?> row, string token, out object? val)
    {
        val = null;
        var lastDot = token.LastIndexOf('.');
        if (lastDot <= 0)
            return false;

        var wildcardKey = string.Concat(token.AsSpan(0, lastDot), ".*");
        if (!row.TryGetValue(wildcardKey, out var collectionObj))
            return false;

        if (collectionObj is string or byte[] || collectionObj is not IEnumerable enumerable)
            return false;

        var leaf = token[(lastDot + 1)..];
        var list = new List<object?>();
        foreach (var el in enumerable) {
            if (el is null) {
                list.Add(null);
                continue;
            }

            list.Add(GetLeafFromProjectedElement(el, leaf));
        }

        val = list;
        return true;
    }

    private static object? GetLeafFromProjectedElement(object el, string leafSegment)
    {
        if (el is Dictionary<string, object?> d) {
            foreach (var kv in d) {
                if (string.Equals(kv.Key, leafSegment, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return null;
        }

        var t = el.GetType();
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (string.Equals(p.Name, leafSegment, StringComparison.OrdinalIgnoreCase))
                return p.GetValue(el);
        }

        return null;
    }

    private sealed record IncludeScopeSpec(string CollectionPath, string[] PrefixParts, PropertyInfo CollectionProperty);

    private static class ComparisonEvaluator
    {
        public static bool Evaluate(object? actualValue, ComparisonOperatorEnum comparator, object? expectedValue)
        {
            if (actualValue is not IEnumerable enumerable || actualValue is string or byte[])
                return EvaluateScalar(actualValue, comparator, expectedValue);

            var values = enumerable.Cast<object?>().ToList();
            if (values.Count == 0)
                return false;

            var isNegated = comparator is ComparisonOperatorEnum.NotEquals or ComparisonOperatorEnum.NotContains or ComparisonOperatorEnum.NotStartsWith or ComparisonOperatorEnum.NotEndsWith
                or ComparisonOperatorEnum.NotIn or ComparisonOperatorEnum.NotRegex;

            return isNegated ? values.All(v => EvaluateScalar(v, comparator, expectedValue)) : values.Any(v => EvaluateScalar(v, comparator, expectedValue));
        }

        private static bool EvaluateScalar(object? actualValue, ComparisonOperatorEnum comparator, object? expectedValue)
        {
            var actualText = actualValue?.ToString();
            var expectedText = expectedValue?.ToString();
            return comparator switch {
                ComparisonOperatorEnum.Equals => string.Equals(actualText, expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.NotEquals => !string.Equals(actualText, expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.Contains => actualText != null && expectedText != null && actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.NotContains => actualText == null || expectedText == null || !actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.StartsWith => actualText != null && expectedText != null && actualText.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.NotStartsWith => actualText == null || expectedText == null || !actualText.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.EndsWith => actualText != null && expectedText != null && actualText.EndsWith(expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.NotEndsWith => actualText == null || expectedText == null || !actualText.EndsWith(expectedText, StringComparison.OrdinalIgnoreCase),
                ComparisonOperatorEnum.In => SplitExpected(expectedValue).Any(i => string.Equals(i, actualText, StringComparison.OrdinalIgnoreCase)),
                ComparisonOperatorEnum.NotIn => SplitExpected(expectedValue).All(i => !string.Equals(i, actualText, StringComparison.OrdinalIgnoreCase)),
                var _ => true
            };
        }

        private static IReadOnlyList<string> SplitExpected(object? expectedValue)
        {
            if (expectedValue is IEnumerable enumerable and not string)
                return enumerable.Cast<object?>().Select(i => i?.ToString()).Where(i => !string.IsNullOrWhiteSpace(i)).Cast<string>().ToList();

            var text = expectedValue?.ToString() ?? "";
            return text.Length == 0 ? [] : text.Split(',').Select(i => i.Trim()).Where(i => i.Length > 0).ToList();
        }
    }
}