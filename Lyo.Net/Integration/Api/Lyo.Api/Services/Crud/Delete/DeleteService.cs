using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Api.Services.TypeConversion;
using Lyo.Cache;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Delete;

public class DeleteService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    IEntityLoaderService loaderService,
    IWhereClauseService filterService,
    ITypeConversionService typeConversion,
    BulkOperationOptions bulkOptions,
    CacheOptions cacheOptions,
    ICacheService cache,
    IServiceProvider serviceProvider,
    ILogger<DeleteService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), IDeleteService<TContext>
    where TContext : DbContext
{
    public async Task<DeleteResult<TResult>> DeleteAsync<TDbModel, TResult>(
        object[] keys,
        Action<DeleteContext<TDbModel, TContext>>? before = null,
        Action<DeleteContext<TDbModel, TContext>>? after = null,
        IEnumerable<string>? includes = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "delete";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        ArgumentHelpers.ThrowIfNull(keys, nameof(keys));
        ArgumentHelpers.ThrowIfNullOrEmpty(keys, nameof(keys));
        using var scope = BeginActionScope("DELETE", null, typeof(TDbModel), typeof(TResult));
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entity = await context.Set<TDbModel>().FindAsync(keys, ct);
        if (entity is null) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.DeleteFailure<TResult>(CreateNotFoundError<TDbModel>(keys.Select(k => k.ToString())!));
        }

        var includeProblem = TryIncludePathProblem<TContext, TDbModel>(context, includes);
        if (includeProblem is not null) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.DeleteFailure<TResult>(includeProblem);
        }

        await loaderService.LoadIncludes(context, entity, includes, ct);
        var primaryKeyForCache = typeConversion.GetPrimaryKeyValues(entity, context);
        var result = await DeleteInternal<TDbModel, TResult>(keys, null, entity, context, before, after, ct);
        if (result.IsSuccess) {
            await QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, cacheOptions, typeof(TDbModel), [primaryKeyForCache], ct)
                .ConfigureAwait(false);
        }

        if (result.IsSuccess)
            RecordCrudSuccess(operation, typeof(TDbModel));
        else
            RecordCrudFailure(operation, typeof(TDbModel));

        return result;
    }

    public async Task<DeleteResult<TResult>> DeleteAsync<TDbModel, TResult>(
        DeleteRequest request,
        Action<DeleteContext<TDbModel, TContext>>? before = null,
        Action<DeleteContext<TDbModel, TContext>>? after = null,
        IEnumerable<string>? includes = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "delete_by_request";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        using var scope = BeginActionScope("DELETE", typeof(DeleteRequest), typeof(TDbModel), typeof(TResult));
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct);
        if (entities.Count == 0) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.DeleteFailure<TResult>(
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                    .WithMessage($"Entity not found for deletion.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                    .Build());
        }

        if (entities.Count > 1 && !request.AllowMultiple) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.DeleteFailure<TResult>(
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                    .WithMessage($"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                    .Build());
        }

        // For single delete, just delete the first entity
        var entity = entities[0];
        var includeProblem = TryIncludePathProblem<TContext, TDbModel>(context, includes);
        if (includeProblem is not null) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.DeleteFailure<TResult>(includeProblem);
        }

        await loaderService.LoadIncludes(context, entity, includes, ct);
        var keys = request.Keys?.FirstOrDefault() ?? [];
        var primaryKeyForCache = typeConversion.GetPrimaryKeyValues(entity, context);
        var result = await DeleteInternal<TDbModel, TResult>(keys, request, entity, context, before, after, ct);
        if (result.IsSuccess) {
            await QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, cacheOptions, typeof(TDbModel), [primaryKeyForCache], ct)
                .ConfigureAwait(false);
        }

        if (result.IsSuccess)
            RecordCrudSuccess(operation, typeof(TDbModel));
        else
            RecordCrudFailure(operation, typeof(TDbModel));

        return result;
    }

    public async Task<DeleteBulkResult<TResult>> DeleteBulkAsync<TDbModel, TResult>(
        IEnumerable<DeleteRequest> requests,
        Action<DeleteContext<TDbModel, TContext>>? before = null,
        Action<DeleteContext<TDbModel, TContext>>? after = null,
        IEnumerable<string>? includes = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "delete_bulk";
        RecordCrudRequest(operation, typeof(TDbModel), true);
        using var timer = StartCrudTimer(operation, typeof(TDbModel), true);
        var requestList = requests as IReadOnlyList<DeleteRequest> ?? requests.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(requestList, nameof(requests));
        using var scope = BeginActionScope("DELETE BULK", typeof(DeleteRequest), typeof(TDbModel), typeof(TResult));
        var bulkValidation = BulkListRequestValidator.Validate(new(requestList.Count, bulkOptions.MaxAmount));
        if (!bulkValidation.IsSuccess) {
            var err = bulkValidation.Errors![0];
            Logger.LogWarning("Bulk delete size validation failed: {Code} {Message}", err.Code, err.Message);
            throw new LFException(err.Code, err.Message);
        }

        var bulkResult = await TryBulkDeleteAll<TDbModel, TResult>(requestList, before, after, includes, ct);
        if (bulkResult != null) {
            Logger.LogInformation("Bulk delete completed successfully for {Count} requests", requestList.Count);
            if (bulkResult.DeletedCount > 0)
                RecordCrudSuccess(operation, typeof(TDbModel), true);

            if (bulkResult.FailedCount > 0)
                RecordCrudFailure(operation, typeof(TDbModel), true);

            RecordCrudResultCount(operation, typeof(TDbModel), bulkResult.DeletedCount, true);
            return bulkResult;
        }

        Logger.LogWarning("Bulk delete failed, falling back to partial retry strategy for {Count} requests", requestList.Count);
        var retryResult = await DeleteWithPartialRetry<TDbModel, TResult>(requestList, before, after, includes, ct);
        if (retryResult.DeletedCount > 0)
            RecordCrudSuccess(operation, typeof(TDbModel), true);

        if (retryResult.FailedCount > 0)
            RecordCrudFailure(operation, typeof(TDbModel), true);

        RecordCrudResultCount(operation, typeof(TDbModel), retryResult.DeletedCount, true);
        return retryResult;
    }

    private async Task<DeleteBulkResult<TResult>?> TryBulkDeleteAll<TDbModel, TResult>(
        IReadOnlyList<DeleteRequest> requests,
        Action<DeleteContext<TDbModel, TContext>>? before,
        Action<DeleteContext<TDbModel, TContext>>? after,
        IEnumerable<string>? includes,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var results = new List<DeleteResult<TResult>>();
            var deletedKeySets = new List<IReadOnlyList<object?>>();
            foreach (var request in requests) {
                var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct);
                if (entities.Count == 0) {
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                        .WithMessage($"Entity not found for deletion.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.DeleteFailure<TResult>(err));
                    continue;
                }

                if (entities.Count > 1 && !request.AllowMultiple) {
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage(
                            $"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.DeleteFailure<TResult>(err));
                    continue;
                }

                // Delete all matching entities if AllowMultiple is true
                foreach (var entity in entities) {
                    await loaderService.LoadIncludes(context, entity, includes, ct);
                    var keys = request.Keys?.FirstOrDefault() ?? [];
                    deletedKeySets.Add(typeConversion.GetPrimaryKeyValues(entity, context));
                    var ctx = new DeleteContext<TDbModel, TContext>(keys, request, entity, context, serviceProvider);
                    before?.Invoke(ctx);
                    var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
                    context.Set<TDbModel>().Remove(entity);
                    results.Add(ResultFactory.DeleteSuccess(old));
                }
            }

            await context.SaveChangesAsync(ct);
            foreach (var result in results.Where(r => r.IsSuccess))
                // After hooks run on successfully deleted entities - entity is detached so pass null
                after?.Invoke(new([], null, null!, context, serviceProvider));

            var deleted = results.Count(r => r.IsSuccess);
            var failed = results.Count(r => !r.IsSuccess);
            var bulkResult = new DeleteBulkResult<TResult>(results, deleted, failed);
            if (deleted > 0)
                await QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, cacheOptions, typeof(TDbModel), deletedKeySets, ct).ConfigureAwait(false);

            return bulkResult;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Bulk delete failed, will attempt partial retry");
            return null;
        }
    }

    private async Task<DeleteBulkResult<TResult>> DeleteWithPartialRetry<TDbModel, TResult>(
        IReadOnlyList<DeleteRequest> requests,
        Action<DeleteContext<TDbModel, TContext>>? before,
        Action<DeleteContext<TDbModel, TContext>>? after,
        IEnumerable<string>? includes,
        CancellationToken ct = default)
        where TDbModel : class
    {
        var results = new List<DeleteResult<TResult>>();
        var failed = new List<(int Index, DeleteRequest Request)>();
        int successCount = 0, failureCount = 0;
        var (successResults, failedRequests) = await TryBulkDeleteWithTracking<TDbModel, TResult>(requests, before, after, includes, ct);
        results.AddRange(successResults);
        successCount += successResults.Count;
        failed.AddRange(failedRequests);
        failureCount += failedRequests.Count;
        if (failed.Count > 0) {
            Logger.LogWarning("Retrying {FailedCount} failed items individually", failed.Count);
            foreach (var (index, request) in failed) {
                var individualResult = await DeleteIndividual<TDbModel, TResult>(request, before, after, includes, ct);
                if (index < results.Count)
                    results.Insert(index, individualResult);
                else
                    results.Add(individualResult);

                if (individualResult.IsSuccess)
                    successCount++;
                else
                    failureCount++;
            }
        }

        if (successCount > 0)
            await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<TDbModel>(cache, ct).ConfigureAwait(false);

        return new(results, successCount, failureCount);
    }

    private async Task<(List<DeleteResult<TResult>> Successes, List<(int Index, DeleteRequest Request)> Failures)> TryBulkDeleteWithTracking<TDbModel, TResult>(
        IReadOnlyList<DeleteRequest> requests,
        Action<DeleteContext<TDbModel, TContext>>? before,
        Action<DeleteContext<TDbModel, TContext>>? after,
        IEnumerable<string>? includes,
        CancellationToken ct = default)
        where TDbModel : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entityMap = new Dictionary<int, List<(TDbModel Entity, DeleteRequest Request, object[] Keys)>>();
        var successes = new List<DeleteResult<TResult>>();
        var failures = new List<(int Index, DeleteRequest Request)>();
        try {
            var index = 0;
            foreach (var request in requests) {
                try {
                    var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct);
                    if (entities.Count == 0) {
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                            .WithMessage($"Entity not found for deletion.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.DeleteFailure<TResult>(err));
                        index++;
                        continue;
                    }

                    if (entities.Count > 1 && !request.AllowMultiple) {
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                            .WithMessage($"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.DeleteFailure<TResult>(err));
                        index++;
                        continue;
                    }

                    // Process all matching entities
                    var deletedEntities = new List<(TDbModel Entity, DeleteRequest Request, object[] Keys)>();
                    foreach (var entity in entities) {
                        await loaderService.LoadIncludes(context, entity, includes, ct);
                        var keys = request.Keys?.FirstOrDefault() ?? [];
                        var ctx = new DeleteContext<TDbModel, TContext>(keys, request, entity, context, serviceProvider);
                        before?.Invoke(ctx);
                        context.Set<TDbModel>().Remove(entity);
                        deletedEntities.Add((entity, request, keys));
                    }

                    if (deletedEntities.Count > 0)
                        entityMap[index] = deletedEntities;
                }
                catch (Exception ex) {
                    Logger.LogWarning(ex, "Failed to process delete request at index {Index}", index);
                    failures.Add((index, request));
                }

                index++;
            }

            if (entityMap.Count > 0) {
                await context.SaveChangesAsync(ct);
                foreach (var (mapIndex, entities) in entityMap) {
                    try {
                        foreach (var (entity, request, keys) in entities) {
                            var ctx = new DeleteContext<TDbModel, TContext>(keys, request, entity, context, serviceProvider);
                            after?.Invoke(ctx);
                            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
                            successes.Add(ResultFactory.DeleteSuccess(result));
                        }
                    }
                    catch (Exception ex) {
                        Logger.LogWarning(ex, "Failed to process after hook at index {Index}", mapIndex);
                        failures.Add((mapIndex, requests[mapIndex]));
                    }
                }
            }
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Unexpected error during bulk delete tracking");
            return (successes, failures.Count > 0 ? failures : requests.Select((r, i) => (i, r)).ToList());
        }

        return (successes, failures);
    }

    private async Task<DeleteResult<TResult>> DeleteIndividual<TDbModel, TResult>(
        DeleteRequest request,
        Action<DeleteContext<TDbModel, TContext>>? before,
        Action<DeleteContext<TDbModel, TContext>>? after,
        IEnumerable<string>? includes,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct);
            if (entities.Count == 0) {
                return ResultFactory.DeleteFailure<TResult>(
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                        .WithMessage($"Entity not found for deletion.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            if (entities.Count > 1 && !request.AllowMultiple) {
                return ResultFactory.DeleteFailure<TResult>(
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage($"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            // For individual retry, just delete the first entity
            var entity = entities[0];
            await loaderService.LoadIncludes(context, entity, includes, ct);
            var keys = request.Keys?.FirstOrDefault() ?? [];
            return await DeleteInternal<TDbModel, TResult>(keys, request, entity, context, before, after, ct);
        }
        catch (Exception ex) {
            return ResultFactory.DeleteFailure<TResult>(LogAndReturnApiError(ex, "Individual Delete Error", Models.Constants.ApiErrorCodes.SqlException));
        }
    }

    private LyoProblemDetails? TryIncludePathProblem<TContextModel, TDbModel>(TContextModel context, IEnumerable<string>? includes)
        where TContextModel : DbContext where TDbModel : class
    {
        if (includes == null)
            return null;

        var mat = includes.Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        if (mat.Count == 0)
            return null;

        var errs = loaderService.CollectIncludePathErrors<TContextModel, TDbModel>(context, mat);
        if (errs.Count == 0)
            return null;

        return LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidInclude)
            .WithMessage("One or more include paths are invalid.")
            .AddErrors(errs.Select(e => new ApiError(e.Code, e.Message, e.StackTrace)))
            .Build();
    }

    private async Task<List<TDbModel>> FindEntitiesByRequest<TDbModel>(TContext context, DeleteRequest request, CancellationToken ct = default)
        where TDbModel : class
    {
        var entityTypeName = typeof(TDbModel).Name;

        // Use Keys if provided
        if (request.Keys != null && request.Keys.Count > 0) {
            Logger.LogDebug("Searching for {EntityType} using {KeyCount} key sets", entityTypeName, request.Keys.Count);
            var entities = new List<TDbModel>();
            foreach (var keySet in request.Keys) {
                var keys = typeConversion.ConvertKeysForFind<TDbModel>(keySet, context);
                var entity = await context.Set<TDbModel>().FindAsync(keys, ct);
                if (entity != null)
                    entities.Add(entity);
            }

            Logger.LogDebug("Found {EntityCount} entities for {EntityType} using keys", entities.Count, entityTypeName);
            return entities;
        }

        // Use Query if provided
        if (request.Query != null) {
            Logger.LogDebug("Searching for {EntityType} with Query", entityTypeName);
            var query = filterService.ApplyWhereClause(context.Set<TDbModel>().AsQueryable(), request.Query);
            var entities = await query.ToListAsync(ct);
            Logger.LogDebug("Found {EntityCount} entities for {EntityType} using identifiers", entities.Count, entityTypeName);
            return entities;
        }

        Logger.LogWarning("No Keys or Query provided for {EntityType} deletion", entityTypeName);
        return [];
    }

    private async Task<DeleteResult<TResult>> DeleteInternal<TDbModel, TResult>(
        object[] keys,
        DeleteRequest? request,
        TDbModel entity,
        TContext context,
        Action<DeleteContext<TDbModel, TContext>>? before,
        Action<DeleteContext<TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
            var ctx = new DeleteContext<TDbModel, TContext>(keys, request, entity, context, serviceProvider);
            before?.Invoke(ctx);
            context.Set<TDbModel>().Remove(entity);
            await context.SaveChangesAsync(ct);
            after?.Invoke(ctx);
            return ResultFactory.DeleteSuccess(old);
        }
        catch (Exception ex) {
            return ResultFactory.DeleteFailure<TResult>(LogAndReturnApiError(ex, "Delete Error"));
        }
    }
}