using System.Reflection;
using Lyo.Api.ApiEndpoint.Config;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
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

namespace Lyo.Api.Services.Crud.Update;

public class PatchService<TContext>(
    IDbContextFactory<TContext> contextFactory,
    ILyoMapper mapper,
    IWhereClauseService filterService,
    ITypeConversionService typeConversion,
    BulkOperationOptions bulkOptions,
    CacheOptions cacheOptions,
    ICacheService cache,
    IServiceProvider serviceProvider,
    ILogger<PatchService<TContext>>? logger = null,
    IMetrics? metrics = null)
    : BaseService<TContext>(contextFactory, mapper, logger, metrics), IPatchService<TContext>
    where TContext : DbContext
{
    private const string PropertyCacheKeyPrefix = "PropertyCache_";

    public async Task<PatchResult<TResult>> PatchAsync<TDbModel, TResult>(
        PatchRequest request,
        Action<PatchContext<TDbModel, TContext>>? before = null,
        Action<PatchContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "patch";
        RecordCrudRequest(operation, typeof(TDbModel));
        using var timer = StartCrudTimer(operation, typeof(TDbModel));
        ArgumentHelpers.ThrowIfNull(request, nameof(request));
        var entityTypeName = typeof(TDbModel).Name;
        var responseTypeName = typeof(TResult).Name;
        using var scope = BeginActionScope("PATCH", typeof(PatchRequest), typeof(TDbModel), typeof(TResult));
        Logger.LogInformation("Starting patch operation for entity {EntityType} to response {ResponseType}", entityTypeName, responseTypeName);
        await using var context = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        try {
            var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct).ConfigureAwait(false);
            if (entities.Count == 0) {
                Logger.LogWarning("Entity {EntityType} not found", entityTypeName);
                RecordCrudFailure(operation, typeof(TDbModel));
                return ResultFactory.PatchFailure<TResult>(CreateNotFoundError<TDbModel>(request.Keys?.FirstOrDefault() ?? []));
            }

            if (entities.Count > 1 && !request.AllowMultiple) {
                RecordCrudFailure(operation, typeof(TDbModel));
                return ResultFactory.PatchFailure<TResult>(
                    LogAndReturnApiError(
                        $"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}",
                        Models.Constants.ApiErrorCodes.InvalidOperation));
            }

            // For single entity patch, just patch the first one
            var entity = entities[0];
            Logger.LogDebug("Found entity {EntityType} for patch operation", entityTypeName);
            var result = await PatchInternal<TDbModel, TResult>(entity, request, context, before, after, ct).ConfigureAwait(false);
            if (result.Result is PatchResultEnum.NoChange or PatchResultEnum.Failed) {
                if (result.Result == PatchResultEnum.Failed)
                    RecordCrudFailure(operation, typeof(TDbModel));
                else
                    RecordCrudSuccess(operation, typeof(TDbModel));

                return result;
            }

            await QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(
                    cache, cacheOptions, typeof(TDbModel), [typeConversion.GetPrimaryKeyValues(entity, context)], ct)
                .ConfigureAwait(false);
            var keys = typeConversion.GetPrimaryKeyValues(entity, context);
            cache.Set(
                $"entity:{typeof(TDbModel).Name}:keys={string.Join("|", keys.Order().Select(i => i?.ToString()))}", result.NewData!,
                ["entities", $"entity:{typeof(TDbModel).Name.ToLowerInvariant()}"]);

            RecordCrudSuccess(operation, typeof(TDbModel));
            return result;
        }
        catch (LFException ex) {
            Logger.LogWarning(ex, "Business logic error during patch operation for {EntityType}: {ErrorCode}", entityTypeName, ex.ErrorCode);
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.PatchFailure<TResult>(LogAndReturnApiError(ex, "Patch Error", Models.Constants.ApiErrorCodes.InvalidPatchRequest));
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Unexpected error during patch operation for {EntityType}", entityTypeName);
            RecordCrudFailure(operation, typeof(TDbModel));
            return ResultFactory.PatchFailure<TResult>(LogAndReturnApiError(ex, "Patch Error"));
        }
    }

    public async Task<PatchBulkResult<TResult>> PatchBulkAsync<TDbModel, TResult>(
        IEnumerable<PatchRequest> requests,
        Action<PatchContext<TDbModel, TContext>>? before = null,
        Action<PatchContext<TDbModel, TContext>>? after = null,
        CancellationToken ct = default)
        where TDbModel : class
    {
        const string operation = "patch_bulk";
        RecordCrudRequest(operation, typeof(TDbModel), true);
        using var timer = StartCrudTimer(operation, typeof(TDbModel), true);
        var requestList = requests as IReadOnlyList<PatchRequest> ?? requests.ToList();
        ArgumentHelpers.ThrowIfNullOrEmpty(requestList, nameof(requests));
        var entityTypeName = typeof(TDbModel).Name;
        var responseTypeName = typeof(TResult).Name;
        using var scope = BeginActionScope("PATCH BULK", typeof(PatchRequest), typeof(TDbModel), typeof(TResult));
        Logger.LogInformation("Starting bulk patch operation for entity {EntityType} to response {ResponseType}", entityTypeName, responseTypeName);
        var bulkValidation = BulkListRequestValidator.Validate(new BulkListRequestValidatorInput(requestList.Count, bulkOptions.MaxAmount));
        if (!bulkValidation.IsSuccess) {
            var err = bulkValidation.Errors![0];
            Logger.LogWarning("Bulk patch size validation failed for {EntityType}: {Code} {Message}", entityTypeName, err.Code, err.Message);
            throw new LFException(err.Code, err.Message);
        }

        var bulkResult = await TryBulkPatchAll<TDbModel, TResult>(requestList, before, after, ct).ConfigureAwait(false);
        if (bulkResult != null) {
            Logger.LogInformation("Bulk patch completed successfully for {Count} requests", requestList.Count);
            if (bulkResult.UpdatedCount + bulkResult.NoChangeCount > 0)
                RecordCrudSuccess(operation, typeof(TDbModel), true);

            if (bulkResult.FailedCount > 0)
                RecordCrudFailure(operation, typeof(TDbModel), true);

            RecordCrudResultCount(operation, typeof(TDbModel), bulkResult.UpdatedCount, true);
            return bulkResult;
        }

        Logger.LogWarning("Bulk patch failed, falling back to partial retry strategy for {Count} requests", requestList.Count);
        var retryResult = await PatchWithPartialRetry<TDbModel, TResult>(requestList, before, after, ct).ConfigureAwait(false);
        if (retryResult.UpdatedCount + retryResult.NoChangeCount > 0)
            RecordCrudSuccess(operation, typeof(TDbModel), true);

        if (retryResult.FailedCount > 0)
            RecordCrudFailure(operation, typeof(TDbModel), true);

        RecordCrudResultCount(operation, typeof(TDbModel), retryResult.UpdatedCount, true);
        return retryResult;
    }

    private async Task<PatchBulkResult<TResult>?> TryBulkPatchAll<TDbModel, TResult>(
        IReadOnlyList<PatchRequest> requests,
        Action<PatchContext<TDbModel, TContext>>? before,
        Action<PatchContext<TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var results = new List<PatchResult<TResult>>();
            var modifiedEntityResultPairs = new List<(TDbModel Entity, PatchResult<TResult> Result, PatchRequest Request)>();
            foreach (var request in requests) {
                var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct).ConfigureAwait(false);
                if (entities.Count == 0) {
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                        .WithMessage($"Entity not found for patch.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.PatchFailure<TResult>(err));
                    continue;
                }

                if (entities.Count > 1 && !request.AllowMultiple) {
                    var err = LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage(
                            $"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build();

                    results.Add(ResultFactory.PatchFailure<TResult>(err));
                    continue;
                }

                // Patch all matching entities if AllowMultiple is true
                foreach (var entity in entities) {
                    var patchResult = PatchEntityProperties<TDbModel, TResult>(entity, request);
                    results.Add(patchResult);
                    if (patchResult.Result == PatchResultEnum.Updated)
                        modifiedEntityResultPairs.Add((entity, patchResult, request));
                }
            }

            // Execute before hooks
            foreach (var (entity, _, req) in modifiedEntityResultPairs) {
                var ctx = new PatchContext<TDbModel, TContext>(req, entity, context, serviceProvider);
                before?.Invoke(ctx);
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            // Execute after hooks
            foreach (var (entity, _, req) in modifiedEntityResultPairs) {
                var ctx = new PatchContext<TDbModel, TContext>(req, entity, context, serviceProvider);
                after?.Invoke(ctx);
            }

            // Re-map entities to results so NewData reflects before/after callback changes
            var modifiedIdx = 0;
            for (var i = 0; i < results.Count && modifiedIdx < modifiedEntityResultPairs.Count; i++) {
                if (results[i].Result == PatchResultEnum.Updated) {
                    var (entity, result, _) = modifiedEntityResultPairs[modifiedIdx++];
                    results[i] = ResultFactory.PatchSuccess(result.OldData!, MapOrCast<TDbModel, TResult>(Mapper, entity), result.UpdatedProperties);
                }
            }

            var updated = results.Count(r => r.Result == PatchResultEnum.Updated);
            var noChange = results.Count(r => r.Result == PatchResultEnum.NoChange);
            var failed = results.Count(r => r.Result == PatchResultEnum.Failed);
            var bulkResult = new PatchBulkResult<TResult>(results, updated, noChange, failed);
            if (updated > 0) {
                var keySets = modifiedEntityResultPairs.ConvertAll(p => typeConversion.GetPrimaryKeyValues(p.Entity, context));
                await QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, cacheOptions, typeof(TDbModel), keySets, ct)
                    .ConfigureAwait(false);
            }

            return bulkResult;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Bulk patch failed, will attempt partial retry");
            return null;
        }
    }

    private async Task<PatchBulkResult<TResult>> PatchWithPartialRetry<TDbModel, TResult>(
        IReadOnlyList<PatchRequest> requests,
        Action<PatchContext<TDbModel, TContext>>? before,
        Action<PatchContext<TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        var results = new List<PatchResult<TResult>>();
        var failed = new List<(int Index, PatchRequest Request)>();
        int updated = 0, noChange = 0, failedCount = 0;
        var (successResults, failedRequests) = await TryBulkPatchWithTracking<TDbModel, TResult>(requests, before, after, ct).ConfigureAwait(false);
        results.AddRange(successResults);
        updated += successResults.Count(r => r.Result == PatchResultEnum.Updated);
        noChange += successResults.Count(r => r.Result == PatchResultEnum.NoChange);
        failed.AddRange(failedRequests);
        failedCount += failedRequests.Count;
        if (failed.Count > 0) {
            Logger.LogWarning("Retrying {FailedCount} failed items individually", failed.Count);
            foreach (var (index, request) in failed) {
                var individualResult = await PatchIndividual<TDbModel, TResult>(request, before, after, ct).ConfigureAwait(false);
                if (index < results.Count)
                    results.Insert(index, individualResult);
                else
                    results.Add(individualResult);

                switch (individualResult.Result) {
                    case PatchResultEnum.Updated:
                        updated++;
                        break;
                    case PatchResultEnum.NoChange:
                        noChange++;
                        break;
                    case PatchResultEnum.Failed:
                        failedCount++;
                        break;
                }
            }
        }

        // Partial retry mixes success shapes; use broad type invalidation for correctness.
        if (updated > 0)
            await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<TDbModel>(cache, ct).ConfigureAwait(false);

        return new(results, updated, noChange, failedCount);
    }

    private async Task<(List<PatchResult<TResult>> Successes, List<(int Index, PatchRequest Request)> Failures)> TryBulkPatchWithTracking<TDbModel, TResult>(
        IReadOnlyList<PatchRequest> requests,
        Action<PatchContext<TDbModel, TContext>>? before,
        Action<PatchContext<TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        await using var context = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entityMap = new Dictionary<int, List<TDbModel>>();
        var successes = new List<PatchResult<TResult>>();
        var failures = new List<(int Index, PatchRequest Request)>();
        try {
            var index = 0;
            foreach (var request in requests) {
                try {
                    var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct).ConfigureAwait(false);
                    if (entities.Count == 0) {
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                            .WithMessage($"Entity not found for patch.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.PatchFailure<TResult>(err));
                        index++;
                        continue;
                    }

                    if (entities.Count > 1 && !request.AllowMultiple) {
                        var err = LyoProblemDetailsBuilder.CreateWithActivity()
                            .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                            .WithMessage(
                                $"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                            .Build();

                        successes.Add(ResultFactory.PatchFailure<TResult>(err));
                        index++;
                        continue;
                    }

                    // Process all matching entities
                    var patchedEntities = new List<TDbModel>();
                    foreach (var entity in entities) {
                        var patchResult = PatchEntityProperties<TDbModel, TResult>(entity, request);
                        if (patchResult != null)
                            patchedEntities.Add(entity);
                    }

                    if (patchedEntities.Count > 0)
                        entityMap[index] = patchedEntities;
                }
                catch (Exception ex) {
                    Logger.LogWarning(ex, "Failed to process patch request at index {Index}", index);
                    failures.Add((index, request));
                }

                index++;
            }

            if (entityMap.Count > 0) {
                // Execute before hooks
                foreach (var (mapIndex, entities) in entityMap) {
                    var req = requests[mapIndex];
                    foreach (var entity in entities) {
                        var ctx = new PatchContext<TDbModel, TContext>(req, entity, context, serviceProvider);
                        before?.Invoke(ctx);
                    }
                }

                await context.SaveChangesAsync(ct).ConfigureAwait(false);

                // Execute after hooks and create results
                foreach (var (mapIndex, entities) in entityMap) {
                    try {
                        var req = requests[mapIndex];
                        foreach (var entity in entities) {
                            var ctx = new PatchContext<TDbModel, TContext>(req, entity, context, serviceProvider);
                            after?.Invoke(ctx);
                            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
                            successes.Add(ResultFactory.PatchSuccess(result, result, new Dictionary<string, object?>())); //todo!!!!
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
            Logger.LogWarning(ex, "Unexpected error during bulk patch tracking");
            return (successes, failures.Count > 0 ? failures : requests.Select((r, i) => (i, r)).ToList());
        }

        return (successes, failures);
    }

    private async Task<PatchResult<TResult>> PatchIndividual<TDbModel, TResult>(
        PatchRequest request,
        Action<PatchContext<TDbModel, TContext>>? before,
        Action<PatchContext<TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        try {
            await using var context = await ContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var entities = await FindEntitiesByRequest<TDbModel>(context, request, ct).ConfigureAwait(false);
            if (entities.Count == 0) {
                return ResultFactory.PatchFailure<TResult>(
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.NotFound)
                        .WithMessage($"Entity not found for patch.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            if (entities.Count > 1 && !request.AllowMultiple) {
                return ResultFactory.PatchFailure<TResult>(
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidOperation)
                        .WithMessage(
                            $"Multiple entities ({entities.Count}) found but AllowMultiple is false.{Environment.NewLine}DatabaseType={typeof(TDbModel).FullName}")
                        .Build());
            }

            // For individual retry, just patch the first entity
            var entity = entities[0];
            return await PatchInternal<TDbModel, TResult>(entity, request, context, before, after, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            return ResultFactory.PatchFailure<TResult>(LogAndReturnApiError(ex, "Individual Patch Error", Models.Constants.ApiErrorCodes.InvalidPatchRequest));
        }
    }

    private async Task<List<TDbModel>> FindEntitiesByRequest<TDbModel>(TContext context, PatchRequest request, CancellationToken ct = default)
        where TDbModel : class
    {
        var entityTypeName = typeof(TDbModel).Name;

        // Use Keys if provided
        if (request.Keys != null && request.Keys.Count > 0) {
            Logger.LogTrace("Searching for {EntityType} using {KeyCount} key sets", entityTypeName, request.Keys.Count);
            var entities = new List<TDbModel>();
            foreach (var keySet in request.Keys) {
                var typedKeys = typeConversion.ConvertKeysForFind<TDbModel>(keySet, context);
                var entity = await context.Set<TDbModel>().FindAsync(typedKeys, ct).ConfigureAwait(false);
                if (entity != null)
                    entities.Add(entity);
            }

            Logger.LogTrace("Found {EntityCount} entities for {EntityType} using keys", entities.Count, entityTypeName);
            return entities;
        }

        // Use Query if provided
        if (request.Query != null) {
            Logger.LogTrace("Searching for {EntityType} with Query", entityTypeName);
            var query = filterService.ApplyWhereClause(context.Set<TDbModel>().AsQueryable(), request.Query);
            var entities = await query.ToListAsync(ct).ConfigureAwait(false);
            Logger.LogTrace("Found {EntityCount} entities for {EntityType} using identifiers", entities.Count, entityTypeName);
            return entities;
        }

        Logger.LogWarning("No Keys or Query provided for {EntityType} search", entityTypeName);
        return [];
    }

    private PatchResult<TResult> PatchEntityProperties<TDbModel, TResult>(TDbModel entity, PatchRequest request)
        where TDbModel : class
    {
        var propertyIssues = PatchRequestPropertyValidator.Validate<TDbModel>(request);
        if (propertyIssues.Count > 0) {
            LogPatchPropertyValidationIssues(typeof(TDbModel).Name, propertyIssues);
            return ResultFactory.PatchFailure<TResult>(
                LyoProblemDetailsBuilder.CreateWithActivity()
                    .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidPatchRequest)
                    .WithMessage("Invalid patch request.")
                    .AddErrors(propertyIssues)
                    .Build());
        }

        var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
        var entityTypeName = typeof(TDbModel).Name;
        var actualUpdatedProperties = new Dictionary<string, object?>();
        var entityType = typeof(TDbModel);
        var propertyCache = GetCachedProperties(entityType).Where(kvp => kvp.Value.CanWrite).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in request.Properties) {
            if (!propertyCache.TryGetValue(kvp.Key, out var property)) {
                Logger.LogWarning("Property {PropertyName} not found or not writable on type {EntityType}", kvp.Key, entityTypeName);
                continue;
            }

            var currentValue = property.GetValue(entity);
            object? newValue;
            if (kvp.Value == null) {
                if (property.PropertyType.IsValueType && !property.PropertyType.IsNullable()) {
                    Logger.LogWarning(
                        "Cannot set null value to non-nullable property {PropertyName} of type {PropertyType} on {EntityType}", kvp.Key, property.PropertyType.Name,
                        entityTypeName);

                    continue;
                }

                newValue = null;
            }
            else {
                try {
                    newValue = kvp.Value.ConvertToType(property.PropertyType);
                }
                catch (Exception ex) {
                    Logger.LogWarning(
                        ex, "Failed to convert value {Value} to type {PropertyType} for property {PropertyName} on {EntityType}", kvp.Value, property.PropertyType.Name, kvp.Key,
                        entityTypeName);

                    continue;
                }
            }

            if (Equals(currentValue, newValue))
                continue;

            Logger.LogTrace("Updating property {PropertyName} on {EntityType} from {OldValue} to {NewValue}", kvp.Key, entityTypeName, currentValue, newValue);
            property.SetValue(entity, newValue);
            actualUpdatedProperties[property.Name] = newValue;
        }

        if (actualUpdatedProperties.Count <= 0)
            return ResultFactory.PatchNoChange<TResult>();

        var updated = MapOrCast<TDbModel, TResult>(Mapper, entity);
        return ResultFactory.PatchSuccess(old, updated, actualUpdatedProperties);
    }

    private async Task<PatchResult<TResult>> PatchInternal<TDbModel, TResult>(
        TDbModel entity,
        PatchRequest request,
        TContext context,
        Action<PatchContext<TDbModel, TContext>>? before,
        Action<PatchContext<TDbModel, TContext>>? after,
        CancellationToken ct = default)
        where TDbModel : class
    {
        var entityTypeName = typeof(TDbModel).Name;
        try {
            var propertyIssues = PatchRequestPropertyValidator.Validate<TDbModel>(request);
            if (propertyIssues.Count > 0) {
                LogPatchPropertyValidationIssues(entityTypeName, propertyIssues);
                return ResultFactory.PatchFailure<TResult>(
                    LyoProblemDetailsBuilder.CreateWithActivity()
                        .WithErrorCode(Models.Constants.ApiErrorCodes.InvalidPatchRequest)
                        .WithMessage("Invalid patch request.")
                        .AddErrors(propertyIssues)
                        .Build());
            }

            var old = MapOrCast<TDbModel, TResult>(Mapper, entity);
            var actualUpdatedProperties = new Dictionary<string, object?>();
            var entityType = typeof(TDbModel);
            var propertyCache = GetCachedProperties(entityType).Where(kvp => kvp.Value.CanWrite).ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
            var processedPropertiesCount = 0;
            var skippedPropertiesCount = 0;
            Logger.LogDebug("Processing {PropertyCount} properties for patch on {EntityType}", request.Properties.Count, entityTypeName);
            foreach (var kvp in request.Properties) {
                if (!propertyCache.TryGetValue(kvp.Key, out var property)) {
                    Logger.LogWarning("Property {PropertyName} not found or not writable on type {EntityType}", kvp.Key, entityTypeName);
                    skippedPropertiesCount++;
                    continue;
                }

                var currentValue = property.GetValue(entity);
                object? newValue;
                if (kvp.Value == null) {
                    if (property.PropertyType.IsValueType && !property.PropertyType.IsNullable()) {
                        Logger.LogWarning(
                            "Cannot set null value to non-nullable property {PropertyName} of type {PropertyType} on {EntityType}", kvp.Key, property.PropertyType.Name,
                            entityTypeName);

                        skippedPropertiesCount++;
                        continue;
                    }

                    newValue = null;
                }
                else {
                    try {
                        newValue = kvp.Value.ConvertToType(property.PropertyType);
                    }
                    catch (Exception ex) {
                        Logger.LogWarning(
                            ex, "Failed to convert value {Value} to type {PropertyType} for property {PropertyName} on {EntityType}", kvp.Value, property.PropertyType.Name,
                            kvp.Key, entityTypeName);

                        skippedPropertiesCount++;
                        continue;
                    }
                }

                if (!Equals(currentValue, newValue)) {
                    Logger.LogTrace("Updating property {PropertyName} on {EntityType} from {OldValue} to {NewValue}", kvp.Key, entityTypeName, currentValue, newValue);
                    property.SetValue(entity, newValue);
                    actualUpdatedProperties[property.Name] = newValue;
                    processedPropertiesCount++;
                }
                else
                    Logger.LogTrace("Property {PropertyName} on {EntityType} already has value {Value}, skipping update", kvp.Key, entityTypeName, currentValue);
            }

            Logger.LogDebug(
                "Patch processing completed for {EntityType}: {ProcessedCount} properties updated, {SkippedCount} properties skipped", entityTypeName, processedPropertiesCount,
                skippedPropertiesCount);

            if (actualUpdatedProperties.Count == 0) {
                Logger.LogInformation("No changes detected for {EntityType}, returning NoChange result", entityTypeName);
                return ResultFactory.PatchNoChange<TResult>();
            }

            Logger.LogTrace(
                "Executing before hook and saving changes for {EntityType} with {UpdatedPropertyCount} updated properties", entityTypeName, actualUpdatedProperties.Count);

            var ctx = new PatchContext<TDbModel, TContext>(request, entity, context, serviceProvider);
            before?.Invoke(ctx);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            after?.Invoke(ctx);
            Logger.LogInformation(
                "Successfully patched {EntityType} with {UpdatedPropertyCount} properties: {PropertyNames}", entityTypeName, actualUpdatedProperties.Count,
                string.Join(", ", actualUpdatedProperties.Keys));

            var result = MapOrCast<TDbModel, TResult>(Mapper, entity);
            return ResultFactory.PatchSuccess(old, result, actualUpdatedProperties);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Unexpected error during internal patch operation for {EntityType}", entityTypeName);
            return ResultFactory.PatchFailure<TResult>(LogAndReturnApiError(ex, "Patch Error"));
        }
    }

    private void LogPatchPropertyValidationIssues(string entityTypeName, IReadOnlyList<ApiError> issues)
    {
        Logger.LogWarning(
            "Patch property validation failed for {EntityType}: {IssueCount} issue(s). {Details}",
            entityTypeName,
            issues.Count,
            string.Join("; ", issues.Select(static e => $"{e.Code}: {e.Description}")));
    }

    private Dictionary<string, PropertyInfo> GetCachedProperties(Type type)
    {
        var cacheKey = $"{PropertyCacheKeyPrefix}{type.FullName}";
        return cache.GetOrSet<Dictionary<string, PropertyInfo>>(
            cacheKey, _ => {
                Logger.LogDebug("Caching property information for type {TypeName}", type.Name);
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
                Logger.LogTrace("Cached {PropertyCount} properties for type {TypeName}", properties.Count, type.Name);
                return properties;
            }, cacheOptions.PropertyInfoExpiration)!;
    }
}