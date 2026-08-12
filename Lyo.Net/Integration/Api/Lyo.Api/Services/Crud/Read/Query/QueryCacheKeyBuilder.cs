using System.Text;
using Lyo.Cache;
using Lyo.Common.Enums;
using Lyo.Hashing;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;

// ReSharper disable ConvertClosureToMethodGroup

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Builds cache keys for query results. Shared by QueryService. Tags use <see cref="QueryCacheTagBuilder" />.</summary>
public static class QueryCacheKeyBuilder
{
    private const int MaxCacheSegmentInlineChars = 512;

    /// <summary>
    /// Cache key for GET-by-primary-key responses. The base segment matches <see cref="QueryCacheTagBuilder.EntityInstanceTag" /> for the same PK values (EF key order), so
    /// <see cref="ICacheService.InvalidateCacheItem" /> and tag invalidation stay aligned.
    /// </summary>
    public static string BuildSingleEntityGetCacheKey(
        Type entityClrType,
        IReadOnlyList<object?> primaryKeyValuesInEfOrder,
        IReadOnlyList<string>? includes = null,
        bool rawResponse = false)
        => BuildSingleEntityGetCacheKeyCore(entityClrType, primaryKeyValuesInEfOrder, includes, rawResponse);

    /// <inheritdoc cref="BuildSingleEntityGetCacheKey(System.Type,System.Collections.Generic.IReadOnlyList{object?},System.Collections.Generic.IReadOnlyList{string}?,bool)" />
    public static string BuildSingleEntityGetCacheKey(Type entityClrType, object[] primaryKeyValuesInEfOrder, IReadOnlyList<string>? includes = null, bool rawResponse = false)
    {
        var wrapped = new object?[primaryKeyValuesInEfOrder.Length];
        Array.Copy(primaryKeyValuesInEfOrder, wrapped, primaryKeyValuesInEfOrder.Length);
        return BuildSingleEntityGetCacheKeyCore(entityClrType, wrapped, includes, rawResponse);
    }

    /// <summary>
    /// Shared implementation. Public overloads for PK values as an array vs <see cref="IReadOnlyList{T}" /> must not delegate to each other: an array argument can bind to the
    /// array overload again and recurse infinitely.
    /// </summary>
    private static string BuildSingleEntityGetCacheKeyCore(Type entityClrType, IReadOnlyList<object?> primaryKeyValuesInEfOrder, IReadOnlyList<string>? includes, bool rawResponse)
    {
        var key = QueryCacheTagBuilder.EntityInstanceTag(entityClrType, primaryKeyValuesInEfOrder);
        if (includes is { Count: > 0 }) {
            var includeKey = string.Join("|", includes.Order(StringComparer.OrdinalIgnoreCase));
            key += ":include=" + CompactCacheSegment(includeKey);
        }

        if (rawResponse)
            key += ":raw";

        return key;
    }

