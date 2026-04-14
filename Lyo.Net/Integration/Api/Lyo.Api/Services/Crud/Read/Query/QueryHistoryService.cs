using System.Linq.Expressions;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Common.Enums;
using Lyo.Metrics;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Read.Query;

public class QueryHistoryService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    QueryOptions queryOptions,
    ILyoMapper mapper,
    IWhereClauseService filterService,
    ILogger<QueryHistoryService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), IQueryHistoryService<TContext>
    where TContext : DbContext
{
    public async Task<QueryHistoryResults<HistoryResult<TResult>>> QueryHistory<TDbModel, TResult>(
        HistoryQuery query,
        Expression<Func<TDbModel, object?>> defaultOrder,
        Func<TDbModel, DateTime> startTimeSelector,
        Func<TDbModel, DateTime> endTimeSelector,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "query_history";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        using var scope = BeginActionScope("QUERY TEMPORAL", typeof(HistoryQuery), typeof(TDbModel), typeof(TResult));
        var pagingErrors = QueryPagingBoundsValidator.Validate(query, queryOptions);
        if (pagingErrors.Count > 0) {
            Logger.LogWarning(
                "History query paging validation failed for {EntityType}: {IssueCount} issue(s). {Details}",
                typeof(TDbModel).Name,
                pagingErrors.Count,
                string.Join("; ", pagingErrors.Select(static e => $"{e.Code}: {e.Description}")));

            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.QueryHistoryFailure<TResult>(
                query,
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery)
                    .WithMessage("Invalid query.")
                    .AddErrors(pagingErrors)
                    .Build());
        }

        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var dbSet = context.Set<TDbModel>();
            var queryable = dbSet.AsQueryable();

            //if (!query.FromDateTime.HasValue && !query.ToDateTime.HasValue)
            //    queryable = dbSet.TemporalAll();
            //
            //else if (query.FromDateTime.HasValue && !query.ToDateTime.HasValue)
            //    queryable = dbSet.TemporalAsOf(query.FromDateTime.Value);
            //
            //else if (!query.FromDateTime.HasValue && query.ToDateTime.HasValue)
            //    queryable = dbSet.TemporalAsOf(query.ToDateTime.Value);
            //
            //else 
            //    queryable = dbSet.TemporalBetween(query.FromDateTime!.Value, query.ToDateTime!.Value);
            queryable = filterService.ApplyWhereClause(queryable, query.WhereClause);
            queryable = filterService.ApplyOrdering(queryable, query.SortBy, defaultOrder, defaultSortDirection);
            var total = await queryable.CountAsync(ct);
            var pageSize = query.Amount ??= queryOptions.DefaultPageSize;
            var resultsQuery = queryable.Skip(query.Start ?? 0).Take(pageSize);
            if (queryOptions.UseNoTrackingWithIdentityResolution)
                resultsQuery = resultsQuery.AsNoTrackingWithIdentityResolution();

            //todo split queries with navigations can return partial. personal proj i have fixed, apply fix here
            if (queryOptions.EnableSplitQueries)
                resultsQuery = resultsQuery.AsSplitQuery();

            var results = await resultsQuery.ToListAsync(ct);
            var mappedResults = MapResults<TDbModel, TResult>(results, startTimeSelector, endTimeSelector);
            RecordCrudSuccess(operation, typeof(TDbModel));
            RecordCrudResultCount(operation, typeof(TDbModel), mappedResults.Count);
            return ResultFactory.QueryHistorySuccess(query, mappedResults, total, query.Start, mappedResults.Count);
        }
        catch (Exception ex) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.QueryHistoryFailure<TResult>(query, LogAndReturnApiError(ex, "Query Error", Lyo.Api.Models.Constants.ApiErrorCodes.InvalidQuery));
        }
    }

    private List<HistoryResult<TResult>> MapResults<TDbModel, TResult>(
        List<TDbModel> dbResults,
        Func<TDbModel, DateTime> startTimeSelector,
        Func<TDbModel, DateTime> endTimeSelector)
    {
        var results = new List<HistoryResult<TResult>>(dbResults.Count);
        foreach (var dbResult in dbResults) {
            try {
                var result = MapOrCast<TDbModel, TResult>(Mapper, dbResult!);
                results.Add(ResultFactory.HistorySuccess(result, startTimeSelector.Invoke(dbResult), endTimeSelector.Invoke(dbResult)));
            }
            catch (Exception ex) {
                results.Add(ResultFactory.HistoryError<TResult>(LogAndReturnApiError(ex, "Couldn't map entity history")));
            }
        }

        return results;
    }
}