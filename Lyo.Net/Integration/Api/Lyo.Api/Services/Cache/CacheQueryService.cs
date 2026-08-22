using System.Linq.Expressions;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using ApiErrorCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Lyo.Api.Services.Crud.Read;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Cache;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.WhereClause;
using CacheSnapshotItem = Lyo.Cache.CacheItem;

namespace Lyo.Api.Services.Cache;

/// <summary>
/// In-memory QueryProject over this process's L1 <see cref="ICacheService.Items" />. Redis L2 keys written by other processes are not listed.
/// Never writes the admin query result back into the cache.
/// </summary>
public sealed class CacheQueryService(ICacheService cache, IWhereClauseService filterService, IProjectionService projectionService, QueryOptions queryOptions)
{
    private static readonly Expression<Func<CacheSnapshotItem, object?>> DefaultOrder = static i => i.Created;

    private static readonly string[] DefaultSelectFields = ["Type", "Name", "Tags", "Encrypted", "Compressed", "SizeBytes", "Created", "Expires"];

    /// <summary>Known tag-index prefixes written into <see cref="ICacheService.Items" /> (<c>Lyo.Cache</c> local + Fusion).</summary>
    internal static readonly string[] TagNamePrefixes = ["__fc:t:", "__tag:"];

    public ProjectedQueryRes<object?> QueryProjected(ProjectionQueryReq queryRequest)
    {
        ArgumentNullException.ThrowIfNull(queryRequest);
        var pagingErrors = QueryPagingBoundsValidator.Validate(queryRequest, queryOptions, queryOptions.MaxPageSize);
        if (pagingErrors.Count > 0)
            return ResultFactory.ProjectedQueryFailure<object?>(queryRequest, AggregatedProblem(pagingErrors, "Invalid query."));

        IReadOnlyList<string> selectFields = queryRequest.Select.Count > 0 ? queryRequest.Select : DefaultSelectFields;
        var (specs, pathErrors) = projectionService.ResolveProjectedFields<CacheSnapshotItem>(selectFields, queryOptions.AllowSelectWildcards);
        if (pathErrors.Count > 0)
            return ResultFactory.ProjectedQueryFailure<object?>(queryRequest, AggregatedProblem(pathErrors, "Invalid query."));

        if (queryRequest.ComputedFields.Count > 0) {
            var computedErrors = projectionService.ValidateComputedFieldTemplates(queryRequest.ComputedFields);
            if (computedErrors.Count > 0)
                return ResultFactory.ProjectedQueryFailure<object?>(queryRequest, AggregatedProblem(computedErrors, "Invalid query."));
        }

        try {
            IQueryable<CacheSnapshotItem> source = FilterByKeys(cache.Items, queryRequest.Keys).AsQueryable();
            source = filterService.ApplyWhereClause(source, queryRequest.WhereClause);
            source = filterService.ApplyOrdering(source, queryRequest.SortBy, DefaultOrder, SortDirection.Desc);
            var filtered = source.ToList();
            var start = queryRequest.Start ?? 0;
            var amount = queryRequest.Amount ?? queryOptions.DefaultPageSize;
            var page = filtered.Skip(start).Take(amount).ToList();
            var projected = projectionService.ProjectEntities(
                page, specs, QueryIncludeFilterMode.Full, new([], GroupOperatorEnum.And));
            if (queryRequest.ComputedFields.Count > 0)
                projected = projectionService.ApplyComputedFields(projected, queryRequest.ComputedFields, specs);

            var totalMode = queryRequest.Options.TotalCountMode;
            int? total = totalMode == QueryTotalCountMode.Exact ? filtered.Count : null;
            bool? hasMore = totalMode == QueryTotalCountMode.None ? null : start + page.Count < filtered.Count;
            return ResultFactory.ProjectedQuerySuccess(queryRequest, projected, start, page.Count, total, hasMore, entityTypes: ["CacheItem"]);
        }
        catch (InvalidQueryException ex) {
            return ResultFactory.ProjectedQueryFailure<object?>(
                queryRequest, LyoProblemDetails.FromCode(ApiErrorCodes.InvalidQuery, ex.Message, DateTime.UtcNow));
        }
    }

    public async Task<CacheMutationRes> ClearAsync()
    {
        var count = cache.Items.Count;
        await cache.ClearAsync().ConfigureAwait(false);
        return new(count);
    }

