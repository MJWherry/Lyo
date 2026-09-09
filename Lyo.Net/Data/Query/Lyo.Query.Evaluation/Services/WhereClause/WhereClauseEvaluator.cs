using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Common.Core.Caching;
using Lyo.Common.Core.Enums;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Parameters;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.ValueConversion;

namespace Lyo.Query.Services.WhereClause;

internal enum ComparisonType
{
    SimpleComparison,
    StringMethod,
    NegatedStringMethod,
    OneOf,
    NotOneOf,
    Regex,
    NotRegex
}

[DebuggerDisplay("{ToString(),nq}")]
internal record ComparisonMetadata(ComparisonType ComparisonType, ExpressionType? ExpressionType = null, MethodInfo? ToLowerMethod = null, MethodInfo? StringMethod = null)
{
    public override string ToString() => $"ComparisonType={ComparisonType.ToString()} ExpType={ExpressionType}";
}

[DebuggerDisplay("{ToString(),nq}")]
internal record CollectionMetadata(bool IsCollection, Type ElementType)
{
    public override string ToString() => $"IsCollection={IsCollection} ElementType={ElementType.FullName}";
}

[DebuggerDisplay("{ToString(),nq}")]
internal record PropertyPathMetadata(IReadOnlyList<PropertyInfo> Properties, Type FinalType, int? CollectionPropertyIndex, Type? CollectionElementType, bool IsCountPath = false)
{
    public override string ToString() => $"PropertyCount={Properties.Count} CollectionIndex={CollectionPropertyIndex} FinalType={FinalType.FullName} IsCountPath={IsCountPath}";
}

/// <summary>
/// Default <see cref="IWhereClauseService" />: builds LINQ expression trees for <see cref="Lyo.Query.Models.Common.WhereClause" />, memoizes predicates and matchers, and
/// supports in-memory evaluation plus <see cref="ExplainMatch{TEntity}" /> for loaded entities. Durations, success counters, and errors are recorded under
/// <see cref="Lyo.Query.Constants.Metrics" />.
/// </summary>
/// <remarks>
/// Needs only <see cref="IValueConversionService" />; reflection metadata comes from <see cref="SharedEntityMetadataCache" /> and compiled artifacts are memoized in
/// process-wide <see cref="ConcurrentDictionary{TKey,TValue}" /> instances. Hosts that want a shared or evictable cache override <see cref="GetMatcher{TEntity}" />,
/// <see cref="GetEfPredicate{TEntity}" />, and <see cref="GetIncludePaths{TEntity}" />.
/// </remarks>
public class WhereClauseEvaluator : IWhereClauseService
{
    /// <summary>
    /// Cap on a caller-supplied regex pattern, shared with <see cref="LyoParameterValidator.MaxValidationRegexLength" /> so both validation surfaces accept the same
    /// patterns.
    /// </summary>
    public const int MaxRegexPatternLength = LyoParameterValidator.MaxValidationRegexLength;

    /// <summary>
    /// Entry cap on each of the three process-wide compiled-artifact caches (matchers, EF predicates, include paths). Hitting it evicts the coldest quarter; a clause shape that
    /// falls out is recompiled on its next use.
    /// </summary>
    public const int MaxCompiledArtifactCacheEntries = 2048;

    private const string MatcherCachePrefix = "filter_matcher";
    private const string EfPredicateCachePrefix = "filter_ef_predicate";
    private const BindingFlags PropertySearchFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Match timeout for in-memory regex evaluation, shared with <see cref="LyoParameterValidator.RegexMatchTimeout" />.</summary>
    protected static readonly TimeSpan RegexMatchTimeout = LyoParameterValidator.RegexMatchTimeout;

    private static readonly MethodInfo ObjectToStringMethod = typeof(object).GetMethod(nameof(ToString), Type.EmptyTypes)!;

    /// <summary>
    /// Case folding for the <see cref="IQueryable{T}" /> path. <c>ToLower()</c> is the only overload EF translates (to SQL <c>lower()</c>, which is invariant server-side);
    /// <see cref="string.ToLowerInvariant" /> raises "could not be translated" at query time.
    /// </summary>
    private static readonly MethodInfo StringToLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;

    /// <summary>
    /// Case folding for compiled in-memory matchers, swapped in by <see cref="InMemoryEvaluationRewriter" />. Running <c>ToLower()</c> in this process would make the result
    /// depend on the server's locale, and the Turkish dotless-i mapping would make <c>"I"</c> and <c>"i"</c> stop matching.
    /// </summary>
    private static readonly MethodInfo StringToLowerInvariantMethod = typeof(string).GetMethod(nameof(string.ToLowerInvariant), Type.EmptyTypes)!;

