using Lyo.Common;

namespace Lyo.Images.Models;

/// <summary>Result of an image operation that writes to a stream (no return value).</summary>
public sealed record ImageOperationResult : Result<bool>
{
    /// <summary>The operation type performed.</summary>
    public string? OperationType { get; init; }

    /// <summary>A human-readable message describing the result.</summary>
    public string? Message { get; init; }

    private ImageOperationResult(bool isSuccess, bool data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful ImageOperationResult.</summary>
    public static ImageOperationResult FromSuccess(string? operationType = null, string? message = null) => new(true, true) { OperationType = operationType, Message = message };

    /// <summary>Creates a failed ImageOperationResult from an exception.</summary>
    public static ImageOperationResult FromException(Exception exception, string? operationType = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, false, [error]) { OperationType = operationType };
    }

    /// <summary>Creates a failed ImageOperationResult with a custom error message.</summary>
    public static ImageOperationResult FromError(string errorMessage, string errorCode, string? operationType = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, false, [error]) { OperationType = operationType };
    }
}