using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Api.Services.TypeConversion;
using Lyo.Cache;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Query.Services.PropertyComparison;
using Lyo.Query.Services.WhereClause;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Update;

public class UpsertService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    IPropertyComparisonService comparisonService,
    IWhereClauseService filterService,
    ITypeConversionService typeConversion,
    BulkOperationOptions bulkOptions,
    ICacheService cache,
    ICreateService<TContext> createService,
    IServiceProvider serviceProvider,
    ILogger<UpsertService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), IUpsertService<TContext>
    where TContext : DbContext
{
    public async Task<UpsertResult<TResult>> UpsertAsync<TRequest, TDbModel, TResult>(
        UpsertRequest<TRequest> request,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "upsert";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        using var scope = BeginActionScope("UPSERT", typeof(TRequest), typeof(TDbModel), typeof(TResult));
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
        if (entities.Count == 0) {
            var createResult = await CreateInternal<TRequest, TDbModel, TResult>(request, context, before, beforeCreate, after, afterCreate, ct);
            var result = ResultFactory.UpsertCreated(createResult);
            if (result.Result == UpsertResultEnum.Created)
                await cache.InvalidateQueryCacheAsync<TDbModel>();

            if (result.Result == UpsertResultEnum.Created)
                RecordCrudSuccess(operation, typeof(TDbModel));
            else
                RecordCrudFailure(operation, typeof(TDbModel));

            return result;
        }

        if (entities.Count > 1) {
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.UpsertFailure<TResult>(
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                    .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                    .Build());
        }

        // For single upsert, just update the first entity
        var entity = entities[0];
        var upsertResult = await UpsertInternal<TRequest, TDbModel, TResult>(entity, request, context, before, beforeUpdate, after, afterUpdate, ct);
        if (upsertResult.Result == UpsertResultEnum.Updated)
            await cache.InvalidateQueryCacheAsync<TDbModel>();

        if (upsertResult.Result == UpsertResultEnum.Failed)
            RecordCrudFailure(operation, typeof(TDbModel));
        else
            RecordCrudSuccess(operation, typeof(TDbModel));

        return upsertResult;
    }

    public async Task<UpsertBulkResult<TResult>> UpsertBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<UpsertRequest<TRequest>> requests,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate = null,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "upsert_bulk";
        RecordCrudRequest(operation, typeof(TDbModel), true);
        using var timer = StartCrudTimer(operation, typeof(TDbModel), true);
        var requestList = requests as IReadOnlyList<UpsertRequest<TRequest>> ?? requests.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(requestList, nameof(requests));
        using var scope = BeginActionScope("UPSERT BULK", typeof(TRequest), typeof(TDbModel), typeof(TResult));
        var bulkValidation = BulkListRequestValidator.Validate(new BulkListRequestValidatorInput(requestList.Count, bulkOptions.MaxAmount));
        if (!bulkValidation.IsSuccess) {
            var err = bulkValidation.Errors![0];
            Logger.LogWarning("Bulk upsert size validation failed: {Code} {Message}", err.Code, err.Message);
            throw new LFException(err.Code, err.Message);
        }

        var bulkResult = await TryBulkUpsertAll<TRequest, TDbModel, TResult>(requestList, before, after, beforeCreate, afterCreate, beforeUpdate, afterUpdate, ct);
        if (bulkResult != null) {
            Logger.LogInformation("Bulk upsert completed successfully for {Count} requests", requestList.Count);
            if (bulkResult.CreatedCount + bulkResult.UpdatedCount + bulkResult.NoChangeCount > 0)
                RecordCrudSuccess(operation, typeof(TDbModel), true);

            if (bulkResult.FailedCount > 0)
                RecordCrudFailure(operation, typeof(TDbModel), true);

            RecordCrudResultCount(operation, typeof(TDbModel), bulkResult.CreatedCount + bulkResult.UpdatedCount, true);
            return bulkResult;
        }

        Logger.LogWarning("Bulk upsert failed, falling back to partial retry strategy for {Count} requests", requestList.Count);
        var retryResult = await UpsertWithPartialRetry<TRequest, TDbModel, TResult>(requestList, before, after, beforeCreate, afterCreate, beforeUpdate, afterUpdate, ct);
        if (retryResult.CreatedCount + retryResult.UpdatedCount + retryResult.NoChangeCount > 0)
            RecordCrudSuccess(operation, typeof(TDbModel), true);

        if (retryResult.FailedCount > 0)
            RecordCrudFailure(operation, typeof(TDbModel), true);

        RecordCrudResultCount(operation, typeof(TDbModel), retryResult.CreatedCount + retryResult.UpdatedCount, true);
        return retryResult;
    }

    private static Action<CreateContext<TRequest, TDbModel, TDbCtx>>? AdaptCreateHook<TRequest, TDbModel, TDbCtx>(Action<UpsertContext<TRequest, TDbModel, TDbCtx>>? hook)
        where TDbCtx : DbContext where TDbModel : class
    {
        if (hook == null)
            return null;

        return ctx => hook(new(new() { NewData = ctx.Request }, ctx.Entity, ctx.DbContext, ctx.Services));
    }

    private async Task<UpsertBulkResult<TResult>?> TryBulkUpsertAll<TRequest, TDbModel, TResult>(
        IReadOnlyList<UpsertRequest<TRequest>> requests,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate,
        CancellationToken ct)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var results = new List<UpsertResult<TResult>>();
            var toCreate = new List<TRequest>();
            var toUpdate = new List<(TDbModel Entity, TResult OldValue, UpsertRequest<TRequest> Request)>();
            foreach (var request in requests) {
                // Check cancellation
                ct.ThrowIfCancellationRequested();
                var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
                if (entities.Count == 0) {
                    toCreate.Add(request.NewData);
                    results.Add(null!); // Placeholder
                    continue;
                }

                if (entities.Count > 1) {
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.UpsertFailure<TResult>(err));
                    continue;
                }

                // Update all matching entities if AllowMultiple is true
                foreach (var entity in entities) {
                    var diff = comparisonService.GetPropertyDifferences(entity, request.NewData);
                    foreach (var prop in request.IgnoredCompareProperties)
                        diff.Remove(prop);

                    if (diff.Count == 0)
                        results.Add(ResultFactory.UpsertNoChange<TResult>());
                    else {
                        var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
                        Mapper.Map(request.NewData, entity);
                        var ctx = new UpsertContext<TRequest, TDbModel, TContext>(request, entity, context, serviceProvider);
                        before?.Invoke(ctx);
                        beforeUpdate?.Invoke(ctx);
                        toUpdate.Add((entity, old, request));
                        results.Add(null!); // Placeholder
                    }
                }
            }

            // Bulk create
            List<CreateResult<TResult>>? createResults = null;
            if (toCreate.Count > 0) {
                var createBulkResult = await createService.CreateBulkAsync<TRequest, TDbModel, TResult>(toCreate, AdaptCreateHook(beforeCreate), AdaptCreateHook(afterCreate), ct);

                createResults = createBulkResult.Results.ToList();
            }

            // Bulk update
            if (toUpdate.Count > 0) {
                await context.SaveChangesAsync(ct);
                foreach (var (entity, _, req) in toUpdate) {
                    var ctx = new UpsertContext<TRequest, TDbModel, TContext>(req, entity, context, serviceProvider);
                    after?.Invoke(ctx);
                    afterUpdate?.Invoke(ctx);
                }
            }

            // Replace placeholders with actual results
            int createIndex = 0, updateIndex = 0;
            for (var i = 0; i < results.Count; i++) {
                if (results[i] == null) {
                    if (createIndex < (createResults?.Count ?? 0))
                        results[i] = ResultFactory.UpsertCreated(createResults![createIndex++]);
                    else if (updateIndex < toUpdate.Count) {
                        var (entity, oldValue, _) = toUpdate[updateIndex++];
                        var updatedData = MapOrCast<TDbModel, TResult>(Mapper, entity);
                        results[i] = ResultFactory.UpsertUpdated(oldValue, updatedData);
                    }
                }
            }

            var created = results.Count(r => r.Result == UpsertResultEnum.Created);
            var updated = results.Count(r => r.Result == UpsertResultEnum.Updated);
            var noChange = results.Count(r => r.Result == UpsertResultEnum.NoChange);
            var failed = results.Count(r => r.Result == UpsertResultEnum.Failed);
            var bulkResult = new UpsertBulkResult<TResult>(results, created, updated, noChange, failed);
            if (created > 0 || updated > 0)
                await cache.InvalidateQueryCacheAsync<TDbModel>();

            return bulkResult;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Bulk upsert failed, will attempt partial retry");
            return null;
        }
    }

    private async Task<UpsertBulkResult<TResult>> UpsertWithPartialRetry<TRequest, TDbModel, TResult>(
        IReadOnlyList<UpsertRequest<TRequest>> requests,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate,
        CancellationToken ct)
        where TDbModel : class
    {
        var results = new List<UpsertResult<TResult>>();
        var failed = new List<(int Index, UpsertRequest<TRequest> Request)>();
        int created = 0, updated = 0, noChange = 0, failedCount = 0;
        var (successResults, failedRequests) = await TryBulkUpsertWithTracking<TRequest, TDbModel, TResult>(
            requests, before, after, beforeCreate, afterCreate, beforeUpdate, afterUpdate, ct);

        results.AddRange(successResults);
        created += successResults.Count(r => r.Result == UpsertResultEnum.Created);
        updated += successResults.Count(r => r.Result == UpsertResultEnum.Updated);
        noChange += successResults.Count(r => r.Result == UpsertResultEnum.NoChange);
        failed.AddRange(failedRequests);
        failedCount += failedRequests.Count;
        if (failed.Count > 0) {
            Logger.LogWarning("Retrying {FailedCount} failed items individually", failed.Count);
            foreach (var (index, request) in failed) {
                // Check cancellation before retrying individual items
                ct.ThrowIfCancellationRequested();
                var individualResult = await UpsertIndividual<TRequest, TDbModel, TResult>(request, before, after, beforeCreate, afterCreate, beforeUpdate, afterUpdate, ct);
                if (index < results.Count)
                    results.Insert(index, individualResult);
                else
                    results.Add(individualResult);

                switch (individualResult.Result) {
                    case UpsertResultEnum.Created:
                        created++;
                        break;
                    case UpsertResultEnum.Updated:
                        updated++;
                        break;
                    case UpsertResultEnum.NoChange:
                        noChange++;
                        break;
                    case UpsertResultEnum.Failed:
                        failedCount++;
                        break;
                }
            }
        }

        if (created > 0 || updated > 0)
            await cache.InvalidateQueryCacheAsync<TDbModel>();

        return new(results, created, updated, noChange, failedCount);
    }

    private async Task<(List<UpsertResult<TResult>> Successes, List<(int Index, UpsertRequest<TRequest> Request)> Failures)> TryBulkUpsertWithTracking<TRequest, TDbModel, TResult>(
        IReadOnlyList<UpsertRequest<TRequest>> requests,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate,
        CancellationToken ct)
        where TDbModel : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var toCreateMap = new Dictionary<int, TRequest>();
        var toUpdateMap = new Dictionary<int, List<(TDbModel Entity, TResult OldValue)>>();
        var successes = new List<UpsertResult<TResult>>();
        var failures = new List<(int Index, UpsertRequest<TRequest> Request)>();
        try {
            var index = 0;
            foreach (var request in requests) {
                try {
                    // Check cancellation
                    ct.ThrowIfCancellationRequested();
                    var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
                    if (entities.Count == 0) {
                        toCreateMap[index] = request.NewData;
                        index++;
                        continue;
                    }

                    if (entities.Count > 1) {
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                            .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.UpsertFailure<TResult>(err));
                        index++;
                        continue;
                    }

                    // Update all matching entities
                    var updatedEntities = new List<(TDbModel Entity, TResult OldValue)>();
                    foreach (var entity in entities) {
                        var diff = comparisonService.GetPropertyDifferences(entity, request.NewData);
                        foreach (var prop in request.IgnoredCompareProperties)
                            diff.Remove(prop);

                        if (diff.Count == 0)
                            successes.Add(ResultFactory.UpsertNoChange<TResult>());
                        else {
                            var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
                            Mapper.Map(request.NewData, entity);
                            var ctx = new UpsertContext<TRequest, TDbModel, TContext>(request, entity, context, serviceProvider);
                            before?.Invoke(ctx);
                            beforeUpdate?.Invoke(ctx);
                            updatedEntities.Add((entity, old));
                        }
                    }

                    if (updatedEntities.Count > 0)
                        toUpdateMap[index] = updatedEntities;
                }
                catch (Exception ex) {
                    Logger.LogWarning(ex, "Failed to process upsert request at index {Index}", index);
                    failures.Add((index, request));
                }

                index++;
            }

            // Bulk create
            if (toCreateMap.Count > 0) {
                var createRequests = toCreateMap.Values.ToList();
                var createBulkResult = await createService.CreateBulkAsync<TRequest, TDbModel, TResult>(
                    createRequests, AdaptCreateHook(beforeCreate), AdaptCreateHook(afterCreate), ct);

                foreach (var createResult in createBulkResult.Results)
                    successes.Add(ResultFactory.UpsertCreated(createResult));
            }

            // Bulk update
            if (toUpdateMap.Count > 0) {
                await context.SaveChangesAsync(ct);
                foreach (var (mapIndex, entityList) in toUpdateMap) {
                    try {
                        var req = requests[mapIndex];
                        foreach (var (entity, oldValue) in entityList) {
                            var ctx = new UpsertContext<TRequest, TDbModel, TContext>(req, entity, context, serviceProvider);
                            after?.Invoke(ctx);
                            afterUpdate?.Invoke(ctx);
                            var updatedData = MapOrCast<TDbModel, TResult>(Mapper, entity);
                            successes.Add(ResultFactory.UpsertUpdated(oldValue, updatedData));
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
            Logger.LogWarning(ex, "Unexpected error during bulk upsert tracking");
            return (successes, failures.Count > 0 ? failures : requests.Select((r, i) => (i, r)).ToList());
        }

        return (successes, failures);
    }

    private async Task<UpsertResult<TResult>> UpsertIndividual<TRequest, TDbModel, TResult>(
        UpsertRequest<TRequest> request,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate,
        CancellationToken ct)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var entities = await FindEntitiesByRequest<TRequest, TDbModel>(context, request, ct);
            if (entities.Count == 0) {
                var createResult = await CreateInternal<TRequest, TDbModel, TResult>(request, context, before, beforeCreate, after, afterCreate, ct);
                return ResultFactory.UpsertCreated(createResult);
            }

            if (entities.Count > 1) {
                return ResultFactory.UpsertFailure<TResult>(
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage($"Multiple entities ({entities.Count}) found.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            // For individual retry, just upsert the first entity
            var entity = entities[0];
            return await UpsertInternal<TRequest, TDbModel, TResult>(entity, request, context, before, beforeUpdate, after, afterUpdate, ct);
        }
        catch (Exception ex) {
            return ResultFactory.UpsertFailure<TResult>(LogAndReturnApiError(ex, "Individual Upsert Error", Models.Constants.ApiErrorCodes.SqlException));
        }
    }

    private async Task<List<TDbModel>> FindEntitiesByRequest<TRequest, TDbModel>(TContext context, UpsertRequest<TRequest> request, CancellationToken ct)
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
            return new();
        }

        // Use Query if provided
        if (request.Query != null) {
            Logger.LogTrace("Searching for {EntityType} with Query", entityTypeName);
            var query = filterService.ApplyWhereClause(context.Set<TDbModel>().AsQueryable(), request.Query);
            var entities = await query.ToListAsync(ct);
            Logger.LogTrace("Found {EntityCount} entities for {EntityType} using identifiers", entities.Count, entityTypeName);
            return entities;
        }

        Logger.LogWarning("No Keys or Query provided for {EntityType} upsert", entityTypeName);
        return [];
    }

    private async Task<UpsertResult<TResult>> UpsertInternal<TRequest, TDbModel, TResult>(
        TDbModel entity,
        UpsertRequest<TRequest> request,
        TContext context,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeUpdate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterUpdate,
        CancellationToken ct)
        where TDbModel : class
    {
        var diff = comparisonService.GetPropertyDifferences(entity, request.NewData);
        foreach (var prop in request.IgnoredCompareProperties)
            diff.Remove(prop);

        if (diff.Count == 0)
            return ResultFactory.UpsertNoChange<TResult>();

        try {
            var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
            Mapper.Map(request.NewData, entity);
            var ctx = new UpsertContext<TRequest, TDbModel, TContext>(request, entity, context, serviceProvider);
            before?.Invoke(ctx);
            beforeUpdate?.Invoke(ctx);
            await context.SaveChangesAsync(ct);
            after?.Invoke(ctx);
            afterUpdate?.Invoke(ctx);
            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
            return ResultFactory.UpsertUpdated(old, result);
        }
        catch (Exception ex) {
            return ResultFactory.UpsertFailure<TResult>(LogAndReturnApiError(ex, "Upsert Error"));
        }
    }

    private async Task<CreateResult<TResult>> CreateInternal<TRequest, TDbModel, TResult>(
        UpsertRequest<TRequest> upsertRequest,
        TContext context,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? before,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? beforeCreate,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? after,
        Action<UpsertContext<TRequest, TDbModel, TContext>>? afterCreate,
        CancellationToken ct)
        where TDbModel : class
    {
        try {
            var entity = MapOrCast<TRequest, TDbModel>(Mapper, upsertRequest.NewData!);
            var ctx = new UpsertContext<TRequest, TDbModel, TContext>(upsertRequest, entity, context, serviceProvider);
            before?.Invoke(ctx);
            beforeCreate?.Invoke(ctx);
            context.Set<TDbModel>().Add(entity);
            await context.SaveChangesAsync(ct);
            after?.Invoke(ctx);
            afterCreate?.Invoke(ctx);
            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
            return ResultFactory.CreateSuccess(result);
        }
        catch (Exception ex) {
            return ResultFactory.CreateFailure<TResult>(LogAndReturnApiError(ex, "Create Error", Models.Constants.ApiErrorCodes.InvalidCreateRequest));
        }
    }
}