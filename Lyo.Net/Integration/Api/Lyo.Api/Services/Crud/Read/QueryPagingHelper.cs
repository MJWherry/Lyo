using System.Linq.Expressions;
using System.Reflection;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Read;

/// <summary>Result of ID-first include paging attempt.</summary>
public sealed record IdFirstPagingResult<TDbModel>(TDbModel[] Results, bool HasMore)
    where TDbModel : class;

/// <summary>Handles paging, ID-first include paging, and batch hydration. Shared by QueryService.</summary>
public interface IQueryPagingHelper
{
    Task<(bool Applied, TDbModel[] Results, bool HasMore)> TryIdFirstIncludePaging<TContext, TDbModel>(
        TContext context,
        IQueryable<TDbModel> orderedQueryable,
        IEnumerable<string> includes,
        int startIndex,
        int pageSize,
        int takeSize,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;

    Task<List<TDbModel>> BatchHydrateIncludesAsync<TContext, TDbModel>(
        TContext context,
        List<TDbModel> entities,
        IReadOnlyCollection<string> includes,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;

    Task<(TDbModel[] QueryResults, int? Total, bool? HasMore)> ApplyPagingAndMaterializeAsync<TContext, TDbModel>(
        TContext context,
        QueryExecutionState<TDbModel> state,
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        bool keysProvided,
        IWhereClauseService filterService,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;
}

/// <summary>Handles paging, ID-first include paging, and batch hydration.</summary>
public sealed class QueryPagingHelper(IEntityLoaderService loaderService, QueryOptions queryOptions, ILogger<QueryPagingHelper> logger) : IQueryPagingHelper
{
    public async Task<(bool Applied, TDbModel[] Results, bool HasMore)> TryIdFirstIncludePaging<TContext, TDbModel>(
        TContext context,
        IQueryable<TDbModel> orderedQueryable,
        IEnumerable<string> includes,
        int startIndex,
        int pageSize,
        int takeSize,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class
    {
        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        var pk = entityType?.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1)
            return (false, [], false);

        var keyProperty = pk.Properties[0];
        var keyName = keyProperty.Name;
        var keyClrType = keyProperty.ClrType;
        var method = typeof(QueryPagingHelper).GetMethod(nameof(TryIdFirstIncludePagingInternal), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(TDbModel), keyClrType);

        if (method == null)
            return (false, [], false);

        var task = (Task<IdFirstPagingResult<TDbModel>>)method.Invoke(
            null, [context, loaderService, queryOptions, orderedQueryable, includes, startIndex, pageSize, takeSize, keyName, ct])!;

        var result = await task.ConfigureAwait(false);
        return (true, result.Results, result.HasMore);
    }

    public async Task<List<TDbModel>> BatchHydrateIncludesAsync<TContext, TDbModel>(
        TContext context,
        List<TDbModel> entities,
        IReadOnlyCollection<string> includes,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class
    {
        if (entities.Count == 0 || includes.Count == 0)
            return entities;

        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        var pk = entityType?.FindPrimaryKey();
        if (pk == null || pk.Properties.Count != 1) {
            foreach (var entity in entities)
                await loaderService.LoadIncludes(context, entity, includes, ct).ConfigureAwait(false);

            return entities;
        }

        var keyProperty = pk.Properties[0];
        var keyName = keyProperty.Name;
        var keyClrType = keyProperty.ClrType;
        var method = typeof(QueryPagingHelper).GetMethod(nameof(BatchHydrateIncludesInternal), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(TDbModel), keyClrType);

        if (method == null)
            return entities;

        var task = (Task<List<TDbModel>>)method.Invoke(null, [context, loaderService, queryOptions, entities, includes, keyName, ct])!;
        return await task.ConfigureAwait(false);
    }

    public async Task<(TDbModel[] QueryResults, int? Total, bool? HasMore)> ApplyPagingAndMaterializeAsync<TContext, TDbModel>(
        TContext context,
        QueryExecutionState<TDbModel> state,
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        bool keysProvided,
        IWhereClauseService filterService,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class
    {
        var totalCountMode = queryRequest.Options.TotalCountMode;
        var resultsQueryable = state.OrderingAppliedInDb
            ? state.BaseQueryable
            : filterService.ApplyOrdering(state.BaseQueryable, queryRequest.SortBy, defaultOrder, defaultSortDirection);

        var pageSize = queryRequest.Amount ??= queryOptions.DefaultPageSize;
        var startIndex = queryRequest.Start ?? 0;
        var takeSize = totalCountMode == QueryTotalCountMode.HasMore ? pageSize + 1 : pageSize;
        var useIdFirstIncludePaging = !state.IsInMemoryResults && !keysProvided && queryRequest.Include.Count > 0 && !queryOptions.EnableSplitQueries;
        TDbModel[] rawResults;
        bool? hasMore = null;
        var usedIdFirstIncludePaging = false;
        if (useIdFirstIncludePaging) {
            var idFirst = await TryIdFirstIncludePaging(context, resultsQueryable, queryRequest.Include, startIndex, pageSize, takeSize, ct).ConfigureAwait(false);
            if (idFirst.Applied) {
                rawResults = idFirst.Results;
                hasMore = totalCountMode == QueryTotalCountMode.HasMore ? idFirst.HasMore : null;
                usedIdFirstIncludePaging = true;
                logger.LogDebug("ID-first include paging applied for {EntityType}", typeof(TDbModel).Name);
            }
            else {
                var fallbackQuery = resultsQueryable.Skip(startIndex).Take(takeSize);
                fallbackQuery = loaderService.LoadNestedCollections(context, fallbackQuery, queryRequest.Include);
                fallbackQuery = queryOptions.UseNoTrackingWithIdentityResolution ? fallbackQuery.AsNoTrackingWithIdentityResolution() : fallbackQuery.AsNoTracking();
                if (queryOptions.EnableSplitQueries)
                    fallbackQuery = fallbackQuery.AsSplitQuery();

                rawResults = await fallbackQuery.ToArrayAsync(ct).ConfigureAwait(false);
            }
        }
        else {
            var resultsQuery = resultsQueryable.Skip(startIndex).Take(takeSize);
            if (!state.IsInMemoryResults && queryRequest.Include.Count > 0)
                resultsQuery = loaderService.LoadNestedCollections(context, resultsQuery, queryRequest.Include);

            if (queryRequest.Keys.Count == 0)
                resultsQuery = queryOptions.UseNoTrackingWithIdentityResolution ? resultsQuery.AsNoTrackingWithIdentityResolution() : resultsQuery.AsNoTracking();

            if (queryOptions.EnableSplitQueries && queryRequest.Keys.Count == 0)
                resultsQuery = resultsQuery.AsSplitQuery();

            rawResults = state.IsInMemoryResults ? resultsQuery.ToArray() : await resultsQuery.ToArrayAsync(ct).ConfigureAwait(false);
        }

        if (!usedIdFirstIncludePaging)
            hasMore = totalCountMode == QueryTotalCountMode.HasMore ? rawResults.Length > pageSize : null;

        var queryResults = usedIdFirstIncludePaging ? rawResults : hasMore == true ? rawResults.Take(pageSize).ToArray() : rawResults;
        var total = state.Total;
        if (totalCountMode == QueryTotalCountMode.None || (totalCountMode == QueryTotalCountMode.HasMore && hasMore == true))
            total = null;
        else if (totalCountMode == QueryTotalCountMode.HasMore && hasMore == false)
            total = startIndex + queryResults.Length;

        return (queryResults, total, hasMore);
    }

    private static async Task<IdFirstPagingResult<TDbModel>> TryIdFirstIncludePagingInternal<TDbModel, TKey>(
        DbContext context,
        IEntityLoaderService loaderService,
        QueryOptions queryOptions,
        IQueryable<TDbModel> orderedQueryable,
        IEnumerable<string> includes,
        int startIndex,
        int pageSize,
        int takeSize,
        string keyName,
        CancellationToken ct)
        where TDbModel : class where TKey : notnull
    {
        var keySelector = QueryKeyExpressionBuilder.BuildEfKeySelector<TDbModel, TKey>(keyName);
        var keyAccessor = QueryKeyExpressionBuilder.BuildClrKeyAccessor<TDbModel, TKey>(keyName);
        var pagedKeysWithExtra = await orderedQueryable.Skip(startIndex).Take(takeSize).Select(keySelector).ToArrayAsync(ct).ConfigureAwait(false);
        var hasMore = pagedKeysWithExtra.Length > pageSize;
        var pagedKeys = hasMore ? pagedKeysWithExtra.Take(pageSize).ToArray() : pagedKeysWithExtra;
        if (pagedKeys.Length == 0)
            return new([], hasMore);

        var fetchKeys = pagedKeys.Distinct().ToArray();
        var keyFilter = QueryKeyExpressionBuilder.BuildEfKeyInPredicate<TDbModel, TKey>(keyName, fetchKeys);
        var hydrateQuery = context.Set<TDbModel>().Where(keyFilter);
        hydrateQuery = loaderService.LoadNestedCollections(context, hydrateQuery, includes);
        hydrateQuery = queryOptions.UseNoTrackingWithIdentityResolution ? hydrateQuery.AsNoTrackingWithIdentityResolution() : hydrateQuery.AsNoTracking();
        if (queryOptions.EnableSplitQueries)
            hydrateQuery = hydrateQuery.AsSplitQuery();

        var hydrated = await hydrateQuery.ToArrayAsync(ct).ConfigureAwait(false);
        var byKey = hydrated.ToDictionary(keyAccessor, i => i);
        var orderedResults = new List<TDbModel>(pagedKeys.Length);
        foreach (var key in pagedKeys) {
            if (byKey.TryGetValue(key, out var entity))
                orderedResults.Add(entity);
        }

        return new(orderedResults.ToArray(), hasMore);
    }

    private static async Task<List<TDbModel>> BatchHydrateIncludesInternal<TDbModel, TKey>(
        DbContext context,
        IEntityLoaderService loaderService,
        QueryOptions queryOptions,
        List<TDbModel> phase1Results,
        IReadOnlyCollection<string> includes,
        string keyName,
        CancellationToken ct)
        where TDbModel : class where TKey : notnull
    {
        var keyAccessor = QueryKeyExpressionBuilder.BuildClrKeyAccessor<TDbModel, TKey>(keyName);
        var phase1Keys = phase1Results.Select(keyAccessor).Distinct().ToArray();
        if (phase1Keys.Length == 0)
            return phase1Results;

        var keyFilter = QueryKeyExpressionBuilder.BuildEfKeyInPredicate<TDbModel, TKey>(keyName, phase1Keys);
        var hydrateQuery = context.Set<TDbModel>().Where(keyFilter);
        hydrateQuery = loaderService.LoadNestedCollections(context, hydrateQuery, includes);
        hydrateQuery = queryOptions.UseNoTrackingWithIdentityResolution ? hydrateQuery.AsNoTrackingWithIdentityResolution() : hydrateQuery.AsNoTracking();
        if (queryOptions.EnableSplitQueries)
            hydrateQuery = hydrateQuery.AsSplitQuery();

        var hydrated = await hydrateQuery.ToArrayAsync(ct).ConfigureAwait(false);
        var byKey = hydrated.ToDictionary(keyAccessor, i => i);
        var ordered = new List<TDbModel>(phase1Results.Count);
        foreach (var entity in phase1Results) {
            var key = keyAccessor(entity);
            ordered.Add(byKey.GetValueOrDefault(key, entity));
        }

        return ordered;
    }
}