    /// <summary>Cache key for <c>/QueryConcrete</c> (full entities). Does not include projection dimensions.</summary>
    public static string Build<TDb, TResponse>(QueryConcreteReq queryRequest)
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
            var sortKey = BuildSortKey(queryRequest.SortBy);
            keyBuilder.Append($":sortBy={sortKey}");
        }

        if (queryRequest.Keys.Count != 0) {
            var keySets = queryRequest.Keys.Select(ks => string.Join("|", ks.Select(k => k.ToString() ?? "null")));
            keyBuilder.Append($":keys={string.Join(";", keySets)}");
        }

        if (queryRequest.Include.Count != 0) {
            var includeKey = string.Join("|", NormalizePathValues(queryRequest.Include));
            keyBuilder.Append($":include={CompactCacheSegment(includeKey)}");
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Cache key for an entity load that must align with a projected query (load-then-project fallback): same as <see cref="Build{TDb, TResponse}(QueryConcreteReq)" /> plus
    /// optional projection dimensions.
    /// </summary>
    public static string BuildEntityLoadWithProjectionDimensions<TDb, TResponse>(
        QueryConcreteReq queryRequest,
        IReadOnlyList<string> selectForCacheKey,
        IReadOnlyList<ComputedField> computedForCacheKey)
        where TDb : class
    {
        var keyBuilder = new StringBuilder(Build<TDb, TResponse>(queryRequest));
        if (selectForCacheKey.Count != 0) {
            var selectKey = string.Join("|", NormalizePathValues(selectForCacheKey));
            keyBuilder.Append($":select={CompactCacheSegment(selectKey)}");
        }

        AppendComputedFieldsKey(keyBuilder, computedForCacheKey);
        return keyBuilder.ToString();
    }

    /// <summary>Cache key for <c>/QueryProject</c> SQL / materialized projection paths (includes select + computed).</summary>
    public static string Build<TDb, TResponse>(ProjectionQueryReq queryRequest)
        where TDb : class
    {
        var baseReq = ToQueryConcreteReq(queryRequest);
        return BuildEntityLoadWithProjectionDimensions<TDb, TResponse>(baseReq, queryRequest.Select, queryRequest.ComputedFields);
    }

    /// <summary>Appends QueryProject shape flags so cache entries differ when row columns differ (zip vs parallel sibling collection columns).</summary>
    public static string AppendProjectedShapeSuffix(string cacheKey, bool zipSiblingCollectionSelections) => $"{cacheKey}:zipSibling={zipSiblingCollectionSelections}";

    /// <summary>Cache key for root <c>/Query</c> (From/Joins + Select).</summary>
    public static string BuildRootQuery(QueryReq queryRequest, string contextName)
    {
        var keyBuilder = new StringBuilder(256);
        keyBuilder.Append($"rootquery:{NormalizePathValue(contextName)}");
        keyBuilder.Append($":from={NormalizePathValue(queryRequest.From.EntityType)}:{NormalizePathValue(queryRequest.From.Alias)}");
        if (queryRequest.From.Query?.WhereClause != null)
            keyBuilder.Append($":fromTree={WhereClauseHelpers.GetWhereClauseTreeHash(queryRequest.From.Query.WhereClause)}");

        if (queryRequest.Joins.Count != 0) {
            var joinParts = queryRequest.Joins.Select(j => {
                var on = string.Join("&", j.On.Select(o => $"{NormalizePathValue(o.From)}={NormalizePathValue(o.To)}"));
                var nested = j.Query?.WhereClause != null ? $":jt={WhereClauseHelpers.GetWhereClauseTreeHash(j.Query.WhereClause)}" : "";
                return $"{j.Type}:{NormalizePathValue(j.EntityType)}:{NormalizePathValue(j.Alias)}:as={NormalizePathValue(j.As ?? j.Alias)}:on={on}{nested}";
            });

            keyBuilder.Append($":joins={CompactCacheSegment(string.Join("|", joinParts))}");
        }

        keyBuilder.Append($":start={queryRequest.Start ?? 0}");
        keyBuilder.Append($":amount={queryRequest.Amount}");
        keyBuilder.Append($":countMode={queryRequest.Options.TotalCountMode}");
        if (queryRequest.SortBy.Count != 0)
            keyBuilder.Append($":sortBy={BuildSortKey(queryRequest.SortBy)}");

        if (queryRequest.WhereClause != null)
            keyBuilder.Append($":tree={WhereClauseHelpers.GetWhereClauseTreeHash(queryRequest.WhereClause)}");

        if (queryRequest.Select.Count != 0)
            keyBuilder.Append($":select={CompactCacheSegment(string.Join("|", NormalizePathValues(queryRequest.Select)))}");

        AppendComputedFieldsKey(keyBuilder, queryRequest.ComputedFields);
        return keyBuilder.ToString();
    }

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
        var treeHash = queryTree != null ? WhereClauseHelpers.GetWhereClauseTreeHash(queryTree) : "null";
        var includeArray = includes as string[] ?? [.. NormalizePathValues(includes)];
        var includeStr = includeArray.Length != 0 ? $":include={CompactCacheSegment(string.Join("|", includeArray))}" : "";
        var sortStr = sortBy.Length > 0 ? $":sortBy={BuildSortKey(sortBy)}" : "";
        var keysStr = keys != null && keys.Count > 0 ? $":keys={string.Join(";", keys.Select(ks => string.Join("|", ks.Select(k => k.ToString() ?? "null"))))}" : "";
        var selectedFieldsArray = selectedFields as string[] ?? [.. NormalizePathValues(selectedFields ?? [])];
        var selectStr = selectedFields != null && selectedFieldsArray.Length != 0 ? $":select={CompactCacheSegment(string.Join("|", selectedFieldsArray))}" : "";
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

        var computedKey = string.Join("|", computedFields.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Select(c => $"{NormalizePathValue(c.Name)}={c.Template.Trim()}"));
        keyBuilder.Append($":computed={CompactCacheSegment(computedKey)}");
    }

    private static string BuildSortKey(IReadOnlyList<SortBy> sortBy)
    {
        var ordered = sortBy.Select((f, i) => (Field: f, EffectivePriority: f.Priority ?? i))
            .OrderBy(x => x.EffectivePriority)
            .ThenBy(x => x.Field.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Field.Direction)
            .ToArray();

        return string.Join("|", ordered.Select((x, index) => $"{NormalizePathValue(x.Field.PropertyName)}:{x.Field.Direction}:{index}"));
    }

    private static IEnumerable<string> NormalizePathValues(IEnumerable<string> values)
        => values.Select(NormalizePathValue)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase);

    private static string NormalizePathValue(string value) => value.Trim().ToLowerInvariant();

    private static string CompactCacheSegment(string segment)
    {
        if (segment.Length <= MaxCacheSegmentInlineChars)
            return segment;

        var hash = HashingService.Shared.Hash(ContentDigestAlgorithm.Sha256, Encoding.UTF8.GetBytes(segment));
        return $"sha256:{HashingService.Shared.ToHex(hash, TextLetterCase.Lower)}";
    }

    private static QueryConcreteReq ToQueryConcreteReq(ProjectionQueryReq p)
        => new() {
            Start = p.Start,
            Amount = p.Amount,
            Keys = [.. p.Keys.Select(k => k.ToArray())],
            WhereClause = p.WhereClause,
            Include = [.. p.Include],
            SortBy = [.. p.SortBy],
            Options = new() { TotalCountMode = p.Options.TotalCountMode, IncludeFilterMode = p.Options.IncludeFilterMode }
        };
}