    private static readonly MethodInfo RegexIsMatchMethod = typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo RegexIsMatchWithTimeoutMethod =
        typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string), typeof(string), typeof(RegexOptions), typeof(TimeSpan)])!;

    private static readonly MethodInfo[] QueryableOrderMethods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
        .ToArray();

    // Keys embed caller-supplied filter values, and every matcher entry pins a DynamicMethod the GC never reclaims, so these stay capped rather than unbounded.
    private static readonly BoundedCache<string, object> MatcherCache = new(MaxCompiledArtifactCacheEntries);
    private static readonly BoundedCache<string, object?> EfPredicateCache = new(MaxCompiledArtifactCacheEntries);
    private static readonly BoundedCache<string, IReadOnlyList<string>> IncludePathCache = new(MaxCompiledArtifactCacheEntries);

    /// <summary>Converts filter literals to the CLR type of the compared property.</summary>
    protected readonly IValueConversionService ValueConversion;

    private readonly IMetrics _metrics;

    /// <summary>Builds a <see cref="WhereClauseEvaluator" />.</summary>
    /// <param name="valueConversion">Converts filter literals to property types.</param>
    /// <param name="metrics">Optional metrics sink; starts as <see cref="NullMetrics.Instance" />, whose recording and timers allocate nothing.</param>
    public WhereClauseEvaluator(IValueConversionService valueConversion, IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(valueConversion);
        ValueConversion = valueConversion;
        _metrics = metrics ?? NullMetrics.Instance;
    }

    /// <inheritdoc />
    public virtual bool MatchesWhereClause<TEntity>(TEntity entity, Models.Common.WhereClause? queryNode)
    {
        if (entity is null)
            return false;

        // A null clause is a vacuous match, matching Match<TEntity> and the interface contract. Returning false here made "no filter" mean "matches nothing".
        if (queryNode is null)
            return true;

        using var timer = _metrics.StartTimer(Constants.Metrics.MatchesWhereClauseDuration, EntityTypeTag<TEntity>.Tags);
        try {
            var result = GetMatcher<TEntity>(queryNode)(entity);
            _metrics.IncrementCounter(Constants.Metrics.MatchesWhereClauseSuccess, 1, EntityTypeTag<TEntity>.Tags);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.MatchesWhereClauseDuration, ex, EntityTypeTag<TEntity>.Tags);
            throw;
        }
    }

    /// <inheritdoc cref="IWhereClauseService.ExplainMatch{TEntity}" />
    public virtual WhereClauseExplainResult ExplainMatch<TEntity>(TEntity entity, Models.Common.WhereClause? queryNode)
    {
        if (entity is null)
            return new(new() { Passed = false, Kind = WhereClauseExplainKind.None, Path = "" }, null, "Entity is null.");

        if (queryNode is null)
            return new(new() { Passed = false, Kind = WhereClauseExplainKind.None, Path = "" }, null, "Where clause is null.");

        var root = BuildExplainNode<TEntity>(entity, queryNode, "", 0);
        var (blockingPath, failureSummary) = WhereClauseExplainAnalysis.ComputeBlockingFailure(root);
        var orBranches = root.Passed ? null : WhereClauseExplainAnalysis.CollectOrBranchOutcomes(root);
        return new(root, blockingPath, failureSummary, orBranches);
    }

    /// <inheritdoc />
    /// <remarks>
    /// An <see cref="EnumerableQuery" /> source runs the predicate in this process rather than in the database, so it gets the same rewrite the compiled matchers get: a match
    /// timeout on regex and invariant case folding. Without it a caller-supplied catastrophic pattern pinned a core until the request was abandoned.
    /// </remarks>
    public virtual IQueryable<TEntity> ApplyWhereClause<TEntity>(IQueryable<TEntity> source, Models.Common.WhereClause? queryNode, bool includeSubClauses = true)
    {
        if (queryNode is null)
            return source;

        using var timer = _metrics.StartTimer(Constants.Metrics.ApplyWhereClauseDuration, EntityTypeTag<TEntity>.Tags);
        try {
            var expression = GetEfPredicate<TEntity>(queryNode, includeSubClauses);
            if (expression != null && source.Provider is EnumerableQuery)
                expression = (Expression<Func<TEntity, bool>>)InMemoryEvaluationRewriter.Instance.Visit(expression);

            var result = expression != null ? source.Where(expression) : source;
            _metrics.IncrementCounter(Constants.Metrics.ApplyWhereClauseSuccess, 1, EntityTypeTag<TEntity>.Tags);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.ApplyWhereClauseDuration, ex, EntityTypeTag<TEntity>.Tags);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual IQueryable<TEntity> SortByProperty<TEntity>(IQueryable<TEntity> source, string propertyName, SortDirection? direction = null)
    {
        if (string.IsNullOrEmpty(propertyName))
            throw new InvalidQueryException("Property name cannot be null or empty.");

        using var timer = _metrics.StartTimer(Constants.Metrics.SortByPropertyDuration, EntityTypeTag<TEntity>.Tags);
        try {
            var sortKeyCacheKey = $"{MetadataCacheKeys.SortKeyLambdaPrefix}{typeof(TEntity).FullName}:{propertyName}";
            var (lambda, keyType) = SharedEntityMetadataCache.GetOrAddSortKeyLambda(
                sortKeyCacheKey, () => {
                    var parameter = Expression.Parameter(typeof(TEntity), "x");
                    var (keySelector, kt) = BuildKeySelector<TEntity>(propertyName, parameter);
                    return (Expression.Lambda(keySelector, parameter), kt);
                });

            var methodName = GetOrderingMethodName(source, direction ?? SortDirection.Desc);
            var orderMethod = GetQueryableOrderMethodCached<TEntity>(methodName, keyType);
            var result = (IQueryable<TEntity>)orderMethod.Invoke(null, [source, lambda])!;
            _metrics.IncrementCounter(Constants.Metrics.SortByPropertySuccess, 1, EntityTypeTag<TEntity>.Tags);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.SortByPropertyDuration, ex, EntityTypeTag<TEntity>.Tags);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual IQueryable<TEntity> ApplyOrdering<TEntity>(
        IQueryable<TEntity> queryable,
        IEnumerable<SortBy> sortByProps,
        Expression<Func<TEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection)
    {
        var byProps = sortByProps as SortBy[] ?? sortByProps.ToArray();
        using var timer = _metrics.StartTimer(Constants.Metrics.ApplyOrderingDuration, EntityTypeTag<TEntity>.Tags);
        try {
            IQueryable<TEntity> result;
            if (!byProps.Any())
                result = defaultSortDirection == SortDirection.Desc ? queryable.OrderByDescending(defaultOrder) : queryable.OrderBy(defaultOrder);
            else {
                var ordered = byProps.Select((s, i) => (SortBy: s, EffectivePriority: s.Priority ?? i))
                    .OrderBy(x => x.EffectivePriority)
                    .Aggregate(queryable, (current, x) => SortByProperty(current, x.SortBy.PropertyName, x.SortBy.Direction));

                // Callers pass the primary-key selector as the default order. Appending it as the last tiebreaker makes the row order total, so paging over a non-unique sort
                // column stops duplicating rows on one page and skipping them on the next.
                result = ((IOrderedQueryable<TEntity>)ordered).ThenBy(defaultOrder);
            }

            _metrics.IncrementCounter(Constants.Metrics.ApplyOrderingSuccess, 1, EntityTypeTag<TEntity>.Tags);
            _metrics.RecordGauge(Constants.Metrics.SortByCount, byProps.Length, EntityTypeTag<TEntity>.Tags);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.ApplyOrderingDuration, ex, EntityTypeTag<TEntity>.Tags);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual IEnumerable<string> GetCollectionIncludePathsForWhereClause<TEntity>(Models.Common.WhereClause? queryNode)
        => queryNode == null ? [] : GetIncludePaths<TEntity>(queryNode);

    /// <inheritdoc />
    public virtual bool TryValidatePropertyPath<TEntity>(string propertyName, out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(propertyName)) {
            errorMessage = "Property name cannot be null or empty.";
            return false;
        }

        try {
            _ = GetPropertyPathMetadataCached<TEntity>(propertyName);
            return true;
        }
        catch (Exception) {
            // The message reaches API responses, so it names only the caller's own input. Reflection and type detail stay out of it.
            errorMessage = $"Property path '{propertyName}' is not valid for this entity.";
            return false;
        }
    }

    /// <remarks>
    /// Every node resolves through the memoized matcher. Building and compiling a fresh delegate per node made an explain cost one JIT compilation per node plus another for
    /// each sub-clause parent, so a 50-node tree paid 50+ compilations on every call.
    /// </remarks>
    private WhereClauseExplainNode BuildExplainNode<TEntity>(TEntity entity, Models.Common.WhereClause node, string path, int depth)
    {
        if (depth > WhereClauseHelpers.MaxClauseDepth)
            throw new InvalidQueryException($"Where clause nests deeper than the supported limit of {WhereClauseHelpers.MaxClauseDepth}.");

        var passed = GetMatcher<TEntity>(node)(entity);
        var description = node.Description;
        switch (node) {
            case ConditionClause c: {
                bool? primaryPredicatePassed = null;
                WhereClauseExplainNode? subExplain = null;
                if (c.SubClause != null) {
                    // Attribute the outcome to the leaf or to its sub-clause. Cloning without the sub-clause reuses the memoized matcher for that shape.
                    primaryPredicatePassed = GetMatcher<TEntity>(new ConditionClause(c.Field, c.Comparison, c.Value, c.Description))(entity);
                    subExplain = BuildExplainNode(entity, c.SubClause, string.IsNullOrEmpty(path) ? "sub" : $"{path}/sub", depth + 1);
                }

                return new() {
                    Passed = passed,
                    Kind = WhereClauseExplainKind.Condition,
                    Path = path,
                    Description = description,
                    Field = c.Field,
                    Comparison = c.Comparison,
                    FilterValue = c.Value,
                    ActualValueSummary = TryFormatActualValueSummary(entity, c.Field),
                    PrimaryPredicatePassed = primaryPredicatePassed,
                    SubClause = subExplain
                };
            }
            case GroupClause g: {
                var children = new WhereClauseExplainNode[g.Children.Count];
                for (var i = 0; i < g.Children.Count; i++) {
                    var childPath = string.IsNullOrEmpty(path) ? i.ToString() : $"{path}/{i}";
                    children[i] = BuildExplainNode(entity, g.Children[i], childPath, depth + 1);
                }

                var subExplain = g.SubClause != null ? BuildExplainNode(entity, g.SubClause, string.IsNullOrEmpty(path) ? "sub" : $"{path}/sub", depth + 1) : null;
                return new() {
                    Passed = passed,
                    Kind = WhereClauseExplainKind.Group,
                    Path = path,
                    Description = description,
                    GroupOperator = g.Operator,
                    Children = children,
                    SubClause = subExplain
                };
            }
            default:
                throw new InvalidQueryException($"Unknown query node type: {node.GetType().Name}");
        }
    }

    private string? TryFormatActualValueSummary<TEntity>(TEntity entity, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        try {
            var meta = GetPropertyPathMetadataCached<TEntity>(field);
            return FormatPropertyPathValue(entity, meta);
        }
        catch (Exception) {
            // Explain output is diagnostic: an unreadable field degrades to "no summary" rather than failing the explain.
            return null;
        }
    }

    private static string? FormatPropertyPathValue<TEntity>(TEntity entity, PropertyPathMetadata meta)
    {
        if (meta.IsCountPath && meta.CollectionPropertyIndex is int countCollectionIndex) {
            object? cur = entity;
            for (var i = 0; i <= countCollectionIndex; i++) {
                if (cur == null)
                    return "null";

                cur = GetMemberValue(cur, meta.Properties[i]);
            }

            if (cur is IEnumerable e and not string)
                return e.Cast<object?>().Count().ToString();

            return cur?.ToString();
        }

        if (meta.CollectionPropertyIndex is int colIdx) {
            object? cur = entity;
            for (var i = 0; i <= colIdx; i++) {
                if (cur == null)
                    return "null";

                cur = GetMemberValue(cur, meta.Properties[i]);
            }

            if (cur is not IEnumerable enumerable || cur is string || cur is byte[])
                return cur?.ToString();

            var tail = meta.Properties.Skip(colIdx + 1).ToList();
            if (tail.Count == 0)
                return $"({CountEnumerable(enumerable)} items)";

            const int maxSamples = 24;
            var samples = new List<string>();
            var total = 0;
            foreach (var item in enumerable) {
                total++;
                if (samples.Count >= maxSamples)
                    continue;

                if (item == null) {
                    samples.Add("null");
                    continue;
                }

                var leaf = item;
                foreach (var p in tail) {
                    if (leaf == null)
                        break;

                    leaf = GetMemberValue(leaf, p);
                }

                samples.Add(leaf?.ToString() ?? "");
            }

            if (total == 0)
                return "(empty collection)";

            var joined = string.Join(", ", samples);
            var more = total > maxSamples ? $" … (+{total - maxSamples} more)" : "";
            return joined + more;
        }

        object? s = entity;
        foreach (var p in meta.Properties) {
            if (s == null)
                return "null";

            s = GetMemberValue(s, p);
        }

        return s?.ToString() ?? "null";
    }

    private static int CountEnumerable(IEnumerable enumerable)
    {
        if (enumerable is ICollection c)
            return c.Count;

        var n = 0;
        foreach (var _ in enumerable)
            n++;

        return n;
    }

    private static object? GetMemberValue(object? instance, PropertyInfo prop)
    {
        if (instance == null)
            return null;

        return prop.GetValue(instance);
    }

    private PropertyInfo? ResolvePropertyCached(Type type, string name) => SharedEntityMetadataCache.ResolveProperty(type, name);

    private static IEnumerable<string> CollectConditionFields(Models.Common.WhereClause node)
        => node switch {
            ConditionClause condition => CollectFromCondition(condition),
            GroupClause logical => CollectFromLogical(logical),
            var _ => []
        };

    private static IEnumerable<string> CollectFromCondition(ConditionClause condition)
    {
        if (!string.IsNullOrWhiteSpace(condition.Field))
            yield return condition.Field;

        if (condition.SubClause is null)
            yield break;

        foreach (var f in CollectConditionFields(condition.SubClause))
            yield return f;
    }

    private static IEnumerable<string> CollectFromLogical(GroupClause logical)
    {
        foreach (var child in logical.Children.SelectMany(CollectConditionFields))
            yield return child;

        if (logical.SubClause == null)
            yield break;

        foreach (var f in CollectConditionFields(logical.SubClause))
            yield return f;
    }

    /// <summary>
    /// Builds a LINQ predicate expression for <paramref name="queryNode" /> without applying it to an <see cref="IQueryable{T}" />. Override in derived services that customize
    /// translation.
    /// </summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree, or <c>null</c>.</param>
    /// <param name="includeSubClauses">When <c>false</c>, omits <see cref="Lyo.Query.Models.Common.WhereClause.SubClause" /> chains from the expression.</param>
    /// <returns>A lambda <c>x => …</c>, or <c>null</c> when <paramref name="queryNode" /> is null.</returns>
    public virtual Expression<Func<TEntity, bool>>? BuildExpressionFromWhereClause<TEntity>(Models.Common.WhereClause? queryNode, bool includeSubClauses = true)
    {
        if (queryNode == null)
            return null;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var body = BuildWhereClauseExpression<TEntity>(queryNode, parameter, includeSubClauses, 0);
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    /// <summary>Translates one clause node into an expression, recursing through groups.</summary>
    /// <typeparam name="TEntity">Entity type the parameter refers to.</typeparam>
    /// <param name="node">Clause node to translate.</param>
    /// <param name="parameter">Lambda parameter the property paths are rooted at.</param>
    /// <param name="includeSubClauses">Whether nested sub-clauses contribute to the expression.</param>
    /// <param name="depth">
    /// Current recursion depth. A deeply nested clause would otherwise overflow the stack, which is process-fatal and cannot be caught, so the walk refuses to descend past
    /// <see cref="WhereClauseHelpers.MaxClauseDepth" />.
    /// </param>
    private Expression BuildWhereClauseExpression<TEntity>(Models.Common.WhereClause node, ParameterExpression parameter, bool includeSubClauses, int depth)
    {
        if (depth > WhereClauseHelpers.MaxClauseDepth)
            throw new InvalidQueryException($"Where clause nests deeper than the supported limit of {WhereClauseHelpers.MaxClauseDepth}.");

        return node switch {
            ConditionClause condition => BuildConditionExpression<TEntity>(condition, parameter, includeSubClauses, depth),
            GroupClause logical => BuildLogicalExpression<TEntity>(logical, parameter, includeSubClauses, depth),
            var _ => throw new InvalidQueryException($"Unknown query node type: {node.GetType().Name}")
        };
    }

    /// <summary>
    /// Builds the comparison for a single leaf: resolves the parse type from the property type and operator, rejects an invalid null comparison, parses the literal, and
    /// short-circuits a trivial regex.
    /// </summary>
    /// <remarks>
    /// Both the scalar path and the collection-element paths resolve here. They used to differ: the OR-group collection branch parsed against the property type rather than
    /// the operator-adjusted parse type, so a <c>Contains</c> or a rewritten regex against a Guid collection element tried to read the fragment as a Guid and threw, while the
    /// same filter outside an OR group worked.
    /// </remarks>
    /// <param name="comparison">The comparison operator.</param>
    /// <param name="rawValue">The unparsed filter literal.</param>
    /// <param name="fieldName">Dotted path, used only for error messages.</param>
    /// <param name="propertyType">CLR type of the compared property.</param>
    /// <param name="target">Expression producing the property value.</param>
    /// <param name="parameter">Lambda parameter the comparison is built against.</param>
    private Expression BuildLeafComparison(
        ComparisonOperatorEnum comparison,
        object? rawValue,
        string fieldName,
        Type propertyType,
        Expression target,
        ParameterExpression parameter)
    {
        ValidateNullComparison(comparison, propertyType, rawValue, fieldName);
        var parseType = GetFilterValueParseType(propertyType, comparison);
        var (parsedSingle, parsedMultiple) = ParseFilterValue(rawValue, comparison, parseType);
        if (comparison is ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex && parsedSingle is string pattern && IsTrivialRegex(pattern))
            return Expression.Constant(true);

        var valueExpr = GetRhsConstantExpression(parsedSingle, parseType, target, comparison);
        return BuildComparisonExpressionCached(comparison, propertyType, target, valueExpr, parsedMultiple, parameter);
    }

    private static bool IsTrivialRegex(string? pattern)
    {
        var p = pattern?.Trim();
        return p is { Length: > 0 } && (p == ".*" || p == "^.*$" || p == "\\A.*\\z" || p == "(?s).*");
    }

    private static List<ConditionClause> CombineContainsNodesToRegex(IReadOnlyList<ConditionClause> nodes, Type fieldType)
    {
        if (!SupportsStringMethodComparisons(fieldType))
            return nodes.ToList();

        var containsNodes = nodes.Where(n => n.Comparison == ComparisonOperatorEnum.Contains).ToList();
        if (containsNodes.Count < 2)
            return nodes.ToList();

        var literals = containsNodes.Select(n => ConvertToString(n.Value)).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (literals.Length < 2)
            return nodes.ToList();

        var pattern = $"({string.Join("|", literals.Select(ToCaseInsensitiveLiteralPattern))})";

        // The rewrite is an optimisation, not a requirement: if the fused pattern would exceed the cap that a caller-supplied pattern must satisfy, keep the original Contains
        // nodes rather than synthesising a pattern the evaluator would then reject.
        if (pattern.Length > MaxRegexPatternLength)
            return nodes.ToList();

        var replacement = new ConditionClause(containsNodes[0].Field, ComparisonOperatorEnum.Regex, pattern);
        var optimized = nodes.Where(n => n.Comparison != ComparisonOperatorEnum.Contains).ToList();
        optimized.Add(replacement);
        return optimized;
    }

    private static string ToCaseInsensitiveLiteralPattern(string literal)
    {
        var sb = new StringBuilder(literal.Length * 2);
        foreach (var ch in literal) {
            if (char.IsLetter(ch)) {
                var lower = char.ToLowerInvariant(ch);
                var upper = char.ToUpperInvariant(ch);
                if (lower == upper)
                    sb.Append(Regex.Escape(ch.ToString()));
                else
                    sb.Append('[').Append(lower).Append(upper).Append(']');
            }
            else
                sb.Append(Regex.Escape(ch.ToString()));
        }

        return sb.ToString();
    }

    private Expression BuildConditionExpression<TEntity>(ConditionClause condition, ParameterExpression parameter, bool includeSubClauses, int depth)
    {
        if (string.IsNullOrWhiteSpace(condition.Field))
            throw new InvalidQueryException("Property name cannot be empty.");

        var pathMetadata = GetPropertyPathMetadataCached<TEntity>(condition.Field);
        if (pathMetadata.IsCountPath && pathMetadata.CollectionPropertyIndex != null) {
            var collectionIndex = pathMetadata.CollectionPropertyIndex.Value;
            Expression collectionExpr = parameter;
            for (var i = 0; i <= collectionIndex; i++)
                collectionExpr = Expression.Property(collectionExpr, pathMetadata.Properties[i]);

            var elementType = pathMetadata.CollectionElementType ?? SharedEntityMetadataCache.GetCollectionElementType(collectionExpr.Type);
            var countMethod = GetEnumerableCountMethod(elementType);
            var countExpression = Expression.Call(countMethod, collectionExpr);
            return BuildLeafComparison(condition.Comparison, condition.Value, condition.Field, typeof(int), countExpression, parameter);
        }

        if (pathMetadata.CollectionPropertyIndex != null) {
            var (collectionExpr, elementParam, elementPropExpr, elementType) = BuildCollectionElementAccess(pathMetadata, parameter);
            var comparisonElement = BuildLeafComparison(condition.Comparison, condition.Value, condition.Field, pathMetadata.FinalType, elementPropExpr, elementParam);
            return BuildAnyCall(collectionExpr, elementType, comparisonElement, elementParam);
        }

        var property = GetPropertyExpression<TEntity>(condition.Field, parameter);
        var (targetExpression, propertyType) = AdjustForCollectionCached(property);
        var conditionExpr = BuildLeafComparison(condition.Comparison, condition.Value, condition.Field, propertyType, targetExpression, parameter);
        if (includeSubClauses && condition.SubClause != null)
            return Expression.AndAlso(conditionExpr, BuildWhereClauseExpression<TEntity>(condition.SubClause, parameter, includeSubClauses, depth + 1));

        return conditionExpr;
    }

    /// <summary>Walks a dotted path that crosses a collection navigation, yielding the collection, the element lambda parameter, and the leaf access on that element.</summary>
    private static (Expression CollectionExpr, ParameterExpression ElementParam, Expression ElementPropExpr, Type ElementType) BuildCollectionElementAccess(
        PropertyPathMetadata pathMetadata,
        ParameterExpression parameter)
    {
        var collectionIndex = pathMetadata.CollectionPropertyIndex!.Value;
        var elementType = pathMetadata.CollectionElementType!;
        Expression collectionExpr = parameter;
        for (var i = 0; i <= collectionIndex; i++)
            collectionExpr = Expression.Property(collectionExpr, pathMetadata.Properties[i]);

        var elementParam = Expression.Parameter(elementType, "e");
        Expression elementPropExpr = elementParam;
        for (var j = collectionIndex + 1; j < pathMetadata.Properties.Count; j++)
            elementPropExpr = Expression.Property(elementPropExpr, pathMetadata.Properties[j]);

        return (collectionExpr, elementParam, elementPropExpr, elementType);
    }

    private static Expression BuildAnyCall(Expression collectionExpr, Type elementType, Expression body, ParameterExpression elementParam)
    {
        var anyMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        return Expression.Call(anyMethod, collectionExpr, Expression.Lambda(body, elementParam));
    }

    private Expression BuildLogicalExpression<TEntity>(GroupClause logical, ParameterExpression parameter, bool includeSubClauses, int depth)
    {
        if (logical.Children == null || logical.Children.Count == 0)
            throw new InvalidQueryException("GroupClause must have at least one child");

        if (logical.Operator == GroupOperatorEnum.Or) {
            var remaining = new List<Models.Common.WhereClause>(logical.Children);
            var expressions = new List<Expression>();
            var condNodes = remaining.OfType<ConditionClause>().ToList();
            var groups = condNodes.GroupBy(c => c.Field, StringComparer.OrdinalIgnoreCase);
            foreach (var g in groups) {
                var field = g.Key;
                var nodes = g.ToList();
                if (nodes.Count <= 1)
                    continue;

                var meta = GetPropertyPathMetadataCached<TEntity>(field);
                nodes = CombineContainsNodesToRegex(nodes, meta.FinalType);
                if (meta.CollectionPropertyIndex != null && !meta.IsCountPath) {
                    var (collectionExpr, elementParam, elementPropExpr, elementType) = BuildCollectionElementAccess(meta, parameter);
                    Expression? innerOr = null;
                    foreach (var cond in nodes) {
                        var compExpr = BuildLeafComparison(cond.Comparison, cond.Value, cond.Field, meta.FinalType, elementPropExpr, elementParam);
                        innerOr = innerOr == null ? compExpr : Expression.OrElse(innerOr, compExpr);
                    }

                    if (innerOr == null)
                        continue;

                    expressions.Add(BuildAnyCall(collectionExpr, elementType, innerOr, elementParam));
                    foreach (var rem in g)
                        remaining.Remove(rem);
                }
                else if (nodes.Count != g.Count()) {
                    Expression? groupedOr = null;
                    foreach (var cond in nodes) {
                        var expr = BuildConditionExpression<TEntity>(cond, parameter, includeSubClauses, depth + 1);
                        groupedOr = groupedOr == null ? expr : Expression.OrElse(groupedOr, expr);
                    }

                    if (groupedOr == null)
                        continue;

                    expressions.Add(groupedOr);
                    foreach (var rem in g)
                        remaining.Remove(rem);
                }
            }

            expressions.AddRange(remaining.Select(child => BuildWhereClauseExpression<TEntity>(child, parameter, includeSubClauses, depth + 1)));
            if (expressions.Count == 0)
                throw new InvalidQueryException("Logical OR produced no expressions");

            var combined = expressions[0];
            for (var i = 1; i < expressions.Count; i++)
                combined = Expression.OrElse(combined, expressions[i]);

            if (includeSubClauses && logical.SubClause != null)
                return Expression.AndAlso(combined, BuildWhereClauseExpression<TEntity>(logical.SubClause, parameter, includeSubClauses, depth + 1));

            return combined;
        }

        var childExpressions = logical.Children.Select(child => BuildWhereClauseExpression<TEntity>(child, parameter, includeSubClauses, depth + 1)).ToList();
        var combinedDefault = childExpressions[0];
        for (var i = 1; i < childExpressions.Count; i++) {
            combinedDefault = logical.Operator == GroupOperatorEnum.And
                ? Expression.AndAlso(combinedDefault, childExpressions[i])
                : Expression.OrElse(combinedDefault, childExpressions[i]);
        }

        if (includeSubClauses && logical.SubClause != null)
            return Expression.AndAlso(combinedDefault, BuildWhereClauseExpression<TEntity>(logical.SubClause, parameter, includeSubClauses, depth + 1));

        return combinedDefault;
    }

    private Expression BuildComparisonExpressionCached(
        ComparisonOperatorEnum comparison,
        Type propertyType,
        Expression target,
        Expression value,
        IEnumerable? values,
        ParameterExpression parameter)
    {
        var cacheKey = $"{MetadataCacheKeys.ComparisonMetadataPrefix}{comparison}_{propertyType.FullName}";
        var metadata = SharedEntityMetadataCache.GetOrAddComparisonMetadata(cacheKey, () => CreateComparisonMetadata(comparison, propertyType));
        return metadata.ComparisonType switch {
            ComparisonType.SimpleComparison => Expression.MakeBinary(metadata.ExpressionType!.Value, target, value),
            ComparisonType.StringMethod => BuildStringComparison(target, value, metadata.StringMethod!, metadata.ToLowerMethod, false),
            ComparisonType.NegatedStringMethod => BuildStringComparison(target, value, metadata.StringMethod!, metadata.ToLowerMethod, true),
            ComparisonType.OneOf => BuildOneOfExpressionCached(propertyType, target, values, false),
            ComparisonType.NotOneOf => BuildOneOfExpressionCached(propertyType, target, values, true),
            ComparisonType.Regex => BuildRegexComparison(target, value, false),
            ComparisonType.NotRegex => BuildRegexComparison(target, value, true),
            var _ => throw new InvalidQueryException($"Comparison '{comparison}' is not supported for type '{propertyType.Name}'")
        };
    }

    protected virtual Expression BuildStringComparison(Expression target, Expression value, MethodInfo stringMethod, MethodInfo? toLowerMethod, bool negate)
    {
        var stringTarget = AsStringExpression(target);
        var stringValue = AsStringExpression(value);
        var left = toLowerMethod != null ? Expression.Call(stringTarget, toLowerMethod) : stringTarget;
        var right = toLowerMethod != null ? Expression.Call(stringValue, toLowerMethod) : stringValue;
        Expression methodCall;
        if (stringMethod.Name == nameof(string.Contains))
            methodCall = Expression.Call(left, stringMethod, right);
        else if (stringMethod.Name == nameof(string.StartsWith))
            methodCall = Expression.Call(left, stringMethod, right);
        else if (stringMethod.Name == nameof(string.EndsWith))
            methodCall = Expression.Call(left, stringMethod, right);
        else
            throw new InvalidQueryException($"Unsupported string method: {stringMethod.Name}");

        var nullCheck = Expression.NotEqual(stringTarget, Expression.Constant(null, typeof(string)));
        var combined = Expression.AndAlso(nullCheck, methodCall);
        return negate ? Expression.Not(combined) : combined;
    }

    /// <summary>
    /// Renders a non-string operand as a string. Binds <c>ToString()</c> on the operand's own type rather than on <see cref="object" />: EF's method translators match the
    /// declared method, and <c>object.ToString()</c> has no Npgsql translation, so a <c>Contains</c> against a Guid column failed at query time.
    /// </summary>
    private static Expression AsStringExpression(Expression expression)
    {
        if (expression.Type == typeof(string))
            return expression;

        var declared = expression.Type.GetMethod(nameof(ToString), Type.EmptyTypes);
        return Expression.Call(expression, declared ?? ObjectToStringMethod);
    }

    /// <summary>
    /// Emits <see cref="Regex.IsMatch(string,string)" />. The two-argument overload is deliberate: it is the form Npgsql translates to the <c>~</c> operator, so the pattern
    /// runs in the database on the <see cref="IQueryable{T}" /> path. Predicates compiled for in-memory evaluation are rewritten to the timeout-bounded overload by
    /// <see cref="InMemoryEvaluationRewriter" />, because there the pattern runs in this process.
    /// </summary>
    protected virtual Expression BuildRegexComparison(Expression target, Expression value, bool negate)
    {
        if (value is ConstantExpression { Value: string pattern })
            ValidateRegexPattern(pattern);

        var targetAsString = AsStringExpression(target);
        var notNull = Expression.NotEqual(targetAsString, Expression.Constant(null, typeof(string)));
        var call = Expression.Call(RegexIsMatchMethod, targetAsString, value);
        var combined = Expression.AndAlso(notNull, call);
        return negate ? Expression.Not(combined) : combined;
    }

    /// <summary>
    /// Rejects a caller-supplied pattern before it reaches the matcher, so an unusable pattern is a request error rather than a failure part-way through a query. The bound
    /// matches <see cref="LyoParameterValidator.MaxValidationRegexLength" /> so both validation surfaces accept the same patterns.
    /// </summary>
    /// <param name="pattern">The pattern to check.</param>
    /// <exception cref="InvalidQueryException">The pattern is longer than the cap or is not a valid regular expression.</exception>
    protected static void ValidateRegexPattern(string pattern)
    {
        if (pattern.Length > MaxRegexPatternLength)
            throw new InvalidQueryException($"Regex pattern exceeds the maximum supported length of {MaxRegexPatternLength} characters.");

        try {
            _ = new Regex(pattern, RegexOptions.None, RegexMatchTimeout);
        }
        catch (ArgumentException ex) {
            throw new InvalidQueryException("Regex pattern is not a valid regular expression.", ex);
        }
    }

    /// <summary>Cache key identifying an entity type and clause shape for compiled matcher lookups.</summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree to hash.</param>
    protected static string GenerateWhereClauseCacheKey<TEntity>(Models.Common.WhereClause queryNode)
    {
        var sb = new StringBuilder(MatcherCachePrefix);
        sb.Append('_').Append(typeof(TEntity).FullName).Append('_');
        WhereClauseHelpers.AppendWhereClauseHash(queryNode, sb);
        return sb.ToString();
    }

    /// <summary>Compiled in-memory matcher for <paramref name="queryNode" />, memoized per entity type and clause shape.</summary>
    /// <remarks>Override to back the matcher with a shared or evictable cache. <see cref="CompileMatcher{TEntity}" /> performs the uncached work.</remarks>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree to compile.</param>
    protected virtual Func<TEntity, bool> GetMatcher<TEntity>(Models.Common.WhereClause queryNode)
        => (Func<TEntity, bool>)MatcherCache.GetOrAdd(GenerateWhereClauseCacheKey<TEntity>(queryNode), _ => CompileMatcher<TEntity>(queryNode));

    /// <summary>Compiles an in-memory matcher without consulting any cache.</summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree to compile.</param>
    protected Func<TEntity, bool> CompileMatcher<TEntity>(Models.Common.WhereClause queryNode)
    {
        var expr = BuildExpressionFromWhereClause<TEntity>(queryNode);
        if (expr == null)
            return _ => true;

        return ((Expression<Func<TEntity, bool>>)InMemoryEvaluationRewriter.Instance.Visit(expr)).Compile();
    }

    /// <summary>Predicate expression handed to <c>Queryable.Where</c>, memoized per entity type and clause shape.</summary>
    /// <remarks>Override to back the predicate with a shared or evictable cache.</remarks>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree to translate.</param>
    /// <param name="includeSubClauses">Whether nested sub-clauses are included in the expression.</param>
    protected virtual Expression<Func<TEntity, bool>>? GetEfPredicate<TEntity>(Models.Common.WhereClause queryNode, bool includeSubClauses)
        => (Expression<Func<TEntity, bool>>?)EfPredicateCache.GetOrAdd(
            GenerateEfPredicateCacheKey<TEntity>(queryNode, includeSubClauses), _ => BuildExpressionFromWhereClause<TEntity>(queryNode, includeSubClauses));

    /// <summary>Navigation include paths for collection segments in <paramref name="queryNode" />, memoized per entity type and clause shape.</summary>
    /// <remarks>Override to back the paths with a shared or evictable cache. <see cref="ComputeIncludePaths{TEntity}" /> performs the uncached work.</remarks>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree to inspect.</param>
    protected virtual IReadOnlyList<string> GetIncludePaths<TEntity>(Models.Common.WhereClause queryNode)
        => IncludePathCache.GetOrAdd(GenerateIncludePathsCacheKey<TEntity>(queryNode), _ => ComputeIncludePaths<TEntity>(queryNode));

    /// <summary>Resolves navigation include paths without consulting any cache.</summary>
    /// <typeparam name="TEntity">Entity type.</typeparam>
    /// <param name="queryNode">Filter tree to inspect.</param>
    protected IReadOnlyList<string> ComputeIncludePaths<TEntity>(Models.Common.WhereClause queryNode)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in CollectConditionFields(queryNode)) {
            if (string.IsNullOrWhiteSpace(field))
                continue;

            var pathMetadata = GetPropertyPathMetadataCached<TEntity>(field);
            if (pathMetadata.CollectionPropertyIndex is not { } collectionIndex)
                continue;

            var includePath = string.Join(".", pathMetadata.Properties.Take(collectionIndex + 1).Select(p => p.Name));
            if (!string.IsNullOrEmpty(includePath))
                paths.Add(includePath);
        }

        return paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>Builds the cache key identifying an entity type and clause shape for include-path lookups.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="queryNode">The filter tree to hash.</param>
    protected static string GenerateIncludePathsCacheKey<TEntity>(Models.Common.WhereClause queryNode)
    {
        var sb = new StringBuilder("SubQueryIncludePaths_").Append(typeof(TEntity).FullName).Append('_');
        WhereClauseHelpers.AppendWhereClauseHash(queryNode, sb);
        return sb.ToString();
    }

    /// <summary>Builds the cache key identifying an entity type, clause shape, and sub-clause mode for predicate lookups.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="queryNode">The filter tree to hash.</param>
    /// <param name="includeSubClauses">Whether nested sub-clauses are included in the expression.</param>
    protected static string GenerateEfPredicateCacheKey<TEntity>(Models.Common.WhereClause queryNode, bool includeSubClauses)
    {
        var sb = new StringBuilder(EfPredicateCachePrefix);
        sb.Append('_').Append(typeof(TEntity).FullName).Append('_').Append(includeSubClauses ? '1' : '0').Append('_');
        sb.Append(WhereClauseHelpers.GetWhereClauseTreeHash(queryNode));
        return sb.ToString();
    }

    private static string ConvertToString(object? value)
    {
        if (value == null)
            return string.Empty;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return jsonElement.GetString() ?? string.Empty;

        return value.ToString() ?? string.Empty;
    }

    private MemberExpression GetPropertyExpression<T>(string propertyName, ParameterExpression parameter)
    {
        var pathMetadata = GetPropertyPathMetadataCached<T>(propertyName);
        Expression expression = parameter;
        foreach (var propertyInfo in pathMetadata.Properties)
            expression = Expression.Property(expression, propertyInfo);

        return (MemberExpression)expression;
    }

    private PropertyPathMetadata GetPropertyPathMetadataCached<T>(string propertyName)
        => SharedEntityMetadataCache.GetOrAddPropertyPath<T>(propertyName, () => BuildPropertyPathMetadata<T>(propertyName));

    private PropertyPathMetadata BuildPropertyPathMetadata<T>(string propertyName)
    {
        try {
            var properties = new List<PropertyInfo>();
            var currentType = typeof(T);
            var propertyParts = propertyName.Split('.');
            int? collectionIndex = null;
            Type? collectionElementType = null;
            var isCountPath = false;
            for (var i = 0; i < propertyParts.Length; i++) {
                var part = propertyParts[i];
                if (string.Equals(part, "Count", StringComparison.OrdinalIgnoreCase) && properties.Count > 0) {
                    var prevProp = properties[^1];
                    var prevType = prevProp.PropertyType;
                    var prevIsCollection = typeof(IEnumerable).IsAssignableFrom(prevType) && prevType != typeof(string) && prevType != typeof(byte[]);
                    if (prevIsCollection) {
                        collectionIndex ??= properties.Count - 1;
                        collectionElementType ??= SharedEntityMetadataCache.GetCollectionElementType(prevType);
                        currentType = typeof(int);
                        isCountPath = true;
                        break;
                    }
                }

                var propertyInfo = ResolvePropertyCached(currentType, part);
                if (propertyInfo != null) {
                    properties.Add(propertyInfo);
                    var propType = propertyInfo.PropertyType;
                    var isCollection = typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string) && propType != typeof(byte[]);
                    if (isCollection) {
                        var elemType = SharedEntityMetadataCache.GetCollectionElementType(propType);
                        collectionIndex ??= i;
                        collectionElementType ??= elemType;
                        currentType = elemType;
                    }
                    else
                        currentType = propType;
                }
                else {
                    // Implicit hop: the caller named a property that lives one navigation down. Type.GetProperties has no defined order, so taking the first match made the
                    // winner arbitrary when two navigations expose the same name — and the arbitrary winner was then memoised process-wide. Scan every candidate, order by
                    // name so the choice is reproducible, and refuse an ambiguous path rather than silently picking one.
                    var candidates = new List<(PropertyInfo Parent, PropertyInfo Nested)>();
                    foreach (var cand in currentType.GetProperties(PropertySearchFlags).OrderBy(p => p.Name, StringComparer.Ordinal)) {
                        var candNested = ResolvePropertyCached(cand.PropertyType, part);
                        if (candNested != null) {
                            candidates.Add((cand, candNested));
                            continue;
                        }

                        if (!typeof(IEnumerable).IsAssignableFrom(cand.PropertyType) || cand.PropertyType == typeof(string) || cand.PropertyType == typeof(byte[]))
                            continue;

                        var elemType = SharedEntityMetadataCache.GetCollectionElementType(cand.PropertyType);
                        var elemNested = ResolvePropertyCached(elemType, part);
                        if (elemNested != null)
                            candidates.Add((cand, elemNested));
                    }

                    if (candidates.Count > 1) {
                        var options = string.Join(", ", candidates.Select(c => $"{c.Parent.Name}.{c.Nested.Name}"));
                        throw new InvalidQueryException($"Property '{part}' is ambiguous on type '{currentType.Name}'; qualify the path. Candidates: {options}.");
                    }

                    var parentProp = candidates.Count == 1 ? candidates[0].Parent : null;
                    var nestedProp = candidates.Count == 1 ? candidates[0].Nested : null;
                    if (parentProp != null && nestedProp != null) {
                        properties.Add(parentProp);
                        properties.Add(nestedProp);
                        var parentIsCollection = typeof(IEnumerable).IsAssignableFrom(parentProp.PropertyType) && parentProp.PropertyType != typeof(string) &&
                            parentProp.PropertyType != typeof(byte[]);

                        if (parentIsCollection) {
                            var elemType = SharedEntityMetadataCache.GetCollectionElementType(parentProp.PropertyType);
                            collectionIndex ??= i;
                            collectionElementType ??= elemType;
                        }

                        currentType = nestedProp.PropertyType;
                    }
                    else
                        throw new InvalidQueryException($"Property '{part}' not found on type '{currentType.Name}'.");
                }
            }

            return new(properties, currentType, collectionIndex, collectionElementType, isCountPath);
        }
        catch (Exception ex) when (ex is not InvalidQueryException) {
            throw new InvalidQueryException($"Property path '{propertyName}' not found on type '{typeof(T).Name}'.", ex);
        }
    }

    private (Expression Expression, Type Type) AdjustForCollectionCached(MemberExpression property)
    {
        var type = property.Type;
        var metadata = SharedEntityMetadataCache.GetOrAddCollectionAdjustment(
            type,
            () => new(typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) && type != typeof(byte[]), SharedEntityMetadataCache.GetCollectionElementType(type)));

        if (!metadata.IsCollection)
            return (property, type);

        var countMethod = GetEnumerableCountMethod(metadata.ElementType);
        var countExpression = Expression.Call(countMethod, property);
        return (countExpression, typeof(int));
    }

    private Expression BuildOneOfExpressionCached(Type type, Expression target, IEnumerable? values, bool negate)
    {
        if (values == null)
            throw new InvalidQueryException("OneOf/NotOneOf requires a list of values.");

        var cacheKey = $"{MetadataCacheKeys.ReflectedMethodPrefix}Contains_{type.FullName}";
        var containsMethod = SharedEntityMetadataCache.GetOrAddReflectedMethod(
            cacheKey,
            () => typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(type));

        var valuesExpr = Expression.Constant(values);
        var call = Expression.Call(containsMethod, valuesExpr, target);
        return negate ? Expression.Not(call) : call;
    }

    private (Expression keySelector, Type keyType) BuildKeySelector<T>(string propertyName, ParameterExpression parameter)
    {
        var pathMetadata = GetPropertyPathMetadataCached<T>(propertyName);
        if (pathMetadata.CollectionPropertyIndex != null) {
            var collectionIndex = pathMetadata.CollectionPropertyIndex.Value;
            Expression collectionExpr = parameter;
            for (var i = 0; i <= collectionIndex; i++)
                collectionExpr = Expression.Property(collectionExpr, pathMetadata.Properties[i]);

            var elementType = pathMetadata.CollectionElementType ?? SharedEntityMetadataCache.GetCollectionElementType(collectionExpr.Type);
            var asQueryableMethod = GetAsQueryableMethod(elementType);
            var queryableExpr = Expression.Call(asQueryableMethod, collectionExpr);
            var countMethod = GetQueryableCountMethod(elementType);
            var countCall = Expression.Call(countMethod, queryableExpr);
            return (countCall, typeof(int));
        }

        Expression expression = parameter;
        foreach (var propertyInfo in pathMetadata.Properties)
            expression = Expression.Property(expression, propertyInfo);

        var resolvedType = expression.Type;
        var isResolvedEnumerable = typeof(IEnumerable).IsAssignableFrom(resolvedType) && resolvedType != typeof(string);
        if (isResolvedEnumerable) {
            var elementType = SharedEntityMetadataCache.GetCollectionElementType(resolvedType);
            var asQueryableMethod = GetAsQueryableMethod(elementType);
            var queryableExpr = Expression.Call(asQueryableMethod, expression);
            var countMethod = GetQueryableCountMethod(elementType);
            var countCall = Expression.Call(countMethod, queryableExpr);
            return (countCall, typeof(int));
        }

        return (expression, pathMetadata.FinalType);
    }

    private MethodInfo GetQueryableOrderMethodCached<TSource>(string methodName, Type keyType)
    {
        var cacheKey = $"{MetadataCacheKeys.OrderMethodPrefix}{typeof(TSource).FullName}_{methodName}_{keyType.FullName}";
        return SharedEntityMetadataCache.GetOrAddOrderMethod(
            cacheKey, () => QueryableOrderMethods.Single(m => m.Name == methodName && m.GetParameters().Length == 2).MakeGenericMethod(typeof(TSource), keyType));
    }

    private MethodInfo GetEnumerableCountMethod(Type elementType)
    {
        var cacheKey = $"{MetadataCacheKeys.ReflectedMethodPrefix}EnumerableCount_{elementType.FullName}";
        return SharedEntityMetadataCache.GetOrAddReflectedMethod(
            cacheKey, () => typeof(Enumerable).GetMethods().First(m => m.Name == "Count" && m.GetParameters().Length == 1).MakeGenericMethod(elementType));
    }

    private MethodInfo GetAsQueryableMethod(Type elementType)
    {
        var cacheKey = $"{MetadataCacheKeys.ReflectedMethodPrefix}AsQueryable_{elementType.FullName}";
        return SharedEntityMetadataCache.GetOrAddReflectedMethod(
            cacheKey,
            () => typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "AsQueryable" && m.GetParameters().Length == 1 && m.IsGenericMethod)
                .MakeGenericMethod(elementType));
    }

    private MethodInfo GetQueryableCountMethod(Type elementType)
    {
        var cacheKey = $"{MetadataCacheKeys.ReflectedMethodPrefix}QueryableCount_{elementType.FullName}";
        return SharedEntityMetadataCache.GetOrAddReflectedMethod(
            cacheKey,
            () => typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                .MakeGenericMethod(elementType));
    }

    private ComparisonMetadata CreateComparisonMetadata(ComparisonOperatorEnum comparison, Type propertyType)
        => comparison switch {
            ComparisonOperatorEnum.Equals => new(ComparisonType.SimpleComparison, ExpressionType.Equal),
            ComparisonOperatorEnum.NotEquals => new(ComparisonType.SimpleComparison, ExpressionType.NotEqual),
            ComparisonOperatorEnum.GreaterThan => new(ComparisonType.SimpleComparison, ExpressionType.GreaterThan),
            ComparisonOperatorEnum.GreaterThanOrEqual => new(ComparisonType.SimpleComparison, ExpressionType.GreaterThanOrEqual),
            ComparisonOperatorEnum.LessThan => new(ComparisonType.SimpleComparison, ExpressionType.LessThan),
            ComparisonOperatorEnum.LessThanOrEqual => new(ComparisonType.SimpleComparison, ExpressionType.LessThanOrEqual),
            ComparisonOperatorEnum.Contains when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.StringMethod, ToLowerMethod: StringToLowerMethod,
                StringMethod: typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!),
            ComparisonOperatorEnum.NotContains when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.NegatedStringMethod, ToLowerMethod: StringToLowerMethod,
                StringMethod: typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!),
            ComparisonOperatorEnum.StartsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.StringMethod, ToLowerMethod: StringToLowerMethod,
                StringMethod: typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!),
            ComparisonOperatorEnum.EndsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.StringMethod, ToLowerMethod: StringToLowerMethod,
                StringMethod: typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!),
            ComparisonOperatorEnum.NotStartsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.NegatedStringMethod, ToLowerMethod: StringToLowerMethod,
                StringMethod: typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!),
            ComparisonOperatorEnum.NotEndsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.NegatedStringMethod, ToLowerMethod: StringToLowerMethod,
                StringMethod: typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!),
            ComparisonOperatorEnum.In => new(ComparisonType.OneOf),
            ComparisonOperatorEnum.NotIn => new(ComparisonType.NotOneOf),
            ComparisonOperatorEnum.Regex when SupportsStringMethodComparisons(propertyType) => new(ComparisonType.Regex),
            ComparisonOperatorEnum.NotRegex when SupportsStringMethodComparisons(propertyType) => new(ComparisonType.NotRegex),
            var _ => throw new InvalidQueryException($"Comparison '{comparison}' is not supported for type '{propertyType.Name}'")
        };

    /// <summary>
    /// Guid (and nullable Guid) use the same string operations as <see cref="string" />; values are compared on canonical <see cref="Guid.ToString()" /> forms (aligned with
    /// typical SQL string casts).
    /// </summary>
    private static bool IsGuidLikeType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(Guid);
    }

    private static bool IsStringOrRegexComparison(ComparisonOperatorEnum comparison)
        => comparison is ComparisonOperatorEnum.Contains or ComparisonOperatorEnum.NotContains or ComparisonOperatorEnum.StartsWith or ComparisonOperatorEnum.EndsWith
            or ComparisonOperatorEnum.NotStartsWith or ComparisonOperatorEnum.NotEndsWith or ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex;

    /// <summary>Types that support Contains/StartsWith/EndsWith/Regex using string semantics (including Guid, matched via string representation).</summary>
    private static bool SupportsStringMethodComparisons(Type propertyType) => propertyType == typeof(string) || IsGuidLikeType(propertyType);

    private static Type GetFilterValueParseType(Type propertyType, ComparisonOperatorEnum comparison)
        => SupportsStringMethodComparisons(propertyType) && IsStringOrRegexComparison(comparison) ? typeof(string) : propertyType;

    /// <summary>RHS for string-method / regex filters on Guid must stay <see cref="string" /> (partial fragments); do not convert to <see cref="Guid" />.</summary>
    private static Expression GetRhsConstantExpression(object? parsedValue, Type parseType, Expression propertyExpression, ComparisonOperatorEnum comparison)
        => parseType == typeof(string) && propertyExpression.Type != typeof(string) && IsGuidLikeType(propertyExpression.Type) && IsStringOrRegexComparison(comparison)
            ? Expression.Constant(parsedValue, typeof(string))
            : GetValueExpression(parsedValue, parseType, propertyExpression);

    private static Expression GetValueExpression(object? value, Type targetType, Expression targetExpression)
    {
        var constant = Expression.Constant(value, value == null ? typeof(object) : targetType);
        return constant.Type != targetExpression.Type ? Expression.Convert(constant, targetExpression.Type) : constant;
    }

    private static string GetOrderingMethodName<T>(IQueryable<T> source, SortDirection sortDirection)
        => !IsQueryableOrdered(source) ? sortDirection == SortDirection.Desc ? "OrderByDescending" : "OrderBy" :
            sortDirection == SortDirection.Desc ? "ThenByDescending" : "ThenBy";

    /// <summary>
    /// Operators that pass the current ordering through unchanged. Walking past them is what lets <c>query.OrderBy(x).AsNoTracking()</c> still be recognised as ordered;
    /// inspecting only the outermost call reported it as unordered, so the next sort emitted <c>OrderBy</c> and silently discarded the first sort key.
    /// </summary>
    private static readonly HashSet<string> OrderPreservingQueryOperators = new(StringComparer.Ordinal) {
        "AsNoTracking",
        "AsNoTrackingWithIdentityResolution",
        "AsTracking",
        "AsSplitQuery",
        "AsSingleQuery",
        "IgnoreQueryFilters",
        "IgnoreAutoIncludes",
        "Include",
        "ThenInclude",
        "TagWith",
        "TagWithCallSite",
        "Where",
        "Cast",
        "OfType",
        "Skip",
        "Take"
    };

    /// <summary>
    /// Whether an ordering has already been established on <paramref name="source" />. Cannot use <c>source is IOrderedQueryable&lt;T&gt;</c>: EF Core's root queryable
    /// implements that interface whether or not an <c>OrderBy</c> has been applied, so the test would emit <c>ThenBy</c> with nothing to chain onto.
    /// </summary>
    private static bool IsQueryableOrdered<T>(IQueryable<T> source)
    {
        var expression = source.Expression;
        while (expression is MethodCallExpression methodCall) {
            var methodName = methodCall.Method.Name;
            if (methodName is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
                return true;

            if (methodCall.Arguments.Count == 0 || !OrderPreservingQueryOperators.Contains(methodName))
                return false;

            expression = methodCall.Arguments[0];
        }

        return false;
    }

    private static bool IsNullFilterValue(object? value) => value == null || value is JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined };

    private static bool IsNonNullableType(Type type) => Nullable.GetUnderlyingType(type) == null && type.IsValueType;

    private static void ValidateNullComparison(ComparisonOperatorEnum comparison, Type propertyType, object? rawValue, string propertyName)
    {
        if (comparison is not ComparisonOperatorEnum.Equals and not ComparisonOperatorEnum.NotEquals)
            return;

        if (!IsNullFilterValue(rawValue))
            return;

        if (!IsNonNullableType(propertyType))
            return;

        throw new InvalidQueryException($"Comparison '{comparison}' cannot be used with null for non-nullable field '{propertyName}'.");
    }

    private (object? SingleValue, IEnumerable? MultipleValues) ParseFilterValue(object? value, ComparisonOperatorEnum comparison, Type propertyType)
    {
        if (comparison is ComparisonOperatorEnum.In or ComparisonOperatorEnum.NotIn) {
            var listType = typeof(List<>).MakeGenericType(propertyType);
            var list = (IList)Activator.CreateInstance(listType)!;
            switch (value) {
                case null:
                    break;
                case string stringValue:
                    var parts = stringValue.Split(',');
                    foreach (var part in parts) {
                        var trimmed = part.Trim();
                        if (trimmed.Length > 0)
                            list.Add(ValueConversion.ConvertToTargetType(trimmed, propertyType)!);
                    }

                    break;
                case JsonElement { ValueKind: JsonValueKind.String } jsonStrElement:
                    var jsonStringValue = jsonStrElement.GetString();
                    if (jsonStringValue is { Length: > 0 }) {
                        var jsonParts = jsonStringValue.Split(',');
                        foreach (var part in jsonParts) {
                            var trimmed = part.Trim();
                            if (trimmed.Length > 0)
                                list.Add(ValueConversion.ConvertToTargetType(trimmed, propertyType)!);
                        }
                    }

                    break;
                case JsonElement { ValueKind: JsonValueKind.Array } jsonElement:
                    foreach (var item in jsonElement.EnumerateArray()) {
                        // Tolerate an accidentally nested array (e.g. a collection wrapped by a params overload).
                        if (item.ValueKind == JsonValueKind.Array) {
                            foreach (var nested in item.EnumerateArray())
                                list.Add(ValueConversion.ConvertToTargetType(nested, propertyType)!);
                        }
                        else
                            list.Add(ValueConversion.ConvertToTargetType(item, propertyType)!);
                    }

                    break;
                default:
                    if (ValueConversion.IsObjectEnumerable(value)) {
                        foreach (var item in (IEnumerable)value) {
                            if (item is not string && item is IEnumerable nestedEnumerable) {
                                foreach (var nested in nestedEnumerable)
                                    list.Add(ValueConversion.ConvertToTargetType(nested, propertyType)!);
                            }
                            else
                                list.Add(ValueConversion.ConvertToTargetType(item, propertyType)!);
                        }
                    }
                    else
                        list.Add(ValueConversion.ConvertToTargetType(value, propertyType)!);

                    break;
            }

            return (null, list);
        }

        if (value is JsonElement element) {
            if (element.ValueKind == JsonValueKind.String)
                return (ValueConversion.ConvertToTargetType(element.GetString(), propertyType), null);

            if (element.ValueKind == JsonValueKind.Array) {
                var enumerator = element.EnumerateArray();
                if (enumerator.MoveNext())
                    return (ValueConversion.ConvertToTargetType(enumerator.Current, propertyType), null);
            }

            return (ValueConversion.ConvertToTargetType(element, propertyType), null);
        }

        if (value is byte[])
            return (value, null);

        if (ValueConversion.IsObjectEnumerable(value)) {
            var enumerator = ((IEnumerable)value!).GetEnumerator();
            if (enumerator.MoveNext())
                return (ValueConversion.ConvertToTargetType(enumerator.Current, propertyType), null);
        }

        return (ValueConversion.ConvertToTargetType(value, propertyType), null);
    }

    /// <summary>Whether <paramref name="entity" /> matches <paramref name="queryNode" /> using a cached compiled delegate.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <param name="queryNode">The filter tree, or <c>null</c> for a vacuous match.</param>
    /// <returns><c>true</c> if the entity matches; <c>false</c> if the entity is null or the clause does not match.</returns>
    public virtual bool Match<TEntity>(TEntity entity, Models.Common.WhereClause? queryNode)
    {
        if (entity == null)
            return false;

        if (queryNode == null)
            return true;

        return GetMatcher<TEntity>(queryNode)(entity);
    }

    /// <summary>Whether <paramref name="entity" /> matches the optional <see cref="QueryConcreteReq.WhereClause" /> on <paramref name="queryRequest" />.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <param name="queryRequest">The API query request, or <c>null</c> (treated as no filter).</param>
    /// <returns>
    /// <c>false</c> if <paramref name="entity" /> is null; <c>true</c> if the request is null or has no where clause; otherwise the result of
    /// <see cref="Match{TEntity}(TEntity, Lyo.Query.Models.Common.WhereClause?)" />.
    /// </returns>
    public virtual bool Match<TEntity>(TEntity entity, QueryConcreteReq? queryRequest)
    {
        if (entity == null)
            return false;

        if (queryRequest is null)
            return true;

        return queryRequest.WhereClause == null || Match(entity, queryRequest.WhereClause);
    }

    /// <summary>The single metric tag every recording here carries, materialized once per entity type so the instrumented paths stay allocation-free.</summary>
    private static class EntityTypeTag<TEntity>
    {
        public static readonly (string, string)[] Tags = [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)];
    }

    /// <summary>
    /// Retargets the expression tree from "translatable to SQL" to "safe and correct in this process". Two swaps: <see cref="Regex.IsMatch(string,string)" /> becomes the
    /// timeout-bounded overload, because a pattern like <c>^(a+)+$</c> backtracks catastrophically and would otherwise pin a core with no way to interrupt it; and
    /// <c>ToLower()</c> becomes <see cref="string.ToLowerInvariant" />, because the culture-sensitive fold makes matching depend on the server's locale.
    /// </summary>
    private sealed class InMemoryEvaluationRewriter : ExpressionVisitor
    {
        public static readonly InMemoryEvaluationRewriter Instance = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method == RegexIsMatchMethod) {
                return Expression.Call(
                    RegexIsMatchWithTimeoutMethod, Visit(node.Arguments[0]), Visit(node.Arguments[1]), Expression.Constant(RegexOptions.None),
                    Expression.Constant(RegexMatchTimeout));
            }

            if (node.Method == StringToLowerMethod)
                return Expression.Call(Visit(node.Object)!, StringToLowerInvariantMethod);

            return base.VisitMethodCall(node);
        }
    }
}