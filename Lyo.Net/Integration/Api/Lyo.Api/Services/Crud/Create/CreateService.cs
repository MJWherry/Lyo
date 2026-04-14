using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Validation;
using Lyo.Cache;
using Lyo.Exceptions;
using Lyo.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Api.Services.Crud.Create;

public class CreateService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    BulkOperationOptions bulkOptions,
    ICacheService cache,
    IServiceProvider serviceProvider,
    ILogger<CreateService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), ICreateService<TContext>
    where TContext : DbContext
{
    public async Task<CreateResult<TResult>> CreateAsync<TRequest, TDbModel, TResult>(
        TRequest request,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "create";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        using var scope = BeginActionScope("CREATE", typeof(TRequest), typeof(TDbModel), typeof(TResult));
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var result = await CreateInternal<TRequest, TDbModel, TResult>(request, context, before, after, ct);
        if (result.IsSuccess)
            await cache.InvalidateQueryCacheAsync<TDbModel>();

        if (result.IsSuccess)
            RecordCrudSuccess(operation, typeof(TDbModel));
        else
            RecordCrudFailure(operation, typeof(TDbModel));

        return result;
    }

    public async Task<CreateBulkResult<TResult>> CreateBulkAsync<TRequest, TDbModel, TResult>(
        IEnumerable<TRequest> requests,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before = null,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "create_bulk";
        RecordCrudRequest(operation, typeof(TDbModel), true);
        using var timer = StartCrudTimer(operation, typeof(TDbModel), true);
        var requestList = requests as TRequest[] ?? requests.ToArray();
        ArgumentHelpers.ThrowIfNullOrEmpty(requestList, nameof(requests));
        using var scope = BeginActionScope("CREATE BULK", typeof(TRequest), typeof(TDbModel), typeof(TResult));
        var bulkValidation = BulkListRequestValidator.Validate(new(requestList.Length, bulkOptions.MaxAmount));
        if (!bulkValidation.IsSuccess) {
            var err = bulkValidation.Errors![0];
            Logger.LogWarning("Bulk create size validation failed: {Code} {Message}", err.Code, err.Message);
            throw new LFException(err.Code, err.Message);
        }

        var bulkResult = await TryBulkCreateAll<TRequest, TDbModel, TResult>(requestList, before, after, ct);
        if (bulkResult != null) {
            Logger.LogInformation("Bulk create completed successfully for {Count} requests", requestList.Length);
            if (bulkResult.CreatedCount > 0)
                RecordCrudSuccess(operation, typeof(TDbModel), true);

            if (bulkResult.FailedCount > 0)
                RecordCrudFailure(operation, typeof(TDbModel), true);

            RecordCrudResultCount(operation, typeof(TDbModel), bulkResult.CreatedCount, true);
            return bulkResult;
        }

        Logger.LogWarning("Bulk create failed, falling back to partial retry strategy for {Count} requests", requestList.Length);
        var retryResult = await CreateWithPartialRetry<TRequest, TDbModel, TResult>(requestList, before, after, ct);
        if (retryResult.CreatedCount > 0)
            RecordCrudSuccess(operation, typeof(TDbModel), true);

        if (retryResult.FailedCount > 0)
            RecordCrudFailure(operation, typeof(TDbModel), true);

        RecordCrudResultCount(operation, typeof(TDbModel), retryResult.CreatedCount, true);
        return retryResult;
    }

    private async Task<CreateBulkResult<TResult>?> TryBulkCreateAll<TRequest, TDbModel, TResult>(
        IReadOnlyList<TRequest> requests,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var entities = new List<TDbModel>(requests.Count);
            foreach (var req in requests) {
                var entity = MapOrCast<TRequest, TDbModel>(Mapper, req!);
                var ctx = new CreateContext<TRequest, TDbModel, TContext>(req!, entity, context, serviceProvider);
                before?.Invoke(ctx);
                context.Set<TDbModel>().Add(entity);
                entities.Add(entity);
            }

            await context.SaveChangesAsync(ct);
            var results = new List<CreateResult<TResult>>(requests.Count);
            foreach (var (req, entity) in requests.Zip(entities)) {
                var ctx = new CreateContext<TRequest, TDbModel, TContext>(req!, entity, context, serviceProvider);
                after?.Invoke(ctx);
                var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
                results.Add(ResultFactory.CreateSuccess(result));
            }

            var bulkResult = new CreateBulkResult<TResult>(results, results.Count, 0);
            await cache.InvalidateQueryCacheAsync<TDbModel>();
            return bulkResult;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Bulk create failed, will attempt partial retry");
            return null;
        }
    }

    private async Task<CreateBulkResult<TResult>> CreateWithPartialRetry<TRequest, TDbModel, TResult>(
        IReadOnlyList<TRequest> requests,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        var results = new List<CreateResult<TResult>>();
        var failed = new List<(int Index, TRequest Request)>();
        int successCount = 0, failureCount = 0;
        var (successResults, failedRequests) = await TryBulkCreateWithTracking<TRequest, TDbModel, TResult>(requests, before, after, ct);
        results.AddRange(successResults);
        successCount += successResults.Count;
        failed.AddRange(failedRequests);
        failureCount += failedRequests.Count;
        if (failed.Count > 0) {
            Logger.LogWarning("Retrying {FailedCount} failed items individually", failed.Count);
            foreach (var (index, request) in failed) {
                var individualResult = await CreateIndividual<TRequest, TDbModel, TResult>(request, before, after, ct);
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
            await cache.InvalidateQueryCacheAsync<TDbModel>();

        return new(results, successCount, failureCount);
    }

    private async Task<(List<CreateResult<TResult>> Successes, List<(int Index, TRequest Request)> Failures)> TryBulkCreateWithTracking<TRequest, TDbModel, TResult>(
        IReadOnlyList<TRequest> requests,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync(ct);
        var entityMap = new Dictionary<int, TDbModel>();
        var successes = new List<CreateResult<TResult>>();
        var failures = new List<(int Index, TRequest Request)>();
        try {
            var index = 0;
            foreach (var req in requests) {
                try {
                    var entity = MapOrCast<TRequest, TDbModel>(Mapper, req!);
                    var ctx = new CreateContext<TRequest, TDbModel, TContext>(req!, entity, context, serviceProvider);
                    before?.Invoke(ctx);
                    context.Set<TDbModel>().Add(entity);
                    entityMap[index] = entity;
                }
                catch (Exception ex) {
                    Logger.LogWarning(ex, "Failed to map request at index {Index}", index);
                    failures.Add((index, req));
                }

                index++;
            }

            if (entityMap.Count > 0) {
                await context.SaveChangesAsync(ct);
                foreach (var (mapIndex, entity) in entityMap) {
                    try {
                        var req = requests[mapIndex];
                        var ctx = new CreateContext<TRequest, TDbModel, TContext>(req!, entity, context, serviceProvider);
                        after?.Invoke(ctx);
                        var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
                        successes.Add(ResultFactory.CreateSuccess(result));
                    }
                    catch (Exception ex) {
                        Logger.LogWarning(ex, "Failed to process after hook at index {Index}", mapIndex);
                        failures.Add((mapIndex, requests[mapIndex]));
                    }
                }
            }
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Unexpected error during bulk create tracking");
            return (successes, failures.Count > 0 ? failures : requests.Select((r, i) => (i, r)).ToList());
        }

        return (successes, failures);
    }

    private async Task<CreateResult<TResult>> CreateIndividual<TRequest, TDbModel, TResult>(
        TRequest request,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct);
            var entity = MapOrCast<TRequest, TDbModel>(Mapper, request!);
            var ctx = new CreateContext<TRequest, TDbModel, TContext>(request!, entity, context, serviceProvider);
            before?.Invoke(ctx);
            context.Set<TDbModel>().Add(entity);
            await context.SaveChangesAsync(ct);
            after?.Invoke(ctx);
            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
            return ResultFactory.CreateSuccess(result);
        }
        catch (Exception ex) {
            return ResultFactory.CreateFailure<TResult>(LogAndReturnApiError(ex, "Individual Create Error", Models.Constants.ApiErrorCodes.InvalidCreateRequest));
        }
    }

    private async Task<CreateResult<TResult>> CreateInternal<TRequest, TDbModel, TResult>(
        TRequest request,
        TContext context,
        Action<CreateContext<TRequest, TDbModel, TContext>>? before,
        Action<CreateContext<TRequest, TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            var entity = MapOrCast<TRequest, TDbModel>(Mapper, request!);
            var ctx = new CreateContext<TRequest, TDbModel, TContext>(request!, entity, context, serviceProvider);
            before?.Invoke(ctx);
            context.Set<TDbModel>().Add(entity);
            await context.SaveChangesAsync(ct);
            after?.Invoke(ctx);
            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
            return ResultFactory.CreateSuccess(result);
        }
        catch (Exception ex) {
            return ResultFactory.CreateFailure<TResult>(LogAndReturnApiError(ex, "Create Error", Models.Constants.ApiErrorCodes.InvalidCreateRequest));
        }
    }
}