    /// <summary>Same contract as <c>IDeleteService.DeleteAsync</c>: keys or where-clause. <see cref="CacheItemTypeEnum.Key" /> and <see cref="CacheItemTypeEnum.Tag" /> share this path.</summary>
    public async Task<DeleteResult<object?>> DeleteAsync(DeleteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var items = ResolveDeleteItems(request);
        if (items.Count == 0)
            return ResultFactory.DeleteFailure<object?>(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Cache item not found.", DateTime.UtcNow));

        if (items.Count > 1 && !request.AllowMultiple) {
            return ResultFactory.DeleteFailure<object?>(
                LyoProblemDetails.FromCode(ApiErrorCodes.InvalidOperation, $"Multiple cache items ({items.Count}) found but AllowMultiple is false.", DateTime.UtcNow));
        }

        CacheSnapshotItem? last = null;
        foreach (var item in items) {
            await InvalidateItemAsync(item).ConfigureAwait(false);
            last = item;
        }

        return ResultFactory.DeleteSuccess<object?>(ToRow(last!));
    }

    /// <summary>Same contract as <c>IDeleteService.DeleteBulkAsync</c>. Each matched key or tag becomes one <see cref="DeleteResult{T}" />.</summary>
    public async Task<DeleteBulkResult<object?>> DeleteBulkAsync(IReadOnlyList<DeleteRequest>? requests)
    {
        var results = new List<DeleteResult<object?>>();
        if (requests is null || requests.Count == 0)
            return ResultFactory.DeleteBulk(results);

        foreach (var request in requests) {
            var items = ResolveDeleteItems(request);
            if (items.Count == 0) {
                results.Add(ResultFactory.DeleteFailure<object?>(LyoProblemDetails.FromCode(ApiErrorCodes.NotFound, "Cache item not found.", DateTime.UtcNow)));
                continue;
            }

            if (items.Count > 1 && !request.AllowMultiple) {
                results.Add(
                    ResultFactory.DeleteFailure<object?>(
                        LyoProblemDetails.FromCode(
                            ApiErrorCodes.InvalidOperation, $"Multiple cache items ({items.Count}) found but AllowMultiple is false.", DateTime.UtcNow)));
                continue;
            }

            foreach (var item in items) {
                await InvalidateItemAsync(item).ConfigureAwait(false);
                results.Add(ResultFactory.DeleteSuccess<object?>(ToRow(item)));
            }
        }

        return ResultFactory.DeleteBulk(results);
    }

    /// <summary>Strips Fusion/local tag-index prefixes so grid-selected <c>Name</c> values work with <see cref="ICacheService.InvalidateCacheItemByTag" />.</summary>
    public static string NormalizeTagName(string name)
    {
        foreach (var prefix in TagNamePrefixes) {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return name[prefix.Length..];
        }

        return name;
    }

    private static bool TagNamesEqual(string storedName, string logicalTag)
    {
        var stored = NormalizeTagName(storedName);
        return string.Equals(stored, logicalTag, StringComparison.OrdinalIgnoreCase)
            || string.Equals(storedName, logicalTag, StringComparison.OrdinalIgnoreCase);
    }

    private List<CacheSnapshotItem> ResolveDeleteItems(DeleteRequest request)
    {
        var hasKeys = request.Keys is { Count: > 0 };
        var hasQuery = request.Query is not null;
        if (!hasKeys && !hasQuery)
            return [];

        IEnumerable<CacheSnapshotItem> source = hasKeys ? FilterByKeys(cache.Items, request.Keys!) : cache.Items;
        if (hasQuery)
            source = filterService.ApplyWhereClause(source.AsQueryable(), request.Query);

        return source.ToList();
    }

    private Task InvalidateItemAsync(CacheSnapshotItem item)
        => item.Type == CacheItemTypeEnum.Tag
            ? cache.InvalidateCacheItemByTag(NormalizeTagName(item.Name))
            : cache.InvalidateCacheItem(item.Name);

    private static Dictionary<string, object?> ToRow(CacheSnapshotItem item)
        => new(StringComparer.OrdinalIgnoreCase) {
            ["Type"] = item.Type.ToString(),
            ["Name"] = item.Name,
            ["Created"] = item.Created,
            ["Expires"] = item.Expires,
            ["Tags"] = item.Tags,
            ["Encrypted"] = item.Encrypted,
            ["Compressed"] = item.Compressed,
            ["SizeBytes"] = item.SizeBytes
        };

    private static List<CacheSnapshotItem> FilterByKeys(IReadOnlyCollection<CacheSnapshotItem> items, IReadOnlyList<object[]> keys)
    {
        if (keys.Count == 0)
            return items.ToList();

        var matches = new List<(CacheItemTypeEnum Type, string Name)>();
        foreach (var key in keys) {
            if (key.Length < 2 || !TryParseItemType(key[0], out var type))
                continue;

            var name = key[1]?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
                matches.Add((type, name));
        }

        return items.Where(i => matches.Any(m => ItemMatches(i, m.Type, m.Name))).ToList();
    }

    private static bool ItemMatches(CacheSnapshotItem item, CacheItemTypeEnum type, string name)
    {
        if (item.Type != type)
            return false;

        return type == CacheItemTypeEnum.Tag
            ? TagNamesEqual(item.Name, name)
            : string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseItemType(object? value, out CacheItemTypeEnum type)
    {
        if (value is CacheItemTypeEnum parsed) {
            type = parsed;
            return true;
        }

        var text = value?.ToString();
        if (string.Equals(text, nameof(CacheItemTypeEnum.Key), StringComparison.OrdinalIgnoreCase) || text == "0") {
            type = CacheItemTypeEnum.Key;
            return true;
        }

        if (string.Equals(text, nameof(CacheItemTypeEnum.Tag), StringComparison.OrdinalIgnoreCase) || text == "1") {
            type = CacheItemTypeEnum.Tag;
            return true;
        }

        type = default;
        return false;
    }

    private static LyoProblemDetails AggregatedProblem(IReadOnlyList<ApiError> errors, string message)
        => new(message, LyoProblemDetails.MapErrorCodeToHttpStatus(ApiErrorCodes.InvalidQuery), DateTime.UtcNow, errors.ToList());
}
