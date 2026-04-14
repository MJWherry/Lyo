using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Models.Common.Response;

public static class ResultFactory
{
    public static QueryRes<T> QuerySuccess<T>(
        QueryReq queryRequest,
        IReadOnlyList<T> items,
        int? start,
        int? amount,
        int? total,
        bool? hasMore = null,
        int? queryScore = null)
        => new(queryRequest, true, items, start, amount, total, hasMore, queryScore ?? QueryRequestScorer.Score(queryRequest), null);

    public static QueryRes<T> QueryFailure<T>(QueryReq queryRequest, LyoProblemDetails error, int? queryScore = null)
        => new(queryRequest, false, null, null, null, null, null, queryScore ?? QueryRequestScorer.Score(queryRequest), error);

    public static ProjectedQueryRes<T> ProjectedQuerySuccess<T>(
        ProjectionQueryReq queryRequest,
        IReadOnlyList<T> items,
        int? start,
        int? amount,
        int? total,
        bool? hasMore = null,
        int? queryScore = null,
        IReadOnlyList<string>? entityTypes = null)
        => new(queryRequest, true, items, start, amount, total, hasMore, queryScore ?? QueryRequestScorer.Score(queryRequest), null, entityTypes);

    public static ProjectedQueryRes<T> ProjectedQueryFailure<T>(ProjectionQueryReq queryRequest, LyoProblemDetails error, int? queryScore = null, IReadOnlyList<string>? entityTypes = null)
        => new(queryRequest, false, null, null, null, null, null, queryScore ?? QueryRequestScorer.Score(queryRequest), error, entityTypes);

    // Create
    public static CreateResult<T> CreateSuccess<T>(T data) => new(true, data, null);

    public static CreateResult<T> CreateFailure<T>(LyoProblemDetails error) => new(false, default, error);

    public static CreateBulkResult<T> CreateBulk<T>(IReadOnlyList<CreateResult<T>> results)
    {
        var createdCount = results.Count(r => r.IsSuccess);
        var failedCount = results.Count - createdCount;
        return new(results, createdCount, failedCount);
    }

    // Update
    public static UpdateResult<T> UpdateSuccess<T>(IReadOnlyList<object?> keys, T oldData, T newData, UpdateResultEnum result) => new(result, keys, oldData, newData, null);

    public static UpdateResult<T> Updated<T>(object[] keys, T oldData, T newData) => new(UpdateResultEnum.Updated, keys, oldData, newData, null);

    public static UpdateResult<T> UpdateNoChange<T>(object[] keys, T oldData, T newData) => new(UpdateResultEnum.NoChange, keys, oldData, newData, null);

    public static UpdateResult<T> UpdateFailure<T>(IReadOnlyList<object?> keys, LyoProblemDetails error) => new(UpdateResultEnum.Failed, keys, default, default, error);

    public static UpdateBulkResult<T> UpdateBulk<T>(IReadOnlyList<UpdateResult<T>>? results)
    {
        if (results == null)
            return new(null, 0, 0, 0);

        var updatedCount = results.Count(r => r.Result == UpdateResultEnum.Updated);
        var failedCount = results.Count(r => r.Result == UpdateResultEnum.Failed);
        var noChangeCount = results.Count(r => r.Result == UpdateResultEnum.NoChange);
        return new(results, updatedCount, failedCount, noChangeCount);
    }

    // Patch
    public static PatchResult<T> PatchSuccess<T>(T oldData, T newData, IReadOnlyDictionary<string, object?>? updatedProperties)
        => new(updatedProperties?.Count > 0 ? PatchResultEnum.Updated : PatchResultEnum.NoChange, oldData, newData, updatedProperties, null);

    public static PatchResult<T> PatchNoChange<T>() => new(PatchResultEnum.NoChange, default, default, null, null);

    public static PatchResult<T> PatchFailure<T>(LyoProblemDetails error) => new(PatchResultEnum.Failed, default, default, null, error);

    public static PatchBulkResult<T> PatchBulk<T>(IReadOnlyList<PatchResult<T>>? results)
    {
        if (results == null)
            return new(null, 0, 0, 0);

        var updatedCount = results.Count(r => r.Result == PatchResultEnum.Updated);
        var failedCount = results.Count(r => r.Result == PatchResultEnum.Failed);
        var noChangeCount = results.Count(r => r.Result == PatchResultEnum.NoChange);
        return new(results, updatedCount, failedCount, noChangeCount);
    }

    // Upsert
    public static UpsertResult<T> UpsertCreated<T>(T newData) => new(UpsertResultEnum.Created, default, newData, null);

    public static UpsertResult<T> UpsertCreated<T>(CreateResult<T> createResult)
        => createResult.IsSuccess ? new(UpsertResultEnum.Created, default, createResult.Data, null) : new(UpsertResultEnum.Failed, default, default, createResult.Error);

    public static UpsertResult<T> UpsertUpdated<T>(T oldData, T newData) => new(UpsertResultEnum.Updated, oldData, newData, null);

    public static UpsertResult<T> UpsertNoChange<T>() => new(UpsertResultEnum.NoChange, default, default, null);

    public static UpsertResult<T> UpsertFailure<T>(LyoProblemDetails error) => new(UpsertResultEnum.Failed, default, default, error);

    public static UpsertBulkResult<T> UpsertBulk<T>(IReadOnlyList<UpsertResult<T>>? results)
    {
        if (results == null)
            return new(null, 0, 0, 0, 0);

        var createdCount = results.Count(r => r.Result == UpsertResultEnum.Created);
        var updatedCount = results.Count(r => r.Result == UpsertResultEnum.Updated);
        var noChangeCount = results.Count(r => r.Result == UpsertResultEnum.NoChange);
        var failedCount = results.Count(r => r.Result == UpsertResultEnum.Failed);
        return new(results, createdCount, updatedCount, noChangeCount, failedCount);
    }

    // Delete
    public static DeleteResult<T> DeleteSuccess<T>(T data) => new(true, data, null);

    public static DeleteResult<T> DeleteFailure<T>(LyoProblemDetails error) => new(false, default, error);

    public static DeleteBulkResult<T> DeleteBulk<T>(IReadOnlyList<DeleteResult<T>> results)
    {
        var deletedCount = results.Count(r => r.IsSuccess);
        var failedCount = results.Count - deletedCount;
        return new(results, deletedCount, failedCount);
    }

    // History / QueryHistory
    public static QueryHistoryResults<HistoryResult<T>> QueryHistorySuccess<T>(
        HistoryQuery query,
        IReadOnlyList<HistoryResult<T>> items,
        int total,
        int? start = null,
        int? amount = null)
        => new(query, true, items, start, amount, total, null);

    public static QueryHistoryResults<HistoryResult<T>> QueryHistoryFailure<T>(HistoryQuery query, LyoProblemDetails error) => new(query, false, null, null, null, null, error);

    public static HistoryResult<T> HistorySuccess<T>(T value, DateTime startTimestamp, DateTime endTimestamp) => new(value, startTimestamp, endTimestamp, null);

    public static HistoryResult<T> HistoryError<T>(LyoProblemDetails error) => new(default, null, null, error);
}