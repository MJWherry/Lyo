using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Api.Services.TypeConversion;
using Lyo.Cache;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Update;

public class UpdateService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    IWhereClauseService filterService,
    BulkOperationOptions bulkOptions,
    ITypeConversionService typeConversion,
    ICacheService cache,
    IServiceProvider serviceProvider,
    ILogger<UpdateService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), IUpdateService<TContext>
    where TContext : DbContext
{
    public async Task<UpdateResult<TResult>> UpdateAsync<TRequest, TDbModel, TResult>(
        UpdateRequest<TRequest> request,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "update";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        using var scope = BeginActionScope("UPDATE", typeof(TRequest), typeof(TDbModel), typeof(TResult));
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
        if (entities.Count == 0) {
            var keys = request.Keys ?? [];
            return ResultFactory.UpdateFailure<TResult>(keys, LogAndReturnApiError("Not Found", Models.Constants.ApiErrorCodes.NotFound));
        }

        if (entities.Count > 1) {
            var keys = request.Keys ?? [];
            return ResultFactory.UpdateFailure<TResult>(
                keys,
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                    .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                    .Build());
        }

        var entity = entities[0];
        var result = await UpdateInternal<TRequest, TDbModel, TResult>(entity, request, context, before, after, ct);
        if (result.Result is UpdateResultEnum.NoChange or UpdateResultEnum.Failed) {
            if (result.Result == UpdateResultEnum.Failed)
                RecordCrudFailure(operation, typeof(TDbModel));
            else
                RecordCrudSuccess(operation, typeof(TDbModel));

            return result;
        }

        await cache.InvalidateQueryCacheAsync<TDbModel>();
        var entityKeys = typeConversion.GetPrimaryKeyValues(entity, context);
        cache.Set(
            $"entity:{typeof(TDbModel).Name}:keys={string.Join("|", entityKeys.Order().Select(i => i?.ToString()))}", result.NewData!,
            ["entities", $"entity:{typeof(TDbModel).Name.ToLowerInvariant()}"]);

        RecordCrudSuccess(operation, typeof(TDbModel));
        return result;
    }

    public async Task<UpdateBulkResult<TResult>> UpdateBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<UpdateRequest<TRequest>> requests,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "update_bulk";
        RecordCrudRequest(operation, typeof(TDbModel), true);
        using var timer = StartCrudTimer(operation, typeof(TDbModel), true);
        var requestList = requests as IReadOnlyList<UpdateRequest<TRequest>> ?? requests.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(requestList, nameof(requests));
        using var scope = BeginActionScope("UPDATE BULK", typeof(TRequest), typeof(TDbModel), typeof(TResult));
        var bulkValidation = BulkListRequestValidator.Validate(new(requestList.Count, bulkOptions.MaxAmount));
        if (!bulkValidation.IsSuccess) {
            var err = bulkValidation.Errors![0];
            Logger.LogWarning("Bulk update size validation failed: {Code} {Message}", err.Code, err.Message);
            throw new LFException(err.Code, err.Message);
        }

        var bulkResult = await TryBulkUpdateAll<TRequest, TDbModel, TResult>(requestList, before, after, ct);
        if (bulkResult != null) {
            Logger.LogInformation("Bulk update completed successfully for {Count} requests", requestList.Count);
            if (bulkResult.UpdatedCount + bulkResult.NoChangeCount > 0)
                RecordCrudSuccess(operation, typeof(TDbModel), true);

            if (bulkResult.FailedCount > 0)
                RecordCrudFailure(operation, typeof(TDbModel), true);

            RecordCrudResultCount(operation, typeof(TDbModel), bulkResult.UpdatedCount, true);
            return bulkResult;
        }

        Logger.LogWarning("Bulk update failed, falling back to partial retry strategy for {Count} requests", requestList.Count);
        var retryResult = await UpdateWithPartialRetry<TRequest, TDbModel, TResult>(requestList, before, after, ct);
        if (retryResult.UpdatedCount + retryResult.NoChangeCount > 0)
            RecordCrudSuccess(operation, typeof(TDbModel), true);

        if (retryResult.FailedCount > 0)
            RecordCrudFailure(operation, typeof(TDbModel), true);

        RecordCrudResultCount(operation, typeof(TDbModel), retryResult.UpdatedCount, true);
        return retryResult;
    }

    private async Task<UpdateBulkResult<TResult>?> TryBulkUpdateAll<TRequest, TDbModel, TResult>(
        IReadOnlyList<UpdateRequest<TRequest>> requests,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var results = new List<UpdateResult<TResult>>();

            // Dictionary to store old data and request keyed by entity reference
            var oldDataMap = new Dictionary<TDbModel, (TResult OldData, object[] Keys)>();
            var requestMap = new Dictionary<TDbModel, UpdateRequest<TRequest>>();
            foreach (var request in requests) {
                ct.ThrowIfCancellationRequested();
                var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
                if (entities.Count == 0) {
                    var keys = request.Keys ?? [];
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                        .WithMessage($"Entity not found for update.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.UpdateFailure<TResult>(keys, err));
                    continue;
                }

                if (entities.Count > 1) {
                    var keys = request.Keys ?? [];
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.UpdateFailure<TResult>(keys, err));
                    continue;
                }

                // Update all matching entities
                foreach (var entity in entities) {
                    var oldData = MapOrCast<TDbModel, TResult>(Mapper, entity);
                    var entityKeys = typeConversion.GetPrimaryKeyValues(entity, context);

                    // Store old data in dictionary BEFORE mapping
                    oldDataMap[entity] = (oldData, [entityKeys]);
                    requestMap[entity] = request;
                    Mapper.Map(request.Data, entity);
                    var ctx = new UpdateContext<TRequest, TDbModel, TContext>(request, entity, context, serviceProvider);
                    before?.Invoke(ctx);
                }
            }

            await context.SaveChangesAsync(ct);
            foreach (var entity in context.ChangeTracker.Entries<TDbModel>()) {
                if (requestMap.TryGetValue(entity.Entity, out var req)) {
                    var ctx = new UpdateContext<TRequest, TDbModel, TContext>(req, entity.Entity, context, serviceProvider);
                    after?.Invoke(ctx);
                }

                if (oldDataMap.TryGetValue(entity.Entity, out var oldInfo)) {
                    var newData = MapOrCast<TDbModel, TResult>(Mapper, entity.Entity);
                    var s = entity.State switch {
                        EntityState.Modified => UpdateResultEnum.Updated,
                        EntityState.Unchanged => UpdateResultEnum.NoChange,
                        var _ => UpdateResultEnum.Failed
                    };

                    results.Add(ResultFactory.UpdateSuccess(oldInfo.Keys, oldInfo.OldData, newData, s));
                }
            }

            var updated = results.Count(r => r.Result == UpdateResultEnum.Updated);
            var failed = results.Count(r => r.Result == UpdateResultEnum.Failed);
            var noChange = results.Count(r => r.Result == UpdateResultEnum.NoChange);
            var bulkResult = new UpdateBulkResult<TResult>(results, updated, failed, noChange);
            if (updated > 0)
                await cache.InvalidateQueryCacheAsync<TDbModel>();

            return bulkResult;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Bulk update failed, will attempt partial retry");
            return null;
        }
    }

    private async Task<UpdateBulkResult<TResult>> UpdateWithPartialRetry<TRequest, TDbModel, TResult>(
        IReadOnlyList<UpdateRequest<TRequest>> requests,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        var results = new List<UpdateResult<TResult>>();
        var failed = new List<(int Index, UpdateRequest<TRequest> Request)>();
        var (successResults, failedRequests) = await TryBulkUpdateWithTracking<TRequest, TDbModel, TResult>(requests, before, after, ct);
        results.AddRange(successResults);
        failed.AddRange(failedRequests);
        if (failed.Count > 0) {
            Logger.LogWarning("Retrying {FailedCount} failed items individually", failed.Count);
            foreach (var (index, request) in failed) {
                var individualResult = await UpdateIndividual<TRequest, TDbModel, TResult>(request, before, after, ct);
                if (index < results.Count)
                    results.Insert(index, individualResult);
                else
                    results.Add(individualResult);
            }
        }

        var updated = results.Count(r => r.Result == UpdateResultEnum.Updated);
        var failureCount = results.Count(r => r.Result == UpdateResultEnum.Failed);
        var noChange = results.Count(r => r.Result == UpdateResultEnum.NoChange);
        if (updated > 0)
            await cache.InvalidateQueryCacheAsync<TDbModel>();

        return new(results, updated, failureCount, noChange);
    }

    private async Task<(List<UpdateResult<TResult>> Successes, List<(int Index, UpdateRequest<TRequest> Request)> Failures)> TryBulkUpdateWithTracking<TRequest, TDbModel, TResult>(
        IReadOnlyList<UpdateRequest<TRequest>> requests,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entityMap = new Dictionary<int, List<(TDbModel Entity, TResult OldData, IReadOnlyList<object> Keys)>>();
        var successes = new List<UpdateResult<TResult>>();
        var failures = new List<(int Index, UpdateRequest<TRequest> Request)>();
        try {
            var index = 0;
            foreach (var request in requests) {
                try {
                    var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
                    if (entities.Count == 0) {
                        var keys = request.Keys ?? [];
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                            .WithMessage($"Entity not found for update.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.UpdateFailure<TResult>(keys, err));
                        index++;
                        continue;
                    }

                    if (entities.Count > 1) {
                        var keys = request.Keys ?? [];
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                            .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.UpdateFailure<TResult>(keys, err));
                        index++;
                        continue;
                    }

                    // Update all matching entities
                    var updatedEntities = new List<(TDbModel Entity, TResult OldData, IReadOnlyList<object> Keys)>();
                    foreach (var entity in entities) {
                        var oldData = MapOrCast<TDbModel, TResult>(Mapper, entity);
                        var entityKeys = typeConversion.GetPrimaryKeyValues(entity, context);
                        Mapper.Map(request.Data, entity);
                        var ctx = new UpdateContext<TRequest, TDbModel, TContext>(request, entity, context, serviceProvider);
                        before?.Invoke(ctx);
                        updatedEntities.Add((entity, oldData, entityKeys)!);
                    }

                    if (updatedEntities.Count > 0)
                        entityMap[index] = updatedEntities;
                }
                catch (Exception ex) {
                    Logger.LogWarning(ex, "Failed to process update request at index {Index}", index);
                    failures.Add((index, request));
                }

                index++;
            }

            if (entityMap.Count > 0) {
                await context.SaveChangesAsync(ct);

                // Execute after hooks and create results based on entity state
                foreach (var (mapIndex, entityList) in entityMap) {
                    try {
                        foreach (var (entity, oldData, entityKeys) in entityList) {
                            var req = requests[mapIndex];
                            var ctx = new UpdateContext<TRequest, TDbModel, TContext>(req, entity, context, serviceProvider);
                            after?.Invoke(ctx);
                            var entry = context.Entry(entity);
                            var newData = MapOrCast<TDbModel, TResult>(Mapper, entity);
                            var resultState = entry.State switch {
                                EntityState.Modified => UpdateResultEnum.Updated,
                                EntityState.Unchanged => UpdateResultEnum.NoChange,
                                var _ => UpdateResultEnum.Failed
                            };

                            successes.Add(ResultFactory.UpdateSuccess(entityKeys, oldData, newData, resultState));
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
            Logger.LogWarning(ex, "Unexpected error during bulk update tracking");
            return (successes, failures.Count > 0 ? failures : requests.Select((r, i) => (i, r)).ToList());
        }

        return (successes, failures);
    }

    private async Task<UpdateResult<TResult>> UpdateIndividual<TRequest, TDbModel, TResult>(
        UpdateRequest<TRequest> request,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
            var keys = request.Keys ?? [];
            if (entities.Count == 0) {
                return ResultFactory.UpdateFailure<TResult>(
                    keys,
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                        .WithMessage($"Entity not found for update.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            if (entities.Count > 1) {
                return ResultFactory.UpdateFailure<TResult>(
                    keys,
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            // For individual retry, just update the first entity
            var entity = entities[0];
            return await UpdateInternal<TRequest, TDbModel, TResult>(entity, request, context, before, after, ct);
        }
        catch (Exception ex) {
            var keys = request.Keys ?? [];
            return ResultFactory.UpdateFailure<TResult>(keys, LogAndReturnApiError(ex, "Individual Update Error", Models.Constants.ApiErrorCodes.InvalidUpdateRequest));
        }
    }

    private async Task<List<TDbModel>> FindEntitiesByRequest<TRequest, TDbModel>(TContext context, UpdateRequest<TRequest> request, CancellationToken ct = default)
        where TDbModel : class
    {
        var entityTypeName = typeof(TDbModel).Name;

        // Use Keys if provided
        if (request.Keys != null && request.Keys.Length > 0) {
            Logger.LogTrace("Searching for {EntityType} using keys: {Keys}", entityTypeName, string.Join(", ", request.Keys));
            var keys = typeConversion.ConvertKeysForFind<TDbModel>(request.Keys, context);
            var entity = await context.Set<TDbModel>().FindAsync(keys, ct);
            if (entity != null) {
                Logger.LogTrace("Found 1 entity for {EntityType} using keys", entityTypeName);
                return [entity];
            }

            Logger.LogTrace("Found 0 entities for {EntityType} using keys", entityTypeName);
            return [];
        }

        // Use Query if provided
        if (request.Query != null) {
            Logger.LogTrace("Searching for {EntityType} with Query", entityTypeName);
            var query = filterService.ApplyWhereClause(context.Set<TDbModel>().AsQueryable(), request.Query);
            var entities = await query.ToListAsync(ct);
            Logger.LogTrace("Found {EntityCount} entities for {EntityType} using identifiers", entities.Count, entityTypeName);
            return entities;
        }

        Logger.LogWarning("No Keys or Query provided for {EntityType} update", entityTypeName);
        return [];
    }

    private async Task<UpdateResult<TResult>> UpdateInternal<TRequest, TDbModel, TResult>(
        TDbModel entity,
        UpdateRequest<TRequest> request,
        TContext context,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? before,
        Action<UpdateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        var keys = typeConversion.GetPrimaryKeyValues(entity, context);
        try {
            var oldData = MapOrCast<TDbModel, TResult>(Mapper, entity);
            Mapper.Map(request.Data, entity);
            var ctx = new UpdateContext<TRequest, TDbModel, TContext>(request, entity, context, serviceProvider);
            before?.Invoke(ctx);
            await context.SaveChangesAsync(ct);
            after?.Invoke(ctx);
            var entry = context.Entry(entity);
            var newData = MapOrCast<TDbModel, TResult>(Mapper, entity);
            var resultState = entry.State switch {
                EntityState.Modified => UpdateResultEnum.Updated,
                EntityState.Unchanged => UpdateResultEnum.NoChange,
                var _ => UpdateResultEnum.Failed
            };

            return ResultFactory.UpdateSuccess(keys, oldData, newData, resultState);
        }
        catch (Exception ex) {
            return ResultFactory.UpdateFailure<TResult>(keys, LogAndReturnApiError(ex, "Update Error", Models.Constants.ApiErrorCodes.InvalidUpdateRequest));
        }
    }
}