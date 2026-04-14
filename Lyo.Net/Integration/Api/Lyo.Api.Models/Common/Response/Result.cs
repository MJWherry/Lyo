using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Api.Models.Error;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Models.Common.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record QueryRes<T>(
    QueryReq QueryRequest,
    bool IsSuccess,
    IReadOnlyList<T>? Items,
    int? Start,
    int? Amount,
    int? Total,
    bool? HasMore,
    int QueryScore,
    LyoProblemDetails? Error)
{
    public override string ToString() => IsSuccess ? $"Start={Start} Amount={Amount} Total: {Total} HasMore: {HasMore} Score: {QueryScore}" : Error?.ToString() ?? "";
}

/// <summary>Result of <c>/QueryProject</c> (projected rows).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ProjectedQueryRes<T>(
    ProjectionQueryReq QueryRequest,
    bool IsSuccess,
    IReadOnlyList<T>? Items,
    int? Start,
    int? Amount,
    int? Total,
    bool? HasMore,
    int QueryScore,
    LyoProblemDetails? Error,
    /// <summary>CLR entity type names touched by this projection (root + navigations from <c>Select</c> paths and computed-field templates).</summary>
    IReadOnlyList<string>? EntityTypes = null)
{
    public ProjectedQueryRes<T> WithEntityTypes(IReadOnlyList<string>? entityTypes) => this with { EntityTypes = entityTypes };

    public override string ToString() => IsSuccess ? $"Start={Start} Amount={Amount} Total: {Total} HasMore: {HasMore} Score: {QueryScore}" : Error?.ToString() ?? "";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record QueryHistoryResults<T>(HistoryQuery Query, bool IsSuccess, IReadOnlyList<T>? Items, int? Start, int? Amount, int? Total, LyoProblemDetails? Error)
{
    public override string ToString() => IsSuccess ? $"Start={Start} Amount={Amount} Total: {Total}" : Error?.ToString() ?? "";
}

[DebuggerDisplay("{ToString(),nq}")]
public record HistoryResult<T>(T? Value, DateTime? StartTimestamp, DateTime? EndTimestamp, LyoProblemDetails? Error)
{
    public override string ToString() => $"{StartTimestamp:g} - {EndTimestamp:g}, {Value!.ToString()}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CreateResult<T>(bool IsSuccess, T? Data, LyoProblemDetails? Error)
{
    public override string ToString() => $"{(IsSuccess ? $"Success, {Data}" : $"Failure, {Error}")}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record CreateBulkResult<T>(IReadOnlyList<CreateResult<T>> Results, int CreatedCount, int FailedCount)
{
    public override string ToString() => $"Created={CreatedCount} Failed={FailedCount} Total={Results.Count}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record UpdateResult<T>(UpdateResultEnum Result, IReadOnlyList<object?>? Keys, T? OldData, T? NewData, LyoProblemDetails? Error)
{
    public override string ToString()
        => $"{typeof(T).Name} Keys={string.Join(",", Keys?.Select(i => i!.ToString()) ?? [])} {(Result != UpdateResultEnum.Failed
            ? nameof(Result)
            : Error?.ToString())}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record UpdateBulkResult<T>(IReadOnlyList<UpdateResult<T>>? Results, int UpdatedCount, int FailedCount, int NoChangeCount)
{
    public override string ToString() => $"{typeof(T).Name} Updated={UpdatedCount} Failed={FailedCount} NoChange={NoChangeCount}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PatchResult<T>(PatchResultEnum Result, T? OldData, T? NewData, IReadOnlyDictionary<string, object?>? UpdatedProperties, LyoProblemDetails? Error)
{
    [JsonIgnore]
    public bool IsSuccess => Result is PatchResultEnum.Updated or PatchResultEnum.NoChange;

    public override string ToString()
        => Result switch {
            PatchResultEnum.NoChange => "No Change",
            PatchResultEnum.Updated => $"Updated {UpdatedProperties?.Count ?? 0} properties: {string.Join(", ", UpdatedProperties?.Keys ?? [])}",
            PatchResultEnum.Failed => $"Failed {Error}",
            var _ => throw new ArgumentOutOfRangeException(nameof(Result))
        };
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record PatchBulkResult<T>(IReadOnlyList<PatchResult<T>>? Results, int UpdatedCount, int FailedCount, int NoChangeCount)
{
    public override string ToString() => $"NoChange={NoChangeCount} Updated={UpdatedCount} Failed={FailedCount}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record UpsertResult<T>(UpsertResultEnum Result, T? OldData, T? NewData, LyoProblemDetails? Error)
{
    public override string ToString()
        => Result switch {
            UpsertResultEnum.NoChange => "No Change",
            UpsertResultEnum.Created => $"Created {NewData}",
            UpsertResultEnum.Updated => $"Updated  OldData={OldData} NewData={NewData}",
            UpsertResultEnum.Failed => $"Failed {Error}",
            var _ => throw new ArgumentOutOfRangeException(nameof(Result))
        };
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record UpsertBulkResult<T>(IReadOnlyList<UpsertResult<T>>? Results, int CreatedCount, int UpdatedCount, int NoChangeCount, int FailedCount)
{
    public override string ToString() => $"NoChange={NoChangeCount} Created={CreatedCount} Updated={UpdatedCount} Failed={FailedCount}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DeleteResult<T>(bool IsSuccess, T? Data, LyoProblemDetails? Error)
{
    public override string ToString() => $"Success={IsSuccess}{(Error is null ? "" : Error.ToString())}";
}

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DeleteBulkResult<T>(IReadOnlyList<DeleteResult<T>> Results, int DeletedCount, int FailedCount)
{
    public override string ToString() => $"Deleted={DeletedCount} Failed={FailedCount}";
}