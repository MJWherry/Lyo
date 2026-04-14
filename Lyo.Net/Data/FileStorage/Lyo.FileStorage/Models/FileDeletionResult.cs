using Lyo.Common;

namespace Lyo.FileStorage.Models;

/// <summary>Result of a file deletion operation with file storage-specific properties.</summary>
public sealed record FileDeletionResult : Result<bool>
{
    /// <summary>The file ID.</summary>
    public Guid? FileId { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private FileDeletionResult(bool isSuccess, bool data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful FileDeletionResult.</summary>
    public static FileDeletionResult FromSuccess(Guid fileId, bool deleted, string? message = null) => new(true, deleted) { FileId = fileId, Message = message };

    /// <summary>Creates a failed FileDeletionResult from an exception.</summary>
    public static FileDeletionResult FromException(Exception exception, Guid? fileId = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, false, [error]) { FileId = fileId };
    }

    /// <summary>Creates a failed FileDeletionResult with a custom error message.</summary>
    public static FileDeletionResult FromError(string errorMessage, string errorCode, Guid? fileId = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, false, [error]) { FileId = fileId };
    }
}