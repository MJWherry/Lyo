using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Cache;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.ValueConversion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Lyo.Cache.Constants;

namespace Lyo.Query.Services.WhereClause;

internal enum ComparisonType
{
    SimpleComparison,
    StringMethod,
    NegatedStringMethod,
    ExactString,
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

public class BaseWhereClauseService : IWhereClauseService
{
    private const string MatcherCachePrefix = "filter_matcher";
    private const BindingFlags PropertySearchFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

    private static readonly MethodInfo[] QueryableOrderMethods = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending")
        .ToArray();

    private readonly ILogger<BaseWhereClauseService> _logger;
    private readonly IMetrics _metrics;
    protected readonly ICacheService Cache;
    protected readonly CacheOptions CacheOptions;
    protected readonly IValueConversionService ValueConversion;

    public BaseWhereClauseService(
        ICacheService cache,
        CacheOptions cacheOptions,
        IValueConversionService valueConversion,
        ILogger<BaseWhereClauseService>? logger = null,
        IMetrics? metrics = null)
    {
        Cache = cache;
        CacheOptions = cacheOptions;
        ValueConversion = valueConversion;
        _logger = logger ?? NullLogger<BaseWhereClauseService>.Instance;
        _metrics = cacheOptions.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
    }

    public virtual bool MatchesWhereClause<TEntity>(TEntity entity, Models.Common.WhereClause? queryNode)
    {
        if (entity is null || queryNode is null)
            return false;

        using var timer = _metrics.StartTimer(Constants.Metrics.MatchesWhereClauseDuration, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
        try {
            var cacheKey = GenerateWhereClauseCacheKey<TEntity>(queryNode);
            var matcher = Cache.GetOrSet<Func<TEntity, bool>>(
                cacheKey, _ => {
                    var expr = BuildExpressionFromWhereClause<TEntity>(queryNode);
                    return expr == null ? _ => true : expr.Compile();
                }, CacheOptions.DefaultExpiration)!;

            var result = matcher(entity);
            _metrics.IncrementCounter(Constants.Metrics.MatchesWhereClauseSuccess, 1, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            _logger.LogDebug("MatchesWhereClause evaluated for {EntityType}", typeof(TEntity).Name);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.MatchesWhereClauseDuration, ex, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            throw;
        }
    }

    /// <inheritdoc cref="IWhereClauseService.ExplainMatch{TEntity}" />
    public virtual WhereClauseExplainResult ExplainMatch<TEntity>(TEntity entity, Models.Common.WhereClause? queryNode)
    {
        if (entity is null)
            return new WhereClauseExplainResult(
                new WhereClauseExplainNode { Passed = false, Kind = WhereClauseExplainKind.None, Path = "" },
                null,
                "Entity is null.");

        if (queryNode is null)
            return new WhereClauseExplainResult(
                new WhereClauseExplainNode { Passed = false, Kind = WhereClauseExplainKind.None, Path = "" },
                null,
                "Where clause is null.");

        var root = BuildExplainNode<TEntity>(entity, queryNode, "");
        var (blockingPath, failureSummary) = ComputeBlockingFailure(root);
        var orBranches = root.Passed ? null : CollectOrBranchOutcomes(root);
        return new WhereClauseExplainResult(root, blockingPath, failureSummary, orBranches);
    }

    private WhereClauseExplainNode BuildExplainNode<TEntity>(TEntity entity, Models.Common.WhereClause node, string path)
    {
        var passed = EvaluateWhereClauseCompiled<TEntity>(entity, node);
        var description = node.Description;

        switch (node) {
            case ConditionClause c: {
                bool? primaryPredicatePassed = null;
                WhereClauseExplainNode? subExplain = null;
                if (c.SubClause != null) {
                    var param = Expression.Parameter(typeof(TEntity), "x");
                    var primaryBody = BuildConditionExpression<TEntity>(c, param, false);
                    var primaryLambda = Expression.Lambda<Func<TEntity, bool>>(primaryBody, param);
                    primaryPredicatePassed = primaryLambda.Compile()(entity);
                    subExplain = BuildExplainNode<TEntity>(entity, c.SubClause, string.IsNullOrEmpty(path) ? "sub" : $"{path}/sub");
                }

                return new WhereClauseExplainNode {
                    Passed = passed,
                    Kind = WhereClauseExplainKind.Condition,
                    Path = path,
                    Description = description,
                    Field = c.Field,
                    Comparison = c.Comparison,
                    FilterValue = c.Value,
                    ActualValueSummary = TryFormatActualValueSummary<TEntity>(entity, c.Field),
                    PrimaryPredicatePassed = primaryPredicatePassed,
                    SubClause = subExplain
                };
            }
            case GroupClause g: {
                var children = new WhereClauseExplainNode[g.Children.Count];
                for (var i = 0; i < g.Children.Count; i++) {
                    var childPath = string.IsNullOrEmpty(path) ? i.ToString() : $"{path}/{i}";
                    children[i] = BuildExplainNode<TEntity>(entity, g.Children[i], childPath);
                }

                var subExplain = g.SubClause != null ? BuildExplainNode<TEntity>(entity, g.SubClause, string.IsNullOrEmpty(path) ? "sub" : $"{path}/sub") : null;

                return new WhereClauseExplainNode {
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

    private static (string? BlockingPath, string? FailureSummary) ComputeBlockingFailure(WhereClauseExplainNode root)
    {
        if (root.Passed)
            return (null, null);

        return FindBlockingFailure(root);
    }

    private static (string? Path, string? Summary) FindBlockingFailure(WhereClauseExplainNode node)
    {
        if (node.Passed)
            return (null, null);

        switch (node.Kind) {
            case WhereClauseExplainKind.None:
                return (null, null);
            case WhereClauseExplainKind.Condition:
                if (node.SubClause != null && node.PrimaryPredicatePassed == true)
                    return FindBlockingFailure(node.SubClause);

                return (string.IsNullOrEmpty(node.Path) ? null : node.Path, FormatConditionFailureLine(node));
            case WhereClauseExplainKind.Group:
                if (node.Children != null) {
                    foreach (var child in node.Children) {
                        if (!child.Passed) {
                            var inner = FindBlockingFailure(child);
                            if (inner.Path != null || inner.Summary != null)
                                return inner;
                        }
                    }
                }

                if (node.SubClause != null && !node.SubClause.Passed)
                    return FindBlockingFailure(node.SubClause);

                return (string.IsNullOrEmpty(node.Path) ? null : node.Path, "Group conditions were not satisfied.");
            default:
                return (string.IsNullOrEmpty(node.Path) ? null : node.Path, "Clause did not match.");
        }
    }

    private static string FormatConditionFailureLine(WhereClauseExplainNode node)
    {
        var field = node.Field ?? "?";
        var cmp = node.Comparison?.ToString() ?? "?";
        var actual = node.ActualValueSummary;
        return string.IsNullOrEmpty(actual) ? $"{field} {cmp} is not satisfied." : $"{field} {cmp} is not satisfied (actual: {actual}).";
    }

    /// <summary>Lists each direct branch under every <see cref="GroupOperatorEnum.Or"/> group that failed (nested Or groups included).</summary>
    private static IReadOnlyList<ExplainOrBranchOutcome>? CollectOrBranchOutcomes(WhereClauseExplainNode root)
    {
        var list = new List<ExplainOrBranchOutcome>();
        VisitForFailedOrGroups(root);
        return list.Count == 0 ? null : list;

        void VisitForFailedOrGroups(WhereClauseExplainNode n)
        {
            if (n.Kind == WhereClauseExplainKind.Group
                && n.GroupOperator == GroupOperatorEnum.Or
                && !n.Passed
                && n.Children is { Count: > 0 } orChildren) {
                var orPath = n.Path ?? "";
                foreach (var branch in orChildren) {
                    list.Add(new ExplainOrBranchOutcome {
                        OrGroupPath = orPath,
                        BranchPath = branch.Path,
                        Passed = branch.Passed,
                        Summary = SummarizeOrBranchOutcome(branch)
                    });
                }
            }

            if (n.Children != null) {
                foreach (var ch in n.Children)
                    VisitForFailedOrGroups(ch);
            }

            if (n.SubClause != null)
                VisitForFailedOrGroups(n.SubClause);
        }
    }

    private static string SummarizeOrBranchOutcome(WhereClauseExplainNode branch)
    {
        if (branch.Passed)
            return "Branch passed.";

        return branch.Kind switch {
            WhereClauseExplainKind.Condition => FormatConditionFailureLine(branch),
            WhereClauseExplainKind.Group => SummarizeFailedGroupBranch(branch),
            WhereClauseExplainKind.None => "Branch did not match.",
            _ => "Branch did not match."
        };
    }

    private static string SummarizeFailedGroupBranch(WhereClauseExplainNode group)
    {
        var inner = FindBlockingFailure(group);
        return !string.IsNullOrEmpty(inner.Summary) ? inner.Summary : "Subgroup did not match.";
    }

    private string? TryFormatActualValueSummary<TEntity>(TEntity entity, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        try {
            var meta = GetPropertyPathMetadataCached<TEntity>(field);
            return FormatPropertyPathValue(entity, meta);
        }
        catch (Exception ex) {
            _logger.LogDebug(ex, "ExplainMatch could not read actual value for field {Field}", field);
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

                object? leaf = item;
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

    private bool EvaluateWhereClauseCompiled<TEntity>(TEntity entity, Models.Common.WhereClause node)
    {
        var expr = BuildExpressionFromWhereClause<TEntity>(node);
        if (expr == null)
            return true;

        return expr.Compile()(entity);
    }

    public virtual IQueryable<TEntity> ApplyWhereClause<TEntity>(IQueryable<TEntity> source, Models.Common.WhereClause? queryNode, bool includeSubClauses = true)
    {
        if (queryNode is null)
            return source;

        using var timer = _metrics.StartTimer(Constants.Metrics.ApplyWhereClauseDuration, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
        try {
            var expression = BuildExpressionFromWhereClause<TEntity>(queryNode, includeSubClauses);
            var result = expression != null ? source.Where(expression) : source;
            _metrics.IncrementCounter(Constants.Metrics.ApplyWhereClauseSuccess, 1, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            _logger.LogDebug("ApplyWhereClause applied to {EntityType}", typeof(TEntity).Name);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.ApplyWhereClauseDuration, ex, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            throw;
        }
    }

    public virtual IQueryable<TEntity> SortByProperty<TEntity>(IQueryable<TEntity> source, string propertyName, SortDirection? direction = null)
    {
        if (string.IsNullOrEmpty(propertyName))
            throw new InvalidQueryException("Property name cannot be null or empty.");

        using var timer = _metrics.StartTimer(Constants.Metrics.SortByPropertyDuration, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
        try {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var (keySelector, keyType) = BuildKeySelector<TEntity>(propertyName, parameter);
            var methodName = GetOrderingMethodName(source, direction ?? SortDirection.Desc);
            var orderMethod = GetQueryableOrderMethodCached<TEntity>(methodName, keyType);
            var lambda = Expression.Lambda(keySelector, parameter);
            var result = (IQueryable<TEntity>)orderMethod.Invoke(null, [source, lambda])!;
            _metrics.IncrementCounter(Constants.Metrics.SortByPropertySuccess, 1, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            _logger.LogDebug("SortByProperty applied to {EntityType} by {PropertyName} {Direction}", typeof(TEntity).Name, propertyName, direction ?? SortDirection.Desc);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.SortByPropertyDuration, ex, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            throw;
        }
    }

    public virtual IQueryable<TEntity> ApplyOrdering<TEntity>(
        IQueryable<TEntity> queryable,
        IEnumerable<SortBy> sortByProps,
        Expression<Func<TEntity, object?>> defaultOrder,
        SortDirection defaultSortDirection)
    {
        var byProps = sortByProps as SortBy[] ?? sortByProps.ToArray();
        using var timer = _metrics.StartTimer(Constants.Metrics.ApplyOrderingDuration, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
        try {
            var result = !byProps.Any()
                ? defaultSortDirection == SortDirection.Desc ? queryable.OrderByDescending(defaultOrder) : queryable.OrderBy(defaultOrder)
                : byProps.Select((s, i) => (SortBy: s, EffectivePriority: s.Priority ?? i))
                    .OrderBy(x => x.EffectivePriority)
                    .Aggregate(queryable, (current, x) => SortByProperty(current, x.SortBy.PropertyName, x.SortBy.Direction));

            _metrics.IncrementCounter(Constants.Metrics.ApplyOrderingSuccess, 1, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            _metrics.RecordGauge(Constants.Metrics.SortByCount, byProps.Length, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            _logger.LogDebug("ApplyOrdering applied to {EntityType} with {SortByCount} sort properties", typeof(TEntity).Name, byProps.Length);
            return result;
        }
        catch (Exception ex) {
            _metrics.RecordError(Constants.Metrics.ApplyOrderingDuration, ex, [(Constants.Metrics.Tags.EntityType, typeof(TEntity).Name)]);
            throw;
        }
    }

    public virtual IEnumerable<string> GetCollectionIncludePathsForWhereClause<TEntity>(Models.Common.WhereClause? queryNode)
    {
        if (queryNode == null)
            return [];

        var cacheKeyBuilder = new StringBuilder("SubQueryIncludePaths_").Append(typeof(TEntity).FullName).Append('_');
        AppendWhereClauseHash(queryNode, cacheKeyBuilder);
        var cacheKey = cacheKeyBuilder.ToString();
        var tags = new[] { $"entity:{typeof(TEntity).Name.ToLowerInvariant()}" };
        return Cache.GetOrSet<IReadOnlyList<string>>(
            cacheKey, _ => {
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

                return paths.Order(StringComparer.OrdinalIgnoreCase).ToArray();
            }, typeof(TEntity), tags) ?? [];
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

    public virtual Expression<Func<TEntity, bool>>? BuildExpressionFromWhereClause<TEntity>(Models.Common.WhereClause? queryNode, bool includeSubClauses = true)
    {
        if (queryNode == null)
            return null;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var body = BuildWhereClauseExpression<TEntity>(queryNode, parameter, includeSubClauses);
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private Expression BuildWhereClauseExpression<TEntity>(Models.Common.WhereClause node, ParameterExpression parameter, bool includeSubClauses = true)
        => node switch {
            ConditionClause condition => BuildConditionExpression<TEntity>(condition, parameter, includeSubClauses),
            GroupClause logical => BuildLogicalExpression<TEntity>(logical, parameter, includeSubClauses),
            var _ => throw new InvalidQueryException($"Unknown query node type: {node.GetType().Name}")
        };

    private static bool IsTrivialRegex(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return false;

        var p = pattern.Trim();
        return p == ".*" || p == "^.*$" || p == "\\A.*\\z" || p == "(?s).*";
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

    private Expression BuildConditionExpression<TEntity>(ConditionClause condition, ParameterExpression parameter, bool includeSubClauses = true)
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
            ValidateNullComparison(condition.Comparison, typeof(int), condition.Value, condition.Field);
            var (parsedCountValue, parsedCountMultiple) = ParseFilterValue(condition.Value, condition.Comparison, typeof(int));
            var valueExpr = GetValueExpression(parsedCountValue, typeof(int), countExpression);
            return BuildComparisonExpressionCached(condition.Comparison, typeof(int), countExpression, valueExpr, parsedCountMultiple, parameter);
        }

        if (pathMetadata.CollectionPropertyIndex != null) {
            var collectionIndex = pathMetadata.CollectionPropertyIndex.Value;
            var elementType = pathMetadata.CollectionElementType!;
            Expression collectionExpr = parameter;
            for (var i = 0; i <= collectionIndex; i++)
                collectionExpr = Expression.Property(collectionExpr, pathMetadata.Properties[i]);

            var elementParam = Expression.Parameter(elementType, "e");
            Expression elementPropExpr = elementParam;
            for (var j = collectionIndex + 1; j < pathMetadata.Properties.Count; j++)
                elementPropExpr = Expression.Property(elementPropExpr, pathMetadata.Properties[j]);

            var elementPropType = pathMetadata.FinalType;
            ValidateNullComparison(condition.Comparison, elementPropType, condition.Value, condition.Field);
            var filterParseTypeElement = GetFilterValueParseType(elementPropType, condition.Comparison);
            var (parsedValueElement, parsedValuesElement) = ParseFilterValue(condition.Value, condition.Comparison, filterParseTypeElement);
            if (condition.Comparison is ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex && parsedValueElement is string pvElem && IsTrivialRegex(pvElem))
                return Expression.Constant(true);

            var valueExprElement = GetRhsConstantExpression(parsedValueElement, filterParseTypeElement, elementPropExpr, condition.Comparison);
            var comparisonElement = BuildComparisonExpressionCached(condition.Comparison, elementPropType, elementPropExpr, valueExprElement, parsedValuesElement, elementParam);
            var anyMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);

            var predicate = Expression.Lambda(comparisonElement, elementParam);
            return Expression.Call(anyMethod, collectionExpr, predicate);
        }

        var property = GetPropertyExpression<TEntity>(condition.Field, parameter);
        var (targetExpression, propertyType) = AdjustForCollectionCached(property);
        ValidateNullComparison(condition.Comparison, propertyType, condition.Value, condition.Field);
        var filterParseType = GetFilterValueParseType(propertyType, condition.Comparison);
        var (parsedSingle, parsedMultiple) = ParseFilterValue(condition.Value, condition.Comparison, filterParseType);
        if (condition.Comparison is ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex && parsedSingle is string ps && IsTrivialRegex(ps))
            return Expression.Constant(true);

        var valueExprTarget = GetRhsConstantExpression(parsedSingle, filterParseType, targetExpression, condition.Comparison);
        var conditionExpr = BuildComparisonExpressionCached(condition.Comparison, propertyType, targetExpression, valueExprTarget, parsedMultiple, parameter);
        if (includeSubClauses && condition.SubClause != null)
            return Expression.AndAlso(conditionExpr, BuildWhereClauseExpression<TEntity>(condition.SubClause, parameter, includeSubClauses));

        return conditionExpr;
    }

    private Expression BuildLogicalExpression<TEntity>(GroupClause logical, ParameterExpression parameter, bool includeSubClauses = true)
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
                if (nodes.Count > 1) {
                    var meta = GetPropertyPathMetadataCached<TEntity>(field);
                    nodes = CombineContainsNodesToRegex(nodes, meta.FinalType);
                    if (meta.CollectionPropertyIndex != null && !meta.IsCountPath) {
                        var collectionIndex = meta.CollectionPropertyIndex.Value;
                        var elementType = meta.CollectionElementType!;
                        Expression collectionExpr = parameter;
                        for (var i = 0; i <= collectionIndex; i++)
                            collectionExpr = Expression.Property(collectionExpr, meta.Properties[i]);

                        var elementParam = Expression.Parameter(elementType, "e");
                        Expression elementPropExpr = elementParam;
                        for (var j = collectionIndex + 1; j < meta.Properties.Count; j++)
                            elementPropExpr = Expression.Property(elementPropExpr, meta.Properties[j]);

                        Expression? innerOr = null;
                        foreach (var cond in nodes) {
                            var (parsedSingle, parsedMultiple) = ParseFilterValue(cond.Value, cond.Comparison, meta.FinalType);
                            if (cond.Comparison is ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex && parsedSingle is string ps && IsTrivialRegex(ps)) {
                                innerOr = Expression.Constant(true);
                                break;
                            }

                            var valueExpr = GetValueExpression(parsedSingle, meta.FinalType, elementPropExpr);
                            var compExpr = BuildComparisonExpressionCached(cond.Comparison, meta.FinalType, elementPropExpr, valueExpr, parsedMultiple, elementParam);
                            innerOr = innerOr == null ? compExpr : Expression.OrElse(innerOr, compExpr);
                        }

                        if (innerOr != null) {
                            var anyMethod = typeof(Enumerable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 2).MakeGenericMethod(elementType);
                            var predicate = Expression.Lambda(innerOr, elementParam);
                            expressions.Add(Expression.Call(anyMethod, collectionExpr, predicate));
                            foreach (var rem in g)
                                remaining.Remove(rem);
                        }
                    }
                    else if (nodes.Count != g.Count()) {
                        Expression? groupedOr = null;
                        foreach (var cond in nodes) {
                            var expr = BuildConditionExpression<TEntity>(cond, parameter, includeSubClauses);
                            groupedOr = groupedOr == null ? expr : Expression.OrElse(groupedOr, expr);
                        }

                        if (groupedOr != null) {
                            expressions.Add(groupedOr);
                            foreach (var rem in g)
                                remaining.Remove(rem);
                        }
                    }
                }
            }

            expressions.AddRange(remaining.Select(child => BuildWhereClauseExpression<TEntity>(child, parameter, includeSubClauses)));
            if (expressions.Count == 0)
                throw new InvalidQueryException("Logical OR produced no expressions");

            var combined = expressions[0];
            for (var i = 1; i < expressions.Count; i++)
                combined = Expression.OrElse(combined, expressions[i]);

            if (includeSubClauses && logical.SubClause != null)
                return Expression.AndAlso(combined, BuildWhereClauseExpression<TEntity>(logical.SubClause, parameter, includeSubClauses));

            return combined;
        }

        var childExpressions = logical.Children.Select(child => BuildWhereClauseExpression<TEntity>(child, parameter, includeSubClauses)).ToList();
        var combinedDefault = childExpressions[0];
        for (var i = 1; i < childExpressions.Count; i++) {
            combinedDefault = logical.Operator == GroupOperatorEnum.And
                ? Expression.AndAlso(combinedDefault, childExpressions[i])
                : Expression.OrElse(combinedDefault, childExpressions[i]);
        }

        if (includeSubClauses && logical.SubClause != null)
            return Expression.AndAlso(combinedDefault, BuildWhereClauseExpression<TEntity>(logical.SubClause, parameter, includeSubClauses));

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
        var cacheKey = $"{EntityMetadata.ComparisonMetadataPrefix}{comparison}_{propertyType.FullName}";
        var metadata = SharedEntityMetadataCache.GetOrAddComparisonMetadata(cacheKey, () => CreateComparisonMetadata(comparison, propertyType));
        return metadata.ComparisonType switch {
            ComparisonType.SimpleComparison => Expression.MakeBinary(metadata.ExpressionType!.Value, target, value),
            ComparisonType.StringMethod => BuildStringComparison(target, value, metadata.StringMethod!, metadata.ToLowerMethod, false),
            ComparisonType.NegatedStringMethod => BuildStringComparison(target, value, metadata.StringMethod!, metadata.ToLowerMethod, true),
            ComparisonType.ExactString => BuildExactComparison(target, value),
            ComparisonType.OneOf => BuildOneOfExpressionCached(propertyType, target, values, false),
            ComparisonType.NotOneOf => BuildOneOfExpressionCached(propertyType, target, values, true),
            ComparisonType.Regex => BuildRegexComparison(target, value, false),
            ComparisonType.NotRegex => BuildRegexComparison(target, value, true),
            var _ => throw new InvalidQueryException($"Comparison '{comparison}' is not supported for type '{propertyType.Name}'")
        };
    }

    protected virtual Expression BuildStringComparison(Expression target, Expression value, MethodInfo stringMethod, MethodInfo? toLowerMethod, bool negate)
    {
        var stringTarget = target.Type == typeof(string) ? target : Expression.Call(target, typeof(object).GetMethod("ToString")!);
        var stringValue = value.Type == typeof(string) ? value : Expression.Call(value, typeof(object).GetMethod("ToString")!);
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

    protected virtual Expression BuildExactComparison(Expression target, Expression value) => Expression.Equal(target, value);

    protected virtual Expression BuildRegexComparison(Expression target, Expression value, bool negate)
    {
        var regexIsMatch = typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string), typeof(string)]);
        OperationHelpers.ThrowIfNull(regexIsMatch, "Could not find Regex.IsMatch method");
        var targetAsString = target.Type == typeof(string) ? target : Expression.Call(target, typeof(object).GetMethod("ToString")!);
        var notNull = Expression.NotEqual(targetAsString, Expression.Constant(null, typeof(string)));
        var call = Expression.Call(regexIsMatch, targetAsString, value);
        var combined = Expression.AndAlso(notNull, call);
        return negate ? Expression.Not(combined) : combined;
    }

    private static string GenerateWhereClauseCacheKey<TEntity>(Models.Common.WhereClause queryNode)
    {
        var sb = new StringBuilder(MatcherCachePrefix);
        sb.Append('_').Append(typeof(TEntity).FullName).Append('_');
        AppendWhereClauseHash(queryNode, sb);
        return sb.ToString();
    }

    private static void AppendWhereClauseHash(Models.Common.WhereClause node, StringBuilder sb)
    {
        switch (node) {
            case ConditionClause c:
                sb.Append("C(").Append(c.Field).Append(c.Comparison);
                AppendValueHash(c.Value, sb);
                if (c.SubClause != null) {
                    sb.Append("Sub(");
                    AppendWhereClauseHash(c.SubClause, sb);
                    sb.Append(")");
                }

                sb.Append(')');
                break;
            case GroupClause l:
                sb.Append("L(").Append(l.Operator).Append('[');
                var first = true;
                foreach (var child in l.Children) {
                    if (!first)
                        sb.Append(',');

                    AppendWhereClauseHash(child, sb);
                    first = false;
                }

                sb.Append("]");
                if (l.SubClause != null) {
                    sb.Append("Sub(");
                    AppendWhereClauseHash(l.SubClause, sb);
                    sb.Append(")");
                }

                sb.Append(")");
                break;
        }
    }

    private static void AppendValueHash(object? value, StringBuilder sb)
    {
        if (value == null) {
            sb.Append("null");
            return;
        }

        switch (value) {
            case string s:
                sb.Append("str:").Append(s.Length).Append(':').Append(s.GetHashCode(StringComparison.Ordinal));
                break;
            case int i:
                sb.Append("int:").Append(i);
                break;
            case long l:
                sb.Append("long:").Append(l);
                break;
            case double d:
                sb.Append("double:").Append(d.GetHashCode());
                break;
            case JsonElement je:
                sb.Append("json:").Append(je.ValueKind).Append(':').Append(je.GetHashCode());
                break;
            default:
                sb.Append("obj:").Append(value.GetType().Name).Append(':').Append(value.GetHashCode());
                break;
        }
    }

    private int CompareValues(object? left, object? right)
    {
        if (left == null && right == null)
            return 0;

        if (left == null)
            return -1;

        if (right == null)
            return 1;

        if (left is not IComparable comparable)
            return 0;

        var rightConverted = ValueConversion.ConvertToTargetType(right, left.GetType());
        return comparable.CompareTo(rightConverted);
    }

    private static string ConvertToString(object? value)
    {
        if (value == null)
            return string.Empty;

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return jsonElement.GetString() ?? string.Empty;

        return value.ToString() ?? string.Empty;
    }

    private bool MatchesOneOf(object? propertyValue, object? filterValue, Type propertyType)
    {
        if (filterValue == null)
            return false;

        IEnumerable<object> values;
        switch (filterValue) {
            case string str:
                values = str.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                break;
            case JsonElement { ValueKind: JsonValueKind.String } jsonStr:
                var jsonStringValue = jsonStr.GetString() ?? string.Empty;
                values = jsonStringValue.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                break;
            case JsonElement { ValueKind: JsonValueKind.Array } jsonArray:
                values = jsonArray.EnumerateArray().Select(e => (object)e);
                break;
            case IEnumerable<object> enumerable:
                values = enumerable;
                break;
            case IEnumerable enumerable and not string:
                values = enumerable.Cast<object>();
                break;
            default:
                values = [filterValue];
                break;
        }

        return values.Any(v => Equals(propertyValue, ValueConversion.ConvertToTargetType(v, propertyType)));
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
                    PropertyInfo? parentProp = null;
                    PropertyInfo? nestedProp = null;
                    foreach (var cand in currentType.GetProperties(PropertySearchFlags)) {
                        var candNested = ResolvePropertyCached(cand.PropertyType, part);
                        if (candNested != null) {
                            parentProp = cand;
                            nestedProp = candNested;
                            break;
                        }

                        if (!typeof(IEnumerable).IsAssignableFrom(cand.PropertyType) || cand.PropertyType == typeof(string) || cand.PropertyType == typeof(byte[]))
                            continue;

                        var elemType = SharedEntityMetadataCache.GetCollectionElementType(cand.PropertyType);
                        var elemNested = ResolvePropertyCached(elemType, part);
                        if (elemNested == null)
                            continue;

                        parentProp = cand;
                        nestedProp = elemNested;
                        break;
                    }

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

        var cacheKey = $"{EntityMetadata.ReflectedMethodPrefix}Contains_{type.FullName}";
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
        var cacheKey = $"{EntityMetadata.OrderMethodPrefix}{typeof(TSource).FullName}_{methodName}_{keyType.FullName}";
        return SharedEntityMetadataCache.GetOrAddOrderMethod(
            cacheKey, () => QueryableOrderMethods.Single(m => m.Name == methodName && m.GetParameters().Length == 2).MakeGenericMethod(typeof(TSource), keyType));
    }

    private MethodInfo GetEnumerableCountMethod(Type elementType)
    {
        var cacheKey = $"{EntityMetadata.ReflectedMethodPrefix}EnumerableCount_{elementType.FullName}";
        return SharedEntityMetadataCache.GetOrAddReflectedMethod(
            cacheKey, () => typeof(Enumerable).GetMethods().First(m => m.Name == "Count" && m.GetParameters().Length == 1).MakeGenericMethod(elementType));
    }

    private MethodInfo GetAsQueryableMethod(Type elementType)
    {
        var cacheKey = $"{EntityMetadata.ReflectedMethodPrefix}AsQueryable_{elementType.FullName}";
        return SharedEntityMetadataCache.GetOrAddReflectedMethod(
            cacheKey,
            () => typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "AsQueryable" && m.GetParameters().Length == 1 && m.IsGenericMethod)
                .MakeGenericMethod(elementType));
    }

    private MethodInfo GetQueryableCountMethod(Type elementType)
    {
        var cacheKey = $"{EntityMetadata.ReflectedMethodPrefix}QueryableCount_{elementType.FullName}";
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
                ComparisonType.StringMethod, ToLowerMethod: typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!,
                StringMethod: typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!),
            ComparisonOperatorEnum.NotContains when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.NegatedStringMethod, ToLowerMethod: typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!,
                StringMethod: typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!),
            ComparisonOperatorEnum.StartsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.StringMethod, ToLowerMethod: typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!,
                StringMethod: typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!),
            ComparisonOperatorEnum.EndsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.StringMethod, ToLowerMethod: typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!,
                StringMethod: typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!),
            ComparisonOperatorEnum.NotStartsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.NegatedStringMethod, ToLowerMethod: typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!,
                StringMethod: typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!),
            ComparisonOperatorEnum.NotEndsWith when SupportsStringMethodComparisons(propertyType) => new(
                ComparisonType.NegatedStringMethod, ToLowerMethod: typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!,
                StringMethod: typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!),
            ComparisonOperatorEnum.In => new(ComparisonType.OneOf),
            ComparisonOperatorEnum.NotIn => new(ComparisonType.NotOneOf),
            ComparisonOperatorEnum.Regex when SupportsStringMethodComparisons(propertyType) => new(ComparisonType.Regex),
            ComparisonOperatorEnum.NotRegex when SupportsStringMethodComparisons(propertyType) => new(ComparisonType.NotRegex),
            var _ => throw new InvalidQueryException($"Comparison '{comparison}' is not supported for type '{propertyType.Name}'")
        };

    /// <summary>Guid (and nullable Guid) use the same string operations as <see cref="string" />; values are compared on canonical <see cref="Guid.ToString" /> forms (aligned with typical SQL string casts).</summary>
    private static bool IsGuidLikeType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(Guid);
    }

    private static bool IsStringOrRegexComparison(ComparisonOperatorEnum comparison) =>
        comparison is ComparisonOperatorEnum.Contains
            or ComparisonOperatorEnum.NotContains
            or ComparisonOperatorEnum.StartsWith
            or ComparisonOperatorEnum.EndsWith
            or ComparisonOperatorEnum.NotStartsWith
            or ComparisonOperatorEnum.NotEndsWith
            or ComparisonOperatorEnum.Regex
            or ComparisonOperatorEnum.NotRegex;

    /// <summary>Types that support Contains/StartsWith/EndsWith/Regex using string semantics (including Guid, matched via string representation).</summary>
    private static bool SupportsStringMethodComparisons(Type propertyType) =>
        propertyType == typeof(string) || IsGuidLikeType(propertyType);

    private static Type GetFilterValueParseType(Type propertyType, ComparisonOperatorEnum comparison)
    {
        if (SupportsStringMethodComparisons(propertyType) && IsStringOrRegexComparison(comparison))
            return typeof(string);

        return propertyType;
    }

    /// <summary>RHS for string-method / regex filters on Guid must stay <see cref="string"/> (partial fragments); do not convert to <see cref="Guid"/>.</summary>
    private static Expression GetRhsConstantExpression(object? parsedValue, Type parseType, Expression propertyExpression, ComparisonOperatorEnum comparison)
    {
        if (parseType == typeof(string)
            && propertyExpression.Type != typeof(string)
            && IsGuidLikeType(propertyExpression.Type)
            && IsStringOrRegexComparison(comparison))
            return Expression.Constant(parsedValue, typeof(string));

        return GetValueExpression(parsedValue, parseType, propertyExpression);
    }

    private static Expression GetValueExpression(object? value, Type targetType, Expression targetExpression)
    {
        var constant = Expression.Constant(value, value == null ? typeof(object) : targetType);
        return constant.Type != targetExpression.Type ? Expression.Convert(constant, targetExpression.Type) : constant;
    }

    private static string GetOrderingMethodName<T>(IQueryable<T> source, SortDirection sortDirection)
        => !IsQueryableOrdered(source) ? sortDirection == SortDirection.Desc ? "OrderByDescending" : "OrderBy" :
            sortDirection == SortDirection.Desc ? "ThenByDescending" : "ThenBy";

    private static bool IsQueryableOrdered<T>(IQueryable<T> source)
    {
        var expression = source.Expression;
        if (expression is not MethodCallExpression methodCall)
            return false;

        var methodName = methodCall.Method.Name;
        return methodName is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending";
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
                    if (!string.IsNullOrEmpty(jsonStringValue)) {
                        var jsonParts = jsonStringValue.Split(',');
                        foreach (var part in jsonParts) {
                            var trimmed = part.Trim();
                            if (trimmed.Length > 0)
                                list.Add(ValueConversion.ConvertToTargetType(trimmed, propertyType)!);
                        }
                    }

                    break;
                case JsonElement { ValueKind: JsonValueKind.Array } jsonElement:
                    foreach (var item in jsonElement.EnumerateArray())
                        list.Add(ValueConversion.ConvertToTargetType(item, propertyType)!);

                    break;
                default:
                    if (ValueConversion.IsObjectEnumerable(value)) {
                        foreach (var item in (IEnumerable)value)
                            list.Add(ValueConversion.ConvertToTargetType(item, propertyType)!);
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

    public virtual bool Match<TEntity>(TEntity entity, Models.Common.WhereClause? queryNode)
    {
        if (entity == null)
            return false;

        if (queryNode == null)
            return true;

        var expr = BuildExpressionFromWhereClause<TEntity>(queryNode);
        if (expr == null)
            return true;

        var func = expr.Compile();
        return func(entity);
    }

    public virtual bool Match<TEntity>(TEntity entity, QueryReq? queryRequest)
    {
        if (entity == null)
            return false;

        if (queryRequest is null)
            return true;

        return queryRequest.WhereClause == null || Match(entity, queryRequest.WhereClause);
    }

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
        catch (InvalidQueryException ex) {
            errorMessage = ex.Message;
            return false;
        }
        catch (Exception ex) {
            errorMessage = ex.Message;
            return false;
        }
    }
}