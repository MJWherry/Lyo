using System.Linq.Expressions;
using System.Text;
using Lyo.Cache;
using Lyo.Common.Core.Enums;
using Lyo.Hashing;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;

// ReSharper disable ConvertClosureToMethodGroup

namespace Lyo.Api.Services.Crud.Read.Query;

/// <summary>Builds cache keys for query results. Shared by QueryService. Tags come from <see cref="QueryCacheTagBuilder" />.</summary>
public static class QueryCacheKeyBuilder
{
    private const int MaxCacheSegmentInlineChars = 512;

    /// <summary>
    /// Cache key for GET-by-primary-key responses. The base segment matches <see cref="QueryCacheTagBuilder.EntityInstanceTag" /> for the same PK values (EF key order), so
    /// <see cref="ICacheService.InvalidateCacheItem" /> and tag invalidation stay aligned.
    /// </summary>
    /// <param name="entityClrType">The entity CLR type.</param>
    /// <param name="primaryKeyValuesInEfOrder">Primary key values in EF key property order.</param>
    /// <param name="includes">Include paths applied to the load, if any.</param>
    /// <param name="rawResponse">Whether the entry holds the unmapped entity rather than the mapped response.</param>
    /// <param name="responseType">
    /// Mapped response type. Required whenever the same entity is served through more than one shape: without it <c>Get&lt;Widget, WidgetSummary&gt;</c> and
    /// <c>Get&lt;Widget, WidgetDetail&gt;</c> resolve to one key and whichever runs first answers for both.
    /// </param>
    /// <param name="hookPartition">
    /// Opaque discriminator for callers that pass before/after hooks. Hooks mutate the result but are invisible to the key, so two callers with different hooks would
    /// otherwise share an entry. Callers that supply hooks must supply a partition; callers that do not can leave it null.
    /// </param>
    public static string BuildSingleEntityGetCacheKey(
        Type entityClrType,
        IReadOnlyList<object?> primaryKeyValuesInEfOrder,
        IReadOnlyList<string>? includes = null,
        bool rawResponse = false,
        Type? responseType = null,
        string? hookPartition = null)
        => BuildSingleEntityGetCacheKeyCore(entityClrType, primaryKeyValuesInEfOrder, includes, rawResponse, responseType, hookPartition);

    /// <inheritdoc cref="BuildSingleEntityGetCacheKey(System.Type,System.Collections.Generic.IReadOnlyList{object?},System.Collections.Generic.IReadOnlyList{string}?,bool,System.Type?,string?)" />
    public static string BuildSingleEntityGetCacheKey(
        Type entityClrType,
        object[] primaryKeyValuesInEfOrder,
        IReadOnlyList<string>? includes = null,
        bool rawResponse = false,
        Type? responseType = null,
        string? hookPartition = null)
    {
        var wrapped = new object?[primaryKeyValuesInEfOrder.Length];
        Array.Copy(primaryKeyValuesInEfOrder, wrapped, primaryKeyValuesInEfOrder.Length);
        return BuildSingleEntityGetCacheKeyCore(entityClrType, wrapped, includes, rawResponse, responseType, hookPartition);
    }

    /// <summary>
    /// Shared implementation. Public overloads for PK values as an array vs <see cref="IReadOnlyList{T}" /> must not delegate to each other: an array argument can bind to the
    /// array overload again and recurse infinitely.
    /// </summary>
    private static string BuildSingleEntityGetCacheKeyCore(
        Type entityClrType,
        IReadOnlyList<object?> primaryKeyValuesInEfOrder,
        IReadOnlyList<string>? includes,
        bool rawResponse,
        Type? responseType,
        string? hookPartition)
    {
        var key = QueryCacheTagBuilder.EntityInstanceTag(entityClrType, primaryKeyValuesInEfOrder);
        if (responseType is not null && responseType != entityClrType)
            key += ":as=" + TypeKeySegment(responseType);

        if (includes is { Count: > 0 }) {
            // Normalized the same way as every other include segment, so ["JobRuns"] and ["jobruns"] resolve to one entry instead of two.
            var includeKey = string.Join("|", NormalizePathValues(includes));
            key += ":include=" + CompactCacheSegment(includeKey);
        }

        if (rawResponse)
            key += ":raw";

        if (!string.IsNullOrEmpty(hookPartition))
            key += ":hooks=" + CompactCacheSegment(NormalizePathValue(hookPartition!));

        return key;
    }

