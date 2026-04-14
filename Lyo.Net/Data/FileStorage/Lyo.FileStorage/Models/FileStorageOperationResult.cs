using Lyo.Common;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileStorage.Models;

/// <summary>Result of a file storage operation with file storage-specific properties.</summary>
public sealed record FileStorageOperationResult : Result<FileStoreResult>
{
    /// <summary>The file ID.</summary>
    public Guid? FileId { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private FileStorageOperationResult(bool isSuccess, FileStoreResult? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful FileStorageOperationResult with file store result.</summary>
    public static FileStorageOperationResult FromSuccess(FileStoreResult fileStoreResult, string? message = null)
        => new(true, fileStoreResult) { FileId = fileStoreResult.Id, Message = message };

    /// <summary>Creates a failed FileStorageOperationResult from an exception.</summary>
    public static FileStorageOperationResult FromException(Exception exception, Guid? fileId = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]) { FileId = fileId };
    }

    /// <summary>Creates a failed FileStorageOperationResult with a custom error message.</summary>
    public static FileStorageOperationResult FromError(string errorMessage, string errorCode, Guid? fileId = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]) { FileId = fileId };
    }
}