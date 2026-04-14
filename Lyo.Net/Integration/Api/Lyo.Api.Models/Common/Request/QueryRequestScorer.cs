using System.Collections;
using System.Text.RegularExpressions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Models.Common.Request;

public static class QueryRequestScorer
{
    /// <summary>Matches SmartFormat-style placeholders <c>{Name}</c> and <c>{CreatedAt:yyyy}</c> (name segment only); same pattern as formatter placeholder extraction.</summary>
    private static readonly Regex TemplatePlaceholderRegex = new(@"\{([^{}:|]+)", RegexOptions.Compiled);

    public static int Score(QueryReq? request) => ScoreDetailed(request).TotalScore;

    public static int Score(ProjectionQueryReq? request) => ScoreDetailed(request).TotalScore;

    public static QueryRequestScoreBreakdown ScoreDetailed(QueryReq? request)
    {
        if (request is null)
            return QueryRequestScoreBreakdown.Empty();

        var pagingScore = ScorePaging(request.Start, request.Amount);
        var keysScore = ScoreKeys(request.Keys);
        var sortScore = ScoreSortBy(request.SortBy, out var sortCount);
        var includeScore = ScorePathList(request.Include, out var includeCount, out var includeMaxDepth, out var includeTotalPathSegments);
        const int selectScore = 0;
        const int computedFieldsScore = 0;
        var totalCountModeScore = ScoreTotalCountMode(request.Options.TotalCountMode);
        var (nodeCount, conditionCount, groupClauseCount, maxDepth, subClauseCount, maxSubClauseDepth, maxGroupBranchingFactor, comparisonScore) = AnalyzeWhereClause(request.WhereClause);
        var whereClauseScore = ScoreWhereClause(nodeCount, conditionCount, groupClauseCount, maxDepth, subClauseCount, maxSubClauseDepth, maxGroupBranchingFactor, comparisonScore);
        var total = pagingScore + keysScore + sortScore + includeScore + selectScore + computedFieldsScore + totalCountModeScore + whereClauseScore;
        return new(
            total, pagingScore, keysScore, sortScore, includeScore + selectScore, computedFieldsScore, totalCountModeScore, whereClauseScore, request.Start ?? 0, request.Amount ?? 0,
            includeCount, includeMaxDepth, includeTotalPathSegments, sortCount, request.Keys.Count, nodeCount,
            conditionCount, groupClauseCount, maxDepth, subClauseCount, maxSubClauseDepth, maxGroupBranchingFactor);
    }

    public static QueryRequestScoreBreakdown ScoreDetailed(ProjectionQueryReq? request)
    {
        if (request is null)
            return QueryRequestScoreBreakdown.Empty();

        var pagingScore = ScorePaging(request.Start, request.Amount);
        var keysScore = ScoreKeys(request.Keys);
        var sortScore = ScoreSortBy(request.SortBy, out var sortCount);
        var includeScore = ScorePathList(request.Include, out var includeCount, out var includeMaxDepth, out var includeTotalPathSegments);
        var selectScore = ScorePathList(request.Select, out var selectCount, out var selectMaxDepth, out var selectTotalPathSegments);
        var computedFieldsScore = ScoreComputedFields(request.ComputedFields);
        var totalCountModeScore = ScoreTotalCountMode(request.Options.TotalCountMode);
        var (nodeCount, conditionCount, groupClauseCount, maxDepth, subClauseCount, maxSubClauseDepth, maxGroupBranchingFactor, comparisonScore) = AnalyzeWhereClause(request.WhereClause);
        var whereClauseScore = ScoreWhereClause(nodeCount, conditionCount, groupClauseCount, maxDepth, subClauseCount, maxSubClauseDepth, maxGroupBranchingFactor, comparisonScore);
        var total = pagingScore + keysScore + sortScore + includeScore + selectScore + computedFieldsScore + totalCountModeScore + whereClauseScore;
        return new(
            total, pagingScore, keysScore, sortScore, includeScore + selectScore, computedFieldsScore, totalCountModeScore, whereClauseScore, request.Start ?? 0, request.Amount ?? 0,
            includeCount + selectCount, Math.Max(includeMaxDepth, selectMaxDepth), includeTotalPathSegments + selectTotalPathSegments, sortCount, request.Keys.Count, nodeCount,
            conditionCount, groupClauseCount, maxDepth, subClauseCount, maxSubClauseDepth, maxGroupBranchingFactor);
    }

    private static int ScorePaging(int? start, int? amount)
    {
        var score = 0;
        var effectiveStart = Math.Max(0, start ?? 0);
        var effectiveAmount = Math.Max(0, amount ?? 0);
        if (effectiveStart > 0)
            score += 2 + Math.Min(8, effectiveStart / 1000);

        if (effectiveAmount <= 0)
            return score;

        score += 2;
        score += Math.Min(24, effectiveAmount / 100);
        return score;
    }

    private static int ScoreKeys(List<object[]>? keys)
    {
        if (keys is null || keys.Count == 0)
            return 0;

        var keyRows = keys.Count;
        var keyValues = keys.Sum(k => k?.Length ?? 0);
        return Math.Min(20, keyRows * 2 + keyValues);
    }

    private static int ScoreSortBy(List<SortBy>? sortBy, out int sortCount)
    {
        sortCount = sortBy?.Count ?? 0;
        if (sortCount == 0)
            return 0;

        var score = Math.Min(15, sortCount * 3);
        var priorityPenalty = sortBy!.Select((s, i) => (s, EffectivePriority: s.Priority ?? i)).Where(x => x.EffectivePriority > 0).Sum(x => Math.Min(2, x.EffectivePriority));
        score += Math.Min(8, priorityPenalty);
        return score;
    }

    /// <summary>Scores a list of paths (Include or Select). Factors in navigations (.), wildcards (*), depth, and segment count.</summary>
    private static int ScorePathList(List<string>? paths, out int count, out int maxDepth, out int totalPathSegments)
    {
        count = paths?.Count ?? 0;
        maxDepth = 0;
        totalPathSegments = 0;
        if (paths is null || paths.Count == 0)
            return 0;

        var wildcardCount = 0;
        var score = Math.Min(25, count * 5);
        foreach (var path in paths) {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var segments = path.Split('.');
            var depth = segments.Length;
            maxDepth = Math.Max(maxDepth, depth);
            totalPathSegments += depth;
            foreach (var seg in segments) {
                if (seg.Contains('*'))
                    wildcardCount++;
            }
        }

        score += Math.Min(20, Math.Max(0, maxDepth - 1) * 5);
        score += Math.Min(20, totalPathSegments);
        score += Math.Min(15, wildcardCount * 5);
        return score;
    }

    /// <summary>Scores computed columns: a base per formatted field plus cost for distinct placeholder references across templates.</summary>
    private static int ScoreComputedFields(List<ComputedField>? computedFields)
    {
        if (computedFields is null || computedFields.Count == 0)
            return 0;

        var templateCount = 0;
        var uniquePlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cf in computedFields) {
            if (string.IsNullOrWhiteSpace(cf?.Template))
                continue;

            templateCount++;
            foreach (var ph in ExtractTemplatePlaceholders(cf.Template))
                uniquePlaceholders.Add(ph);
        }

        if (templateCount == 0)
            return 0;

        const int basePerComputed = 4;
        const int perUniqueFieldRef = 3;
        var baseScore = Math.Min(20, templateCount * basePerComputed);
        var selectionScore = Math.Min(25, uniquePlaceholders.Count * perUniqueFieldRef);
        return baseScore + selectionScore;
    }

    private static IEnumerable<string> ExtractTemplatePlaceholders(string template)
    {
        foreach (Match match in TemplatePlaceholderRegex.Matches(template)) {
            if (match.Groups.Count > 1 && match.Groups[1].Success) {
                var name = match.Groups[1].Value.Trim();
                if (name.Length > 0)
                    yield return name;
            }
        }
    }

    private static int ScoreTotalCountMode(QueryTotalCountMode mode)
        => mode switch {
            QueryTotalCountMode.Exact => 12,
            QueryTotalCountMode.HasMore => 6,
            var _ => 0
        };

    private static int ScoreWhereClause(
        int nodeCount,
        int conditionCount,
        int groupClauseCount,
        int maxDepth,
        int subClauseCount,
        int maxSubClauseDepth,
        int maxGroupBranchingFactor,
        int comparisonScore)
    {
        if (nodeCount == 0)
            return 0;

        var score = 0;
        score += Math.Min(25, nodeCount * 2);
        score += Math.Min(20, maxDepth * 4);
        score += Math.Min(20, subClauseCount * 6);
        score += Math.Min(20, maxSubClauseDepth * 5);
        score += Math.Min(15, Math.Max(0, maxGroupBranchingFactor - 1) * 3);
        score += Math.Min(40, comparisonScore);
        return score;
    }

    private static (int NodeCount, int ConditionCount, int GroupClauseCount, int MaxDepth, int SubClauseCount, int MaxSubClauseDepth, int MaxGroupBranchingFactor, int ComparisonScore)
        AnalyzeWhereClause(WhereClause? root)
    {
        if (root is null)
            return (0, 0, 0, 0, 0, 0, 0, 0);

        var visited = new HashSet<WhereClause>();
        var acc = new WhereClauseAnalysisAccumulator();
        AnalyzeWhereClauseRecursive(root, 1, 0, acc, visited);
        return (acc.NodeCount, acc.ConditionCount, acc.GroupClauseCount, acc.MaxDepth, acc.SubClauseCount, acc.MaxSubClauseDepth, acc.MaxGroupBranchingFactor, acc.ComparisonScore);
    }

    private static void AnalyzeWhereClauseRecursive(WhereClause node, int depth, int subQueryDepth, WhereClauseAnalysisAccumulator acc, HashSet<WhereClause> visited)
    {
        if (!visited.Add(node))
            return;

        acc.NodeCount++;
        acc.MaxDepth = Math.Max(acc.MaxDepth, depth);
        if (node is GroupClause logical) {
            acc.GroupClauseCount++;
            acc.MaxGroupBranchingFactor = Math.Max(acc.MaxGroupBranchingFactor, logical.Children?.Count ?? 0);
            if (logical.Children is not null) {
                foreach (var child in logical.Children) {
                    if (child is null)
                        continue;

                    AnalyzeWhereClauseRecursive(child, depth + 1, subQueryDepth, acc, visited);
                }
            }

            if (logical.SubClause is null)
                return;

            acc.SubClauseCount++;
            var nextSubDepth = subQueryDepth + 1;
            acc.MaxSubClauseDepth = Math.Max(acc.MaxSubClauseDepth, nextSubDepth);
            AnalyzeWhereClauseRecursive(logical.SubClause, depth + 1, nextSubDepth, acc, visited);
            return;
        }

        if (node is ConditionClause condition) {
            acc.ConditionCount++;
            acc.ComparisonScore += ComparisonComplexity(condition.Comparison, condition.Value);
            if (condition.SubClause is null)
                return;

            acc.SubClauseCount++;
            var nextSubDepth = subQueryDepth + 1;
            acc.MaxSubClauseDepth = Math.Max(acc.MaxSubClauseDepth, nextSubDepth);
            AnalyzeWhereClauseRecursive(condition.SubClause, depth + 1, nextSubDepth, acc, visited);
        }
    }

    private static int ComparisonComplexity(ComparisonOperatorEnum comparison, object? value)
        => comparison switch {
            ComparisonOperatorEnum.Equals => 1,
            ComparisonOperatorEnum.NotEquals => 1,
            ComparisonOperatorEnum.Contains => 3,
            ComparisonOperatorEnum.NotContains => 4,
            ComparisonOperatorEnum.StartsWith => 2,
            ComparisonOperatorEnum.EndsWith => 2,
            ComparisonOperatorEnum.NotStartsWith => 3,
            ComparisonOperatorEnum.NotEndsWith => 3,
            ComparisonOperatorEnum.GreaterThan => 2,
            ComparisonOperatorEnum.GreaterThanOrEqual => 2,
            ComparisonOperatorEnum.LessThan => 2,
            ComparisonOperatorEnum.LessThanOrEqual => 2,
            ComparisonOperatorEnum.In => 4 + ValueItemCountComplexity(value),
            ComparisonOperatorEnum.NotIn => 5 + ValueItemCountComplexity(value),
            ComparisonOperatorEnum.Regex => 8 + RegexLengthComplexity(value),
            ComparisonOperatorEnum.NotRegex => 9 + RegexLengthComplexity(value),
            var _ => 1
        };

    private static int ValueItemCountComplexity(object? value)
    {
        var count = ValueItemCount(value);
        return count <= 0 ? 0 : Math.Min(10, Math.Max(1, count / 5));
    }

    private static int RegexLengthComplexity(object? value)
    {
        if (value is null)
            return 0;

        var len = value.ToString()?.Length ?? 0;
        return Math.Min(6, len / 20);
    }

    private static int ValueItemCount(object? value)
    {
        switch (value) {
            case null:
            case string s when string.IsNullOrWhiteSpace(s):
                return 0;
            case string s:
                return s.Split(',').Count(i => !string.IsNullOrWhiteSpace(i));
            case IEnumerable enumerable: {
                var count = 0;
                foreach (var _ in enumerable)
                    count++;

                return count;
            }
            default:
                return 1;
        }
    }

    /// <summary>Mutable accumulator used during recursive query node analysis; converted to immutable WhereClauseAnalysis at completion.</summary>
    private sealed class WhereClauseAnalysisAccumulator
    {
        public int ComparisonScore;
        public int ConditionCount;
        public int GroupClauseCount;
        public int MaxDepth;
        public int MaxGroupBranchingFactor;
        public int MaxSubClauseDepth;
        public int NodeCount;
        public int SubClauseCount;
    }
}