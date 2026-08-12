using System.Linq.Expressions;
using System.Reflection;
using Lyo.Api.Services.TypeConversion;
using Lyo.Common.Enums;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Read;

/// <summary>Result of building a query path (key-constrained, subquery, or standard).</summary>
public sealed record QueryExecutionState<TDbModel>(IQueryable<TDbModel> BaseQueryable, int? Total, bool IsInMemoryResults, bool OrderingAppliedInDb)
    where TDbModel : class;

/// <summary>Builds the base queryable for a query (key-constrained, subquery, or standard filter path).</summary>
public interface IQueryPathExecutor
{
    Task<QueryExecutionState<TDbModel>> ExecuteKeyConstrainedPathAsync<TContext, TDbModel>(
        TContext context,
        QueryConcreteReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;

    Task<QueryExecutionState<TDbModel>> ExecuteNonKeyPathAsync<TContext, TDbModel>(
        TContext context,
        QueryConcreteReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class;
}

/// <summary>Builds the base queryable for a query. Shared by QueryService.</summary>
public sealed class QueryPathExecutor(
    IWhereClauseService filterService,
    IQueryPagingHelper pagingHelper,
    ITypeConversionService typeConversion,
    ILogger<QueryPathExecutor> logger) : IQueryPathExecutor
{
    public async Task<QueryExecutionState<TDbModel>> ExecuteKeyConstrainedPathAsync<TContext, TDbModel>(
        TContext context,
        QueryConcreteReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class
    {
        logger.LogDebug("FindAsync requested for {KeyCount} key sets", queryRequest.Keys.Count);
        var entityType = context.Model.FindEntityType(typeof(TDbModel));
        var primaryKey = entityType?.FindPrimaryKey();
        var pkCount = primaryKey?.Properties.Count ?? 0;
        if (pkCount == 0) {
            var msg = $"Entity type '{typeof(TDbModel).Name}' does not have a primary key defined.";
            logger.LogWarning(msg);
            throw new ArgumentException(msg);
        }

        var validKeySets = new List<object[]>(queryRequest.Keys.Count);
        var emptyCount = 0;
        var invalidCount = 0;
        foreach (var k in queryRequest.Keys) {
            if (k.Length == pkCount)
                validKeySets.Add(k);
            else if (k.Length == 0)
                emptyCount++;
            else
                invalidCount++;
        }

        if (emptyCount > 0)
            logger.LogWarning("{EmptyCount} empty key set(s) were skipped when loading {Entity}", emptyCount, typeof(TDbModel).Name);

        if (invalidCount > 0)
            logger.LogWarning("{InvalidCount} key set(s) were ignored because they did not match primary key length {PkCount}", invalidCount, pkCount);

        if (validKeySets.Count == 0) {
            logger.LogDebug("No valid key sets found; returning empty result set for key-constrained query");
            return new(Array.Empty<TDbModel>().AsQueryable(), 0, true, false);
        }

        var convertedKeySets = new List<object[]>(validKeySets.Count);
        foreach (var keySet in validKeySets)
            convertedKeySets.Add(typeConversion.ConvertKeysForFind<TDbModel>(keySet, context));

        List<TDbModel> entities;
        var singlePkOptimized = pkCount == 1
            ? await TryLoadBySinglePrimaryKeyAsync<TDbModel>(context, convertedKeySets, primaryKey!.Properties[0].Name, primaryKey.Properties[0].ClrType, ct).ConfigureAwait(false)
            : null;

        if (singlePkOptimized is not null)
            entities = singlePkOptimized;
        else {
            entities = new(convertedKeySets.Count);
            foreach (var convertedKeys in convertedKeySets) {
                var entity = await context.Set<TDbModel>().FindAsync(convertedKeys, ct).ConfigureAwait(false);
                if (entity != null)
                    entities.Add(entity);
            }
        }

        if (entities.Count == 0) {
            logger.LogDebug("No entities found by keys; returning empty result set for key-constrained query.");
            return new(entities.AsQueryable(), 0, true, false);
        }

        if (queryRequest.Include.Any())
            entities = await pagingHelper.BatchHydrateIncludesAsync(context, entities, queryRequest.Include, ct).ConfigureAwait(false);

        if (queryRequest.WhereClause != null) {
            logger.LogDebug("Applying WhereClause to {EntityCount} entities loaded by keys", entities.Count);
            entities = entities.Where(entity => filterService.MatchesWhereClause(entity, queryRequest.WhereClause)).ToList();
        }

        return new(entities.AsQueryable(), entities.Count, true, false);
    }

    public async Task<QueryExecutionState<TDbModel>> ExecuteNonKeyPathAsync<TContext, TDbModel>(
        TContext context,
        QueryConcreteReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        CancellationToken ct = default)
        where TContext : DbContext where TDbModel : class
    {
        var totalCountMode = queryRequest.Options.TotalCountMode;
        var computeExactTotal = totalCountMode == QueryTotalCountMode.Exact;
        var useTwoPhase = WhereClauseHelpers.HasAnySubClause(queryRequest.WhereClause);
        if (useTwoPhase && queryRequest.WhereClause != null)
            return await ExecuteSubQueryPathAsync(context, queryRequest, defaultOrder, defaultSortDirection, computeExactTotal, ct).ConfigureAwait(false);

        return await ExecuteStandardFilterPathAsync<TContext, TDbModel>(context, queryRequest, computeExactTotal, ct).ConfigureAwait(false);
    }

    private async Task<QueryExecutionState<TDbModel>> ExecuteSubQueryPathAsync<TContext, TDbModel>(
        TContext context,
        QueryConcreteReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        bool computeExactTotal,
        CancellationToken ct)
        where TContext : DbContext where TDbModel : class
    {
        try {
            var sqlFirstQueryable = context.Set<TDbModel>().AsQueryable();
            sqlFirstQueryable = filterService.ApplyWhereClause(sqlFirstQueryable, queryRequest.WhereClause);
            logger.LogDebug("Using SQL-first subquery execution for {EntityType}", typeof(TDbModel).Name);
            int? sqlTotal = null;
            if (computeExactTotal)
                sqlTotal = await QueryRootCountHelper.CountDistinctRootEntitiesAsync(context, sqlFirstQueryable, ct).ConfigureAwait(false);

            return new(sqlFirstQueryable, sqlTotal, false, false);
        }
        catch (Exception ex) when (IsLikelyTranslationFailure(ex)) {
            logger.LogDebug(ex, "Subquery expression could not be translated for {EntityType}; using fallback path", typeof(TDbModel).Name);
        }

        logger.LogDebug("Falling back to two-phase subquery execution for {EntityType}", typeof(TDbModel).Name);
        var baseQueryable = context.Set<TDbModel>().AsQueryable();
        var filteredQueryable = filterService.ApplyWhereClause(baseQueryable, queryRequest.WhereClause, false);
        filteredQueryable = filterService.ApplyOrdering(filteredQueryable, queryRequest.SortBy, defaultOrder, defaultSortDirection);
        var subQueryIncludes = filterService.GetCollectionIncludePathsForWhereClause<TDbModel>(queryRequest.WhereClause).ToList();
        var includesForPhase2 = subQueryIncludes.Count > 0
            ? queryRequest.Include.Concat(subQueryIncludes).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : queryRequest.Include.ToList();

        var phase1Results = await filteredQueryable.ToListAsync(ct).ConfigureAwait(false);
        if (includesForPhase2.Count > 0) {
            logger.LogDebug("Loading {IncludeCount} navigation(s) for SubQuery in-memory filter: {Includes}", includesForPhase2.Count, string.Join(", ", includesForPhase2));
            phase1Results = await pagingHelper.BatchHydrateIncludesAsync(context, phase1Results, includesForPhase2, ct).ConfigureAwait(false);
        }

        var phase2Results = phase1Results.Where(e => filterService.MatchesWhereClause(e, queryRequest.WhereClause!)).ToList();
        int? phaseTotal = computeExactTotal ? phase2Results.Count : null;
        return new(phase2Results.AsQueryable(), phaseTotal, true, true);
    }

    private async Task<QueryExecutionState<TDbModel>> ExecuteStandardFilterPathAsync<TContext, TDbModel>(
        TContext context,
        QueryConcreteReq queryRequest,
        bool computeExactTotal,
        CancellationToken ct)
        where TContext : DbContext where TDbModel : class
    {
        var baseQueryable = context.Set<TDbModel>().AsQueryable();
        var filteredQueryable = filterService.ApplyWhereClause(baseQueryable, queryRequest.WhereClause);
        int? total = null;
        if (computeExactTotal)
            total = await QueryRootCountHelper.CountDistinctRootEntitiesAsync(context, filteredQueryable, ct).ConfigureAwait(false);

        return new(filteredQueryable, total, false, false);
    }

    private static bool IsLikelyTranslationFailure(Exception ex)
        => ex is InvalidOperationException or NotSupportedException && ex.Message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase);

    private static async Task<List<TDbModel>?> TryLoadBySinglePrimaryKeyAsync<TDbModel>(
        DbContext context,
        IReadOnlyList<object[]> validKeySets,
        string keyName,
        Type keyClrType,
        CancellationToken ct)
        where TDbModel : class
    {
        var method = typeof(QueryPathExecutor).GetMethod(nameof(LoadBySinglePrimaryKeyAsync), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(typeof(TDbModel), keyClrType);

        if (method == null)
            return null;

        var task = (Task<List<TDbModel>>)method.Invoke(null, [context, validKeySets, keyName, ct])!;
        return await task.ConfigureAwait(false);
    }

    private static async Task<List<TDbModel>> LoadBySinglePrimaryKeyAsync<TDbModel, TKey>(
        DbContext context,
        IReadOnlyList<object[]> validKeySets,
        string keyName,
        CancellationToken ct)
        where TDbModel : class where TKey : notnull
    {
        var keys = validKeySets.Where(static ks => ks.Length > 0 && ks[0] is not null).Select(static ks => (TKey)ks[0]).Distinct().ToArray();
        if (keys.Length == 0)
            return [];

        var predicate = QueryKeyExpressionBuilder.BuildEfKeyInPredicate<TDbModel, TKey>(keyName, keys);
        return await context.Set<TDbModel>().Where(predicate).ToListAsync(ct).ConfigureAwait(false);
    }
}