    /// <summary>Cache key for <c>/QueryConcrete</c> (full entities). Leaves out projection dimensions.</summary>
    /// <param name="queryRequest">The query request.</param>
    /// <param name="defaultSortKey">
    /// Fingerprint of the caller's fallback ordering from <c>FormatDefaultSortKey</c>, applied by execution when <see cref="QueryConcreteReq.SortBy" /> is empty. Two
    /// callers with different fallback orders produce different pages for the same request, so the fallback has to be part of the key.
    /// </param>
    public static string Build<TDb, TResponse>(QueryConcreteReq queryRequest, string? defaultSortKey = null)
        where TDb : class
    {
        var options = queryRequest.Options;
        var keyBuilder = new StringBuilder(256);
        keyBuilder.Append($"query:{TypeKeySegment(typeof(TDb))}:{TypeKeySegment(typeof(TResponse))}");
        keyBuilder.Append($":start={queryRequest.Start ?? 0}");
        keyBuilder.Append($":amount={queryRequest.Amount}");
        keyBuilder.Append($":countMode={options.TotalCountMode}");
        keyBuilder.Append($":includeFilterMode={options.IncludeFilterMode}");
        AppendSortKey(keyBuilder, queryRequest.SortBy, defaultSortKey);
        AppendKeySets(keyBuilder, queryRequest.Keys);
        if (queryRequest.Include.Count != 0) {
            var includeKey = string.Join("|", NormalizePathValues(queryRequest.Include));
            keyBuilder.Append($":include={CompactCacheSegment(includeKey)}");
        }

        return keyBuilder.ToString();
    }

    /// <summary>
    /// Fingerprints the fallback ordering a caller applies when a request carries no explicit sort. The expression's own rendering is the fingerprint: it is stable for a given
    /// selector and differs between selectors, which is all the key needs.
    /// </summary>
    /// <param name="defaultOrderExpression">Rendering of the default order key selector.</param>
    /// <param name="defaultSortDirection">Direction applied to that selector.</param>
    public static string FormatDefaultSortKey(string defaultOrderExpression, SortDirection defaultSortDirection)
        => $"{NormalizePathValue(defaultOrderExpression)}:{defaultSortDirection}";

    /// <inheritdoc cref="FormatDefaultSortKey(string,SortDirection)" />
    /// <param name="defaultOrder">The default order key selector.</param>
    /// <param name="defaultSortDirection">Direction applied to that selector.</param>
    public static string FormatDefaultSortKey(Expression defaultOrder, SortDirection defaultSortDirection)
        => FormatDefaultSortKey(defaultOrder.ToString(), defaultSortDirection);

    /// <summary>
    /// Discriminator identifying a set of result-mutating hooks, or null when none were supplied. Hooks change what gets cached but are invisible to the request, so without
    /// this two callers reading the same row through different hooks would share one entry and the first one through would decide what the second saw. Identity comes from each
    /// delegate's declaring type and method name, which is stable for a given call site.
    /// </summary>
    /// <param name="hooks">The hook delegates, in a fixed order; nulls are ignored.</param>
    public static string? FormatHookPartition(params Delegate?[] hooks)
    {
        StringBuilder? sb = null;
        foreach (var hook in hooks) {
            if (hook is null)
                continue;

            sb ??= new(64);
            if (sb.Length > 0)
                sb.Append('+');

            sb.Append(hook.Method.DeclaringType?.FullName).Append('.').Append(hook.Method.Name);
        }

        return sb?.ToString();
    }

    /// <summary>
    /// Cache key for an entity load that must align with a projected query (load-then-project fallback): same as <see cref="Build{TDb, TResponse}(QueryConcreteReq)" /> plus
    /// optional projection dimensions.
    /// </summary>
    public static string BuildEntityLoadWithProjectionDimensions<TDb, TResponse>(
        QueryConcreteReq queryRequest,
        IReadOnlyList<string> selectForCacheKey,
        IReadOnlyList<ComputedField> computedForCacheKey,
        string? defaultSortKey = null)
        where TDb : class
    {
        var keyBuilder = new StringBuilder(Build<TDb, TResponse>(queryRequest, defaultSortKey));
        if (selectForCacheKey.Count != 0) {
            var selectKey = string.Join("|", NormalizePathValues(selectForCacheKey));
            keyBuilder.Append($":select={CompactCacheSegment(selectKey)}");
        }

        AppendComputedFieldsKey(keyBuilder, computedForCacheKey);
        return keyBuilder.ToString();
    }

    /// <summary>Cache key for <c>/QueryProject</c> SQL or materialized projection paths (includes select plus computed).</summary>
    public static string Build<TDb, TResponse>(ProjectionQueryReq queryRequest, string? defaultSortKey = null)
        where TDb : class
    {
        var baseReq = ToQueryConcreteReq(queryRequest);
        return BuildEntityLoadWithProjectionDimensions<TDb, TResponse>(baseReq, queryRequest.Select, queryRequest.ComputedFields, defaultSortKey);
    }

    /// <summary>Appends QueryProject shape flags so cache entries differ when row columns differ (zip versus parallel sibling collection columns).</summary>
    public static string AppendProjectedShapeSuffix(string cacheKey, bool zipSiblingCollectionSelections) => $"{cacheKey}:zipSibling={zipSiblingCollectionSelections}";

    /// <summary>Cache key for root <c>/Query</c> (From/Joins plus Select).</summary>
    /// <param name="queryRequest">The root query request.</param>
    /// <param name="contextName">Identifier for the owning DbContext; pass <see cref="Type.FullName" /> so two contexts sharing a short name stay separate.</param>
    /// <param name="defaultSortKey">Fingerprint of the fallback ordering applied when <see cref="QueryReq.SortBy" /> is empty.</param>
    public static string BuildRootQuery(QueryReq queryRequest, string contextName, string? defaultSortKey = null)
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
        AppendSortKey(keyBuilder, queryRequest.SortBy, defaultSortKey);
        if (queryRequest.WhereClause != null)
            keyBuilder.Append($":tree={WhereClauseHelpers.GetWhereClauseTreeHash(queryRequest.WhereClause)}");

        if (queryRequest.Select.Count != 0)
            keyBuilder.Append($":select={CompactCacheSegment(string.Join("|", NormalizePathValues(queryRequest.Select)))}");

        AppendComputedFieldsKey(keyBuilder, queryRequest.ComputedFields);
        return keyBuilder.ToString();
    }

    /// <summary>
    /// Cache key for a where-clause query. <c>defaultSortKey</c> fingerprints the caller's fallback ordering (see <c>FormatDefaultSortKey</c>), which execution applies when
    /// <c>sortBy</c> is empty and which therefore changes the page returned for an otherwise identical request.
    /// </summary>
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
        IReadOnlyList<ComputedField>? computedFields = null,
        string? defaultSortKey = null)
    {
        var treeHash = queryTree != null ? WhereClauseHelpers.GetWhereClauseTreeHash(queryTree) : "null";
        var includeArray = includes as string[] ?? [.. NormalizePathValues(includes)];
        var includeStr = includeArray.Length != 0 ? $":include={CompactCacheSegment(string.Join("|", includeArray))}" : "";
        var selectedFieldsArray = selectedFields as string[] ?? [.. NormalizePathValues(selectedFields ?? [])];
        var selectStr = selectedFields != null && selectedFieldsArray.Length != 0 ? $":select={CompactCacheSegment(string.Join("|", selectedFieldsArray))}" : "";
        var sb = new StringBuilder(256);
        sb.Append(
            $"querytree:{TypeKeySegment(typeof(TDbModel))}:{TypeKeySegment(typeof(TResult))}:start={start}:amount={amount}:countMode={totalCountMode}:includeFilterMode={includeFilterMode}:tree={treeHash}{includeStr}");

        AppendSortKey(sb, sortBy, defaultSortKey);
        AppendKeySets(sb, keys);
        sb.Append(selectStr);
        AppendComputedFieldsKey(sb, computedFields);
        return sb.ToString();
    }

    /// <summary>
    /// Appends the explicit sort when there is one, and otherwise the caller's fallback ordering. Omitting the fallback let two callers that page the same rows in different
    /// orders share a cache entry, so whichever ran first decided the order both saw.
    /// </summary>
    private static void AppendSortKey(StringBuilder keyBuilder, IReadOnlyList<SortBy> sortBy, string? defaultSortKey)
    {
        if (sortBy.Count > 0)
            keyBuilder.Append($":sortBy={BuildSortKey(sortBy)}");
        else if (!string.IsNullOrEmpty(defaultSortKey))
            keyBuilder.Append($":defaultSort={CompactCacheSegment(defaultSortKey!)}");
    }

    /// <summary>
    /// Appends requested key sets. Each set is rendered through <see cref="WhereClauseHelpers.FormatValueCanonical" />, which length-prefixes every component: joining raw
    /// values on <c>|</c> made <c>["a|b", "c"]</c> and <c>["a", "b|c"]</c> produce one key, so a request for one row could be answered from another's entry.
    /// </summary>
    private static void AppendKeySets(StringBuilder keyBuilder, IReadOnlyList<object[]>? keys)
    {
        if (keys is not { Count: > 0 })
            return;

        keyBuilder.Append($":keys={CompactCacheSegment(string.Join(";", keys.Select(WhereClauseHelpers.FormatValueCanonical)))}");
    }

    /// <summary>
    /// Renders a CLR type for a cache key. Uses <see cref="Type.FullName" />: two entities or two response DTOs that share a short name in different namespaces would
    /// otherwise produce identical keys and serve each other's rows.
    /// </summary>
    private static string TypeKeySegment(Type type) => (type.FullName ?? type.Name).ToLowerInvariant();

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