using System.Linq.Expressions;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read.Project;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Api.Services.TypeConversion;
using Lyo.Cache;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Read.Query;

public class QueryService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    IWhereClauseService filterService,
    IEntityLoaderService loaderService,
    IProjectionService projectionService,
    IQueryPathExecutor pathExecutor,
    IQueryPagingHelper pagingHelper,
    ITypeConversionService typeConversion,
    ICacheService cache,
    QueryOptions queryOptions,
    IServiceProvider serviceProvider,
    ILogger<QueryService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), IQueryService<TContext>
    where TContext : DbContext
{
    public async Task<QueryRes<TResult>> Query<TDbModel, TResult>(
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "query_map";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        try {
            var raw = await QueryCore(queryRequest, defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
            if (!raw.IsSuccess) {
                RecordCrudFailure(operation, typeof(TDbModel));
                return ResultFactory.QueryFailure<TResult>(queryRequest, raw.Error!);
            }

            // MapResults only needs DbContext when mapping throws (PK for logs); avoid a second context on the hot path.
            var mapped = await MapResultsAsync<TDbModel, TResult>(raw.Items!, ct).ConfigureAwait(false);
            RecordCrudSuccess(operation, typeof(TDbModel));
            RecordCrudResultCount(operation, typeof(TDbModel), mapped.Length);
            return ResultFactory.QuerySuccess(queryRequest, mapped, queryRequest.Start, mapped.Length, raw.Total, raw.HasMore);
        }
        catch (OperationCanceledException) {
            RecordCrudCancelled(operation, typeof(TDbModel));
            throw;
        }
        catch {
            RecordCrudFailure(operation, typeof(TDbModel));
            throw;
        }
    }

    public async Task<QueryRes<TDbModel>> Query<TDbModel>(
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "query";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        try {
            var result = await QueryCore(queryRequest, defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
            if (result.IsSuccess) {
                RecordCrudSuccess(operation, typeof(TDbModel));
                RecordCrudResultCount(operation, typeof(TDbModel), result.Items?.Count ?? 0);
            }
            else
                RecordCrudFailure(operation, typeof(TDbModel));

            return result;
        }
        catch (OperationCanceledException) {
            RecordCrudCancelled(operation, typeof(TDbModel));
            throw;
        }
        catch {
            RecordCrudFailure(operation, typeof(TDbModel));
            throw;
        }
    }

    public async Task<ProjectedQueryRes<object?>> QueryProjected<TDbModel>(
        ProjectionQueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection = SortDirection.Desc,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "query_projected";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        try {
            var result = await QueryProjectedCore(queryRequest, defaultOrder, defaultSortDirection, ct).ConfigureAwait(false);
            if (result.IsSuccess) {
                RecordCrudSuccess(operation, typeof(TDbModel));
                RecordCrudResultCount(operation, typeof(TDbModel), result.Items?.Count ?? 0);
            }
            else
                RecordCrudFailure(operation, typeof(TDbModel));

            return result;
        }
        catch (OperationCanceledException) {
            RecordCrudCancelled(operation, typeof(TDbModel));
            throw;
        }
        catch {
            RecordCrudFailure(operation, typeof(TDbModel));
            throw;
        }
    }

    public async Task<TResult?> Get<TDbModel, TResult>(
        object[] keys,
        IEnumerable<string>? includes = null,
        Action<GetContext<TDbModel, TContext>>? before = null,
        Action<GetContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "get_map";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        try {
            ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
            ArgumentHelpers.ThrowIfNullOrEmpty(keys, nameof(keys));
            using var scope = BeginActionScope("GET", null, typeof(TDbModel), typeof(TResult));
            var typeName = typeof(TDbModel).Name;
            var matIncludes = includes?.ToList() ?? [];
            var includeArray = matIncludes.Any() ? matIncludes.ToArray() : null;
            var cacheKey =
                $"entity:{typeName}:keys={string.Join("|", keys.Order().Select(i => i.ToString()))}{(!matIncludes.Any() ? "" : $":include={string.Join("|", matIncludes.Order().Select(i => i.ToString()))}")}";

            if (matIncludes.Any()) {
                await using var validationContext = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                validationContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                loaderService.ValidateIncludePaths<TContext, TDbModel>(validationContext, matIncludes);
            }

            var cachedResult = await cache.GetOrSetAsync<TResult?>(
                cacheKey, async ct2 => {
                    await using var context = await ContextFactory.CreateDbContextAsync(ct2).ConfigureAwait(false);
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    var types = loaderService.GetReferencedTypes<TContext, TDbModel>(context, matIncludes);
                    var tags = new List<string>(2 + types.Count) { "entities", $"entity:{typeof(TDbModel).Name.ToLowerInvariant()}" };
                    tags.AddRange(types.Select(i => $"entity:{i.Name.ToLowerInvariant()}"));
                    var result = await context.Set<TDbModel>().FindAsync(keys, ct2).ConfigureAwait(false);
                    if (result is null)
                        return (default, tags.ToArray());

                    await loaderService.LoadIncludes(context, result, matIncludes, ct2).ConfigureAwait(false);
                    var ctx = new GetContext<TDbModel, TContext>(keys, includeArray, result, context, serviceProvider);
                    before?.Invoke(ctx);
                    var response = MapOrCast<TDbModel, TResult>(Mapper, result);
                    after?.Invoke(ctx);
                    return (response, tags.ToArray());
                }, token: ct);

            if (cachedResult is null)
                RecordCrudFailure(operation, typeof(TDbModel));
            else
                RecordCrudSuccess(operation, typeof(TDbModel));

            return cachedResult;
        }
        catch (OperationCanceledException) {
            RecordCrudCancelled(operation, typeof(TDbModel));
            throw;
        }
        catch {
            RecordCrudFailure(operation, typeof(TDbModel));
            throw;
        }
    }

    public async Task<TDbModel?> Get<TDbModel>(object[] keys, IEnumerable<string>? includes = null, CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "get";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        try {
            ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
            ArgumentHelpers.ThrowIfNullOrEmpty(keys, nameof(keys));
            using var scope = BeginActionScope("GET", null, typeof(TDbModel), typeof(TDbModel));
            var typeName = typeof(TDbModel).Name;
            var matIncludes = includes?.ToList() ?? [];
            var cacheKey =
                $"entity:{typeName}:keys={string.Join("|", keys.Order().Select(i => i.ToString()))}{(!matIncludes.Any() ? "" : $":include={string.Join("|", matIncludes.Order().Select(i => i.ToString()))}")}:raw";

            if (matIncludes.Any()) {
                await using var validationContext = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                validationContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                loaderService.ValidateIncludePaths<TContext, TDbModel>(validationContext, matIncludes);
            }

            var cachedResult = await cache.GetOrSetAsync<TDbModel?>(
                cacheKey, async ct2 => {
                    await using var context = await ContextFactory.CreateDbContextAsync(ct2).ConfigureAwait(false);
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    var types = loaderService.GetReferencedTypes<TContext, TDbModel>(context, matIncludes);
                    var tags = new List<string>(2 + types.Count) { "entities", $"entity:{typeof(TDbModel).Name.ToLowerInvariant()}" };
                    tags.AddRange(types.Select(i => $"entity:{i.Name.ToLowerInvariant()}"));
                    var result = await context.Set<TDbModel>().FindAsync(keys, ct2).ConfigureAwait(false);
                    if (result is null)
                        return (null, tags.ToArray());

                    await loaderService.LoadIncludes(context, result, matIncludes, ct2).ConfigureAwait(false);
                    return (result, tags.ToArray());
                }, token: ct);

            if (cachedResult is null)
                RecordCrudFailure(operation, typeof(TDbModel));
            else
                RecordCrudSuccess(operation, typeof(TDbModel));

            return cachedResult;
        }
        catch (OperationCanceledException) {
            RecordCrudCancelled(operation, typeof(TDbModel));
            throw;
        }
        catch {
            RecordCrudFailure(operation, typeof(TDbModel));
            throw;
        }
    }

    private async Task<QueryRes<TDbModel>> QueryCore<TDbModel>(
        QueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        CancellationToken ct,
        QueryCoreCacheAugmentation? cacheAugmentation = null)
        where TDbModel : class
    {
        ArgumentHelpers.ThrowIfNull(queryRequest, nameof(queryRequest));
        ArgumentHelpers.ThrowIfNull(defaultOrder, nameof(defaultOrder));

        var pagingErrors = QueryPagingBoundsValidator.Validate(queryRequest, queryOptions, queryOptions.MaxPageSize);
        var pathCache = new QueryPathValidationCache();

        // Reuse one DbContext for include validation + query execution when Include is non-empty (same pattern as QueryProjectedCore).
        TContext? sharedIncludeValidationAndQueryContext = null;
        try {
            IReadOnlyList<Error> queryModelValidationErrors;
            if (queryRequest.Include.Count > 0) {
                sharedIncludeValidationAndQueryContext = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                queryModelValidationErrors = ProjectedQueryModelValidator.Validate(
                    new ProjectedQueryValidatorInput<TContext, TDbModel> {
                        PathCache = pathCache,
                        Db = sharedIncludeValidationAndQueryContext,
                        Loader = loaderService,
                        Filter = filterService,
                        Include = queryRequest.Include,
                        SortBy = queryRequest.SortBy,
                        Where = queryRequest.WhereClause
                    }).Errors ?? [];
            }
            else {
                queryModelValidationErrors = ProjectedQueryModelValidator.Validate(
                    new ProjectedQueryValidatorInput<TContext, TDbModel> {
                        PathCache = pathCache,
                        Db = null,
                        Loader = loaderService,
                        Filter = filterService,
                        Include = queryRequest.Include,
                        SortBy = queryRequest.SortBy,
                        Where = queryRequest.WhereClause
                    }).Errors ?? [];
            }

            if (pagingErrors.Count > 0 || queryModelValidationErrors.Count > 0) {
                var apiErrors = new List<ApiError>(pagingErrors.Count + queryModelValidationErrors.Count);
                apiErrors.AddRange(pagingErrors);
                apiErrors.AddRange(queryModelValidationErrors.Select(e => new ApiError(e.Code, e.Message)));

                Logger.LogWarning(
                    "Query validation failed for {Entity}: {IssueCount} issue(s). {Details}",
                    typeof(TDbModel).Name,
                    apiErrors.Count,
                    string.Join("; ", apiErrors.Select(static e => $"{e.Code}: {e.Description}")));

                return ResultFactory.QueryFailure<TDbModel>(
                    queryRequest,
                    AggregatedValidationProblemDetails(apiErrors, "Invalid query."));
            }

            string cacheKey;
            if (queryRequest.WhereClause != null) {
                cacheKey = QueryCacheKeyBuilder.BuildTree<TDbModel, TDbModel>(
                    queryRequest.WhereClause, queryRequest.Start, queryRequest.Amount, queryRequest.Include, queryRequest.SortBy.ToArray(), queryRequest.Options.TotalCountMode,
                    queryRequest.Options.IncludeFilterMode, queryRequest.Keys, cacheAugmentation?.SelectForCacheKey, cacheAugmentation?.ComputedForCacheKey);
            }
            else if (cacheAugmentation is { } aug && (aug.SelectForCacheKey is { Count: > 0 } || aug.ComputedForCacheKey is { Count: > 0 })) {
                cacheKey = QueryCacheKeyBuilder.BuildEntityLoadWithProjectionDimensions<TDbModel, TDbModel>(
                    queryRequest, aug.SelectForCacheKey ?? [], aug.ComputedForCacheKey ?? []);
            }
            else
                cacheKey = QueryCacheKeyBuilder.Build<TDbModel, TDbModel>(queryRequest);

            try {
                Logger.LogDebug("Query cache key: {CacheKey}", cacheKey);
                var results = await cache.GetOrSetAsync(cacheKey, BuildQueryResultsAsync!, token: ct).ConfigureAwait(false);
                return results!;
            }
            catch (Exception ex) {
                return ct.IsCancellationRequested
                    ? ResultFactory.QueryFailure<TDbModel>(
                        queryRequest,
                        LyoProblemDetails.FromCode(Models.Constants.ApiErrorCodes.Cancelled, "Request was cancelled.", DateTime.UtcNow))
                    : ResultFactory.QueryFailure<TDbModel>(queryRequest, LogAndReturnApiError(ex, "Query Error", Models.Constants.ApiErrorCodes.InvalidQuery));
            }

            async Task<(QueryRes<TDbModel> value, string[]? tags)> BuildQueryResultsAsync(CancellationToken ct2)
            {
                var ownsContext = sharedIncludeValidationAndQueryContext is null;
                TContext? context;
                if (ownsContext)
                    context = await ContextFactory.CreateDbContextAsync(ct2).ConfigureAwait(false);
                else
                    context = sharedIncludeValidationAndQueryContext!;

                try {
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                    // Include paths are already validated by ProjectedQueryModelValidator above (same paths, same model).

                    var types = loaderService.GetReferencedTypes<TContext, TDbModel>(context, queryRequest.Include);
                    var tags = new List<string>(3 + types.Count) { "queries", "entities", $"entity:{typeof(TDbModel).Name.ToLowerInvariant()}" };
                    tags.AddRange(types.Select(i => $"entity:{i.Name.ToLowerInvariant()}"));
                    var keysProvided = queryRequest.Keys.Count > 0;
                    var state = keysProvided
                        ? await pathExecutor.ExecuteKeyConstrainedPathAsync(context, queryRequest, defaultOrder, defaultSortDirection, ct2).ConfigureAwait(false)
                        : await pathExecutor.ExecuteNonKeyPathAsync(context, queryRequest, defaultOrder, defaultSortDirection, ct2).ConfigureAwait(false);

                    var (queryResults, total, hasMore) = await pagingHelper.ApplyPagingAndMaterializeAsync(
                            context, state, queryRequest, defaultOrder, defaultSortDirection, keysProvided, filterService, ct2)
                        .ConfigureAwait(false);

                    await ApplyPostLoadIncludesAsync(context, queryResults, queryRequest, keysProvided, state.IsInMemoryResults, ct2).ConfigureAwait(false);
                    ApplyMatchedOnlyFilterIfNeeded(queryRequest, queryResults);
                    var result = ResultFactory.QuerySuccess(queryRequest, queryResults, queryRequest.Start, queryResults.Length, total, hasMore);
                    return (result, tags.ToArray());
                }
                finally {
                    if (ownsContext && context is IAsyncDisposable owned)
                        await owned.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
        finally {
            if (sharedIncludeValidationAndQueryContext is IAsyncDisposable asyncCtx)
                await asyncCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    private readonly record struct QueryCoreCacheAugmentation(
        IReadOnlyList<string>? SelectForCacheKey,
        IReadOnlyList<ComputedField>? ComputedForCacheKey);

    private async Task<ProjectedQueryRes<object?>> QueryProjectedCore<TDbModel>(
        ProjectionQueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        CancellationToken ct)
        where TDbModel : class
    {
        ArgumentHelpers.ThrowIfNull(queryRequest, nameof(queryRequest));
        ArgumentHelpers.ThrowIfNull(defaultOrder, nameof(defaultOrder));

        // Echo the client request as-is (Select is mutated server-side for computed dependencies).
        var queryRequestForEcho = CloneProjectionQueryReq(queryRequest);

        var pathCache = new QueryPathValidationCache();

        IReadOnlyList<Error> CollectQueryModelErrors(TContext? db, IReadOnlyList<string> includePathsForValidation)
        {
            var queryModelResult = ProjectedQueryModelValidator.Validate(
                new ProjectedQueryValidatorInput<TContext, TDbModel> {
                    PathCache = pathCache,
                    Db = db,
                    Loader = loaderService,
                    Filter = filterService,
                    Include = includePathsForValidation,
                    SortBy = queryRequest.SortBy,
                    Where = queryRequest.WhereClause
                });

            return queryModelResult.IsSuccess ? [] : queryModelResult.Errors!;
        }

        var computedFields = queryRequest.ComputedFields;
        HashSet<string>? autoDerivedSelects = null;
        if (computedFields.Count > 0) {
            var addedForComputed = projectionService.EnsureSelectIncludesComputedDependencies(queryRequest);
            if (addedForComputed.Count > 0)
                autoDerivedSelects = BuildAutoDerivedStripSet(addedForComputed, queryRequestForEcho.Select);
        }

        var aggregatedErrors = new List<ApiError>();
        aggregatedErrors.AddRange(QueryPagingBoundsValidator.Validate(queryRequest, queryOptions, queryOptions.MaxPageSize));

        if (queryRequest.Select.Count == 0) 
            aggregatedErrors.Add(new(Models.Constants.ApiErrorCodes.InvalidQuery, "Projected query requires at least one selected field."));

        IReadOnlyList<ProjectedFieldSpec>? projectedFieldSpecs = null;
        if (queryRequest.Select.Count > 0) {
            var allowWild = queryOptions.AllowSelectWildcards;
            var (specs, pathErrors) = projectionService.ResolveProjectedFields<TDbModel>(queryRequest.Select, allowWild, pathCache);
            projectedFieldSpecs = specs;
            var projectionIssues = pathErrors.Count > 0
                ? pathErrors
                : projectionService.CollectProjectionFieldIssues<TDbModel>(projectedFieldSpecs, allowWild);
            aggregatedErrors.AddRange(projectionIssues);
        }

        if (computedFields.Count > 0)
            aggregatedErrors.AddRange(projectionService.ValidateComputedFieldTemplates(computedFields));

        if (aggregatedErrors.Count > 0) {
            Logger.LogWarning(
                "Projected query validation failed for {Entity}: {IssueCount} issue(s). {Details}",
                typeof(TDbModel).Name,
                aggregatedErrors.Count,
                string.Join("; ", aggregatedErrors.Select(static e => $"{e.Code}: {e.Description}")));

            return ResultFactory.ProjectedQueryFailure<object?>(
                queryRequestForEcho,
                AggregatedValidationProblemDetails(aggregatedErrors, "Invalid projected query."));
        }

        if (projectedFieldSpecs is null)
            throw new InvalidOperationException("Projected field specs must be resolved when Select is non-empty.");

        var effectiveIncludes = BuildQueryProjectEffectiveIncludes<TDbModel>(projectedFieldSpecs, queryRequest.WhereClause);

        // One DbContext for include validation + SQL path when derived includes are non-empty (avoids doubling context startup cost).
        TContext? sharedIncludeValidationAndSqlContext = null;
        try {
            if (effectiveIncludes.Count > 0)
                sharedIncludeValidationAndSqlContext = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

            foreach (var e in CollectQueryModelErrors(sharedIncludeValidationAndSqlContext, effectiveIncludes))
                aggregatedErrors.Add(new(e.Code, e.Message));

            if (aggregatedErrors.Count > 0) {
                Logger.LogWarning(
                    "Projected query validation failed for {Entity}: {IssueCount} issue(s). {Details}",
                    typeof(TDbModel).Name,
                    aggregatedErrors.Count,
                    string.Join("; ", aggregatedErrors.Select(static e => $"{e.Code}: {e.Description}")));

                return ResultFactory.ProjectedQueryFailure<object?>(
                    queryRequestForEcho,
                    AggregatedValidationProblemDetails(aggregatedErrors, "Invalid projected query."));
            }

            var cacheKeyProjection = CloneProjectionQueryReq(queryRequest);
            cacheKeyProjection.Include = effectiveIncludes;
            var keysProvided = queryRequest.Keys.Count > 0;
            var hasSubQuery = WhereClauseUtils.HasAnySubClause(queryRequest.WhereClause);
            var includeFilterMode = queryRequest.Options.IncludeFilterMode;
            var useMatchedOnly = includeFilterMode == QueryIncludeFilterMode.MatchedOnly;
            var sqlBuild = projectionService.TryBuildSqlProjectionExpression<TDbModel>(projectedFieldSpecs, projectionPathsAlreadyValidated: true);
            var sqlProjection = sqlBuild.Projection;
            var sqlConversionPlan = sqlBuild.ConversionPlan;
            var zipSiblingSelections = queryRequest.Options.ZipSiblingCollectionSelections;
            if (!keysProvided && !hasSubQuery && !useMatchedOnly && sqlProjection != null) {
                var cacheKeyBase = queryRequest.WhereClause != null
                    ? QueryCacheKeyBuilder.BuildTree<TDbModel, object>(
                        queryRequest.WhereClause, queryRequest.Start, queryRequest.Amount, cacheKeyProjection.Include, queryRequest.SortBy.ToArray(), queryRequest.Options.TotalCountMode,
                        queryRequest.Options.IncludeFilterMode, queryRequest.Keys, queryRequest.Select, queryRequest.ComputedFields)
                    : QueryCacheKeyBuilder.Build<TDbModel, object>(cacheKeyProjection);

                var cacheKey = QueryCacheKeyBuilder.AppendProjectedShapeSuffix(cacheKeyBase, zipSiblingSelections);

                Logger.LogDebug("Query cache key: {CacheKey}", cacheKey);
                try {
                    var sqlResult = await cache.GetOrSetAsync(
                            cacheKey, async ct2 => {
                                var r = await ExecuteSqlProjectedQueryAsync(
                                    queryRequest, defaultOrder, defaultSortDirection, projectedFieldSpecs, sqlProjection, sqlConversionPlan, sharedIncludeValidationAndSqlContext, ct2);
                                if (r == null)
                                    throw new SqlProjectionFallbackException();

                                IReadOnlyList<object?> items = r.Items!;
                                if (computedFields.Count > 0)
                                    items = projectionService.ApplyComputedFields(items, computedFields, projectedFieldSpecs);

                                projectionService.MergeSiblingCollectionProjectionRows(items, typeof(TDbModel), projectedFieldSpecs, zipSiblingSelections);

                                if (computedFields.Count > 0 && autoDerivedSelects is { Count: > 0 }) {
                                    projectionService.StripAutoDerivedDependencyLeavesFromMergedCollections(items, projectedFieldSpecs, autoDerivedSelects);
                                    StripAutoDerivedFields(items, autoDerivedSelects);
                                }

                                var entityTypes = projectionService.GetProjectionEntityTypeNames<TDbModel>(projectedFieldSpecs, computedFields);
                                ProjectedQueryRes<object?> projected = ResultFactory.ProjectedQuerySuccess(
                                    queryRequestForEcho, items, r.Start, items.Count, r.Total, r.HasMore, entityTypes: entityTypes);

                                var tags = new[] { "queries", "queryproject", $"entity:{typeof(TDbModel).Name.ToLowerInvariant()}" };
                                return (projected, tags);
                            }, token: ct)
                        .ConfigureAwait(false);

                    if (sqlResult != null)
                        return sqlResult;
                }
                catch (SqlProjectionFallbackException) {
                    // SQL projection failed; fall through to load-then-project path
                }
            }

            var workingProjection = CloneProjectionQueryReq(queryRequest);
            workingProjection.Select = new(projectedFieldSpecs.Count);
            foreach (var spec in projectedFieldSpecs)
                workingProjection.Select.Add(spec.NormalizedPath);

            workingProjection.Include = effectiveIncludes;
            var entityLoadRequest = ToQueryReq(workingProjection);
            var cacheAug = new QueryCoreCacheAugmentation(workingProjection.Select, workingProjection.ComputedFields);
            var raw = await QueryCore(entityLoadRequest, defaultOrder, defaultSortDirection, ct, cacheAug).ConfigureAwait(false);
            if (!raw.IsSuccess)
                return ResultFactory.ProjectedQueryFailure<object?>(queryRequestForEcho, raw.Error!);

            var projectionFilterConditions = projectionService.GetProjectedFilterConditions<TDbModel>(queryRequest.WhereClause);
            IReadOnlyList<object?> items = projectionService.ProjectEntities(raw.Items!, projectedFieldSpecs, includeFilterMode, projectionFilterConditions);
            if (computedFields.Count > 0)
                items = projectionService.ApplyComputedFields(items, computedFields, projectedFieldSpecs);

            projectionService.MergeSiblingCollectionProjectionRows(items, typeof(TDbModel), projectedFieldSpecs, zipSiblingSelections);

            if (computedFields.Count > 0 && autoDerivedSelects is { Count: > 0 }) {
                projectionService.StripAutoDerivedDependencyLeavesFromMergedCollections(items, projectedFieldSpecs, autoDerivedSelects);
                StripAutoDerivedFields(items, autoDerivedSelects);
            }

            var projectedEntityTypes = projectionService.GetProjectionEntityTypeNames<TDbModel>(projectedFieldSpecs, computedFields);
            return ResultFactory.ProjectedQuerySuccess(
                queryRequestForEcho, items, queryRequest.Start, items.Count, raw.Total, raw.HasMore, entityTypes: projectedEntityTypes);
        }
        finally {
            if (sharedIncludeValidationAndSqlContext is IAsyncDisposable asyncCtx)
                await asyncCtx.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>EF include strings for QueryProject: derived from <see cref="ProjectedFieldSpec" /> paths plus collection navigation required by the where clause.</summary>
    private List<string> BuildQueryProjectEffectiveIncludes<TDbModel>(IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs, WhereClause? whereClause)
        where TDbModel : class
    {
        var fromSelect = projectionService.GetDerivedIncludes(typeof(TDbModel), projectedFieldSpecs);
        var fromWhere = filterService.GetCollectionIncludePathsForWhereClause<TDbModel>(whereClause);
        return fromSelect.Concat(fromWhere).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Paths to remove from the response after projection: only those appended for computed templates, excluding anything the client listed in <see cref="ProjectionQueryReq.Select" />
    /// before dependency injection (even if the same path is also a template placeholder).
    /// </summary>
    private static HashSet<string> BuildAutoDerivedStripSet(IReadOnlyList<string> addedForComputed, IReadOnlyList<string> userOriginalSelect)
    {
        var userPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in userOriginalSelect) {
            if (string.IsNullOrWhiteSpace(s))
                continue;
            userPaths.Add(s.Trim());
        }

        var strip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in addedForComputed) {
            if (string.IsNullOrWhiteSpace(p))
                continue;
            var t = p.Trim();
            if (userPaths.Contains(t))
                continue;
            strip.Add(t);
        }

        return strip;
    }

    /// <summary>Removes auto-derived dependency columns added for computed templates. Call after sibling merge so zip still sees those columns.</summary>
    private static void StripAutoDerivedFields(IReadOnlyList<object?> items, HashSet<string> fieldsToRemove)
    {
        foreach (var item in items) 
            if (item is Dictionary<string, object?> dict) 
                foreach (var key in fieldsToRemove)
                    dict.Remove(key);
    }

    /// <summary>Executes a projected query with SQL-level projection when possible. Returns null if translation fails (fallback to load-then-project).</summary>
    private async Task<ProjectedQueryRes<object?>?> ExecuteSqlProjectedQueryAsync<TDbModel>(
        ProjectionQueryReq queryRequest,
        Expression<Func<TDbModel, object?>> defaultOrder,
        SortDirection defaultSortDirection,
        IReadOnlyList<ProjectedFieldSpec> projectedFieldSpecs,
        Expression<Func<TDbModel, object?>> projection,
        SqlProjectionConversionPlan? sqlConversionPlan,
        TContext? reuseContextFromIncludeValidation,
        CancellationToken ct)
        where TDbModel : class
    {
        var ownsContext = reuseContextFromIncludeValidation is null;
        TContext context;
        if (ownsContext)
            context = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        else
            context = reuseContextFromIncludeValidation!;

        try {
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var baseQueryable = context.Set<TDbModel>().AsQueryable();
            var filteredQueryable = filterService.ApplyWhereClause(baseQueryable, queryRequest.WhereClause);
            var totalCountMode = queryRequest.Options.TotalCountMode;
            var computeExactTotal = totalCountMode == QueryTotalCountMode.Exact;
            int? total = null;
            if (computeExactTotal)
                total = await QueryRootCountHelper.CountDistinctRootEntitiesAsync(context, filteredQueryable, ct).ConfigureAwait(false);

            var orderedQueryable = filterService.ApplyOrdering(filteredQueryable, queryRequest.SortBy, defaultOrder, defaultSortDirection);
            var pageSize = queryRequest.Amount ?? queryOptions.DefaultPageSize;
            var startIndex = queryRequest.Start ?? 0;
            var takeSize = totalCountMode == QueryTotalCountMode.HasMore ? pageSize + 1 : pageSize;
            var projectedQueryable = orderedQueryable.Skip(startIndex).Take(takeSize).Select(projection);
            if (queryOptions.EnableSplitQueries && SqlProjectionJoinShape.LikelyCausesReaderFanOut(typeof(TDbModel), sqlConversionPlan, projectedFieldSpecs)) {
#pragma warning disable CS8634 // IQueryable<object?> vs AsSplitQuery<TEntity> where TEntity : class
                projectedQueryable = projectedQueryable.AsSplitQuery();
#pragma warning restore CS8634
            }

            var rawProjected = await projectedQueryable.ToListAsync(ct).ConfigureAwait(false);
            var hasMore = totalCountMode == QueryTotalCountMode.HasMore && rawProjected.Count > pageSize;
            var items = hasMore ? rawProjected.Take(pageSize).ToList() : rawProjected;
            var converted = projectionService.ConvertSqlProjectedResults(items, projectedFieldSpecs, sqlConversionPlan);
            if (totalCountMode == QueryTotalCountMode.HasMore && !hasMore)
                total = startIndex + converted.Count;

            Logger.LogDebug("SQL-level projection applied for {EntityType} ({FieldCount} fields)", typeof(TDbModel).Name, projectedFieldSpecs.Count);
            var sqlEntityTypes = projectionService.GetProjectionEntityTypeNames<TDbModel>(projectedFieldSpecs, queryRequest.ComputedFields);
            return ResultFactory.ProjectedQuerySuccess(queryRequest, converted, startIndex, converted.Count, total, hasMore, entityTypes: sqlEntityTypes);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException && ex.Message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase)) {
            Logger.LogDebug(ex, "SQL projection failed for {EntityType}; using fallback path", typeof(TDbModel).Name);
            return null;
        }
        finally {
            if (ownsContext && context is IAsyncDisposable owned)
                await owned.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task ApplyPostLoadIncludesAsync<TDbModel>(
        TContext context,
        TDbModel[] queryResults,
        QueryReq queryRequest,
        bool keysProvided,
        bool isInMemoryResults,
        CancellationToken ct)
        where TDbModel : class
    {
        if (isInMemoryResults && !keysProvided && queryRequest.Include.Count > 0) {
            foreach (var entity in queryResults)
                await loaderService.LoadIncludes(context, entity, queryRequest.Include, ct).ConfigureAwait(false);
        }
    }

    private void ApplyMatchedOnlyFilterIfNeeded<TDbModel>(QueryReq queryRequest, TDbModel[] queryResults)
        where TDbModel : class
    {
        if (queryRequest.Options.IncludeFilterMode != QueryIncludeFilterMode.MatchedOnly || queryRequest.Include.Count == 0 || queryResults.Length == 0)
            return;

        var includeFilterConditions = projectionService.GetProjectedFilterConditions<TDbModel>(queryRequest.WhereClause);
        projectionService.ApplyMatchedOnlyIncludes(queryResults, queryRequest.Include, includeFilterConditions);
    }

    private static ProjectionQueryReq CloneProjectionQueryReq(ProjectionQueryReq source)
    {
        var sourceOptions = source.Options;
        return new() {
            Start = source.Start,
            Amount = source.Amount,
            Options = new() {
                TotalCountMode = sourceOptions.TotalCountMode,
                IncludeFilterMode = sourceOptions.IncludeFilterMode,
                ZipSiblingCollectionSelections = sourceOptions.ZipSiblingCollectionSelections
            },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Select = [..source.Select],
            ComputedFields = [..source.ComputedFields.Select(c => new ComputedField(c.Name, c.Template))],
            Keys = [..source.Keys.Select(i => i.ToArray())],
            SortBy = [..source.SortBy.Select(s => new SortBy { PropertyName = s.PropertyName, Direction = s.Direction, Priority = s.Priority })]
        };
    }

    private static QueryReq ToQueryReq(ProjectionQueryReq source)
        => new() {
            Start = source.Start,
            Amount = source.Amount,
            Options = new QueryRequestOptions {
                TotalCountMode = source.Options.TotalCountMode,
                IncludeFilterMode = source.Options.IncludeFilterMode
            },
            WhereClause = source.WhereClause,
            Include = [..source.Include],
            Keys = [..source.Keys.Select(i => i.ToArray())],
            SortBy = [..source.SortBy.Select(s => new SortBy { PropertyName = s.PropertyName, Direction = s.Direction, Priority = s.Priority })]
        };

    private async Task<TResult[]> MapResultsAsync<TDbModel, TResult>(IReadOnlyList<TDbModel> dbResults, CancellationToken ct)
        where TDbModel : class
    {
        var results = new TResult[dbResults.Count];
        var counter = 0;
        foreach (var dbResult in dbResults) {
            try {
                var result = MapOrCast<TDbModel, TResult>(Mapper, dbResult);
                results[counter] = result;
                counter++;
            }
            catch (Exception ex) {
                await using var context = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
                var ids = typeConversion.GetPrimaryKeyValues(dbResult, context);
                Logger.LogError(ex, "Could not map object with Ids {InvalidMapObjectId}", string.Join(",", ids));
            }
        }

        return results;
    }

    private static LyoProblemDetails AggregatedValidationProblemDetails(IReadOnlyList<ApiError> errors, string rootSummary)
        => LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidQuery)
            .WithMessage(rootSummary)
            .AddErrors(errors)
            .Build();

    /// <summary>Thrown when SQL projection fails so we fall through to load-then-project without caching.</summary>
    private sealed class SqlProjectionFallbackException : Exception;
}