using System.Diagnostics;
using Lyo.Result;

namespace Lyo.Images.Models;

/// <summary>Outcome of an image operation that writes to a stream (no payload).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ImageOperationResult : Result<bool>
{
    /// <summary>Operation type that ran.</summary>
    public string? OperationType { get; init; }

    /// <summary>Human-readable result text.</summary>
    public string? Message { get; init; }

    private ImageOperationResult(bool isSuccess, bool data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Successful ImageOperationResult.</summary>
    public static ImageOperationResult FromSuccess(string? operationType = null, string? message = null) => new(true, true) { OperationType = operationType, Message = message };

    /// <summary>Failed ImageOperationResult from an exception.</summary>
    public static ImageOperationResult FromException(Exception exception, string? operationType = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, false, [error]) { OperationType = operationType };
    }

    /// <summary>Failed ImageOperationResult with a custom error message.</summary>
    public static ImageOperationResult FromError(string errorMessage, string errorCode, string? operationType = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, false, [error]) { OperationType = operationType };
    }

    public override string ToString() => IsSuccess ? $"ImageOperationResult: {OperationType ?? "operation"} ok" : $"ImageOperationResult: {OperationType ?? "operation"} failed";
}