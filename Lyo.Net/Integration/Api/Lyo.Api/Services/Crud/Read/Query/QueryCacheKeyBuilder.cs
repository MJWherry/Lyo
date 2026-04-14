using System.Text;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;

// ReSharper disable ConvertClosureToMethodGroup

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Builds cache keys for query results. Shared by QueryService.</summary>
public static class QueryCacheKeyBuilder
{
    /// <summary>Cache key for <c>/Query</c> (full entities). Does not include projection dimensions.</summary>
    public static string Build<TDb, TResponse>(QueryReq queryRequest)
        where TDb : class
    {
        var options = queryRequest.Options;
        var keyBuilder = new StringBuilder(256);
        keyBuilder.Append($"query:{typeof(TDb).Name.ToLowerInvariant()}:{typeof(TResponse).Name.ToLowerInvariant()}");
        keyBuilder.Append($":start={queryRequest.Start ?? 0}");
        keyBuilder.Append($":amount={queryRequest.Amount}");
        keyBuilder.Append($":countMode={options.TotalCountMode}");
        keyBuilder.Append($":includeFilterMode={options.IncludeFilterMode}");
        if (queryRequest.SortBy.Any()) {
            var sortKey = string.Join(
                "|",
                queryRequest.SortBy.Select((f, i) => (f, EffectivePriority: f.Priority ?? i))
                    .OrderBy(x => x.f.PropertyName)
                    .Select(x => $"{x.f.PropertyName}:{x.f.Direction}:{x.EffectivePriority}"));

            keyBuilder.Append($":sortBy={sortKey}");
        }

        if (queryRequest.Keys.Count != 0) {
            var keySets = queryRequest.Keys.Select(ks => string.Join("|", ks.Select(k => k.ToString() ?? "null")));
            keyBuilder.Append($":keys={string.Join(";", keySets)}");
        }

        if (queryRequest.Include.Count != 0) {
            var includeKey = string.Join("|", queryRequest.Include.OrderBy(i => i));
            keyBuilder.Append($":include={includeKey}");
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Cache key for an entity load that must align with a projected query (load-then-project fallback): same as <see cref="Build{TDb, TResponse}(QueryReq)" /> plus optional projection dimensions.
    /// </summary>
    public static string BuildEntityLoadWithProjectionDimensions<TDb, TResponse>(
        QueryReq queryRequest,
        IReadOnlyList<string> selectForCacheKey,
        IReadOnlyList<ComputedField> computedForCacheKey)
        where TDb : class
    {
        var keyBuilder = new StringBuilder(Build<TDb, TResponse>(queryRequest));
        if (selectForCacheKey.Count != 0) {
            var selectKey = string.Join("|", selectForCacheKey.OrderBy(i => i));
            keyBuilder.Append($":select={selectKey}");
        }

        AppendComputedFieldsKey(keyBuilder, computedForCacheKey);
        return keyBuilder.ToString();
    }

    /// <summary>Cache key for <c>/QueryProject</c> SQL / materialized projection paths (includes select + computed).</summary>
    public static string Build<TDb, TResponse>(ProjectionQueryReq queryRequest)
        where TDb : class
    {
        var baseReq = ToQueryReq(queryRequest);
        return BuildEntityLoadWithProjectionDimensions<TDb, TResponse>(baseReq, queryRequest.Select, queryRequest.ComputedFields);
    }

    /// <summary>Appends QueryProject shape flags so cache entries differ when row columns differ (zip vs parallel sibling collection columns).</summary>
    public static string AppendProjectedShapeSuffix(string cacheKey, bool zipSiblingCollectionSelections)
        => $"{cacheKey}:zipSibling={zipSiblingCollectionSelections}";

    public static string BuildTree<TDbModel, TResult>(
        WhereClause? queryTree,
        int? start,
        int? amount,
        IEnumerable<string> includes,
        SortBy[] sortBy,
        QueryTotalCountMode totalCountMode,
        QueryIncludeFilterMode includeFilterMode,
        IReadOnlyList<object[]>? keys = null,
        IEnumerable<string>? selectedFields = null,
        IReadOnlyList<ComputedField>? computedFields = null)
    {
        var typeName = typeof(TDbModel).Name;
        var resultName = typeof(TResult).Name;
        var treeHash = queryTree != null ? WhereClauseUtils.GetWhereClauseTreeHash(queryTree) : "null";
        var includeArray = includes as string[] ?? includes.Order().ToArray();
        var includeStr = includeArray.Length != 0 ? $":include={string.Join("|", includeArray)}" : "";
        var sortStr = sortBy.Length > 0
            ? $":sortBy={string.Join("|", sortBy.Select((f, i) => (f, EffectivePriority: f.Priority ?? i)).OrderBy(x => x.f.PropertyName).Select(x => $"{x.f.PropertyName}:{x.f.Direction}:{x.EffectivePriority}"))}"
            : "";

        var keysStr = keys != null && keys.Count > 0 ? $":keys={string.Join(";", keys.Select(ks => string.Join("|", ks.Select(k => k.ToString() ?? "null"))))}" : "";
        var selectedFieldsArray = selectedFields as string[] ?? selectedFields?.Order().ToArray() ?? [];
        var selectStr = selectedFields != null && selectedFieldsArray.Length != 0 ? $":select={string.Join("|", selectedFieldsArray)}" : "";
        var sb = new StringBuilder(256);
        sb.Append(
            $"querytree:{typeName}:{resultName}:start={start}:amount={amount}:countMode={totalCountMode}:includeFilterMode={includeFilterMode}:tree={treeHash}{includeStr}{sortStr}{keysStr}{selectStr}");

        AppendComputedFieldsKey(sb, computedFields);
        return sb.ToString();
    }

    private static void AppendComputedFieldsKey(StringBuilder keyBuilder, IReadOnlyList<ComputedField>? computedFields)
    {
        if (computedFields is not { Count: > 0 })
            return;

        var computedKey = string.Join("|", computedFields.OrderBy(c => c.Name).Select(c => $"{c.Name}={c.Template}"));
        keyBuilder.Append($":computed={computedKey}");
    }

    private static QueryReq ToQueryReq(ProjectionQueryReq p)
        => new() {
            Start = p.Start,
            Amount = p.Amount,
            Keys = [..p.Keys.Select(k => k.ToArray())],
            WhereClause = p.WhereClause,
            Include = [..p.Include],
            SortBy = [..p.SortBy],
            Options = new() {
                TotalCountMode = p.Options.TotalCountMode,
                IncludeFilterMode = p.Options.IncludeFilterMode
            }
        };
}
