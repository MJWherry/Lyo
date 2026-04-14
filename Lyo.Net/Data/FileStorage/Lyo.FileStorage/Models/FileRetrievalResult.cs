using Lyo.Common;

namespace Lyo.FileStorage.Models;

/// <summary>Result of a file retrieval operation with file storage-specific properties.</summary>
public sealed record FileRetrievalResult : Result<byte[]>
{
    /// <summary>The file ID.</summary>
    public Guid? FileId { get; init; }

    /// <summary>The file size in bytes.</summary>
    public long? FileSize { get; init; }

    /// <summary>Whether the file was compressed.</summary>
    public bool? WasCompressed { get; init; }

    /// <summary>Whether the file was encrypted.</summary>
    public bool? WasEncrypted { get; init; }

    /// <summary>The message describing the result.</summary>
    public string? Message { get; init; }

    private FileRetrievalResult(bool isSuccess, byte[]? data, IReadOnlyList<Error>? errors = null)
        : base(isSuccess, data, errors) { }

    /// <summary>Creates a successful FileRetrievalResult with file data.</summary>
    public static FileRetrievalResult FromSuccess(
        byte[] fileData,
        Guid fileId,
        long? fileSize = null,
        bool? wasCompressed = null,
        bool? wasEncrypted = null,
        string? message = null)
        => new(true, fileData) {
            FileId = fileId,
            FileSize = fileSize ?? fileData.Length,
            WasCompressed = wasCompressed,
            WasEncrypted = wasEncrypted,
            Message = message
        };

    /// <summary>Creates a failed FileRetrievalResult from an exception.</summary>
    public static FileRetrievalResult FromException(Exception exception, Guid? fileId = null, string? errorCode = null)
    {
        var error = Error.FromException(exception, errorCode);
        return new(false, null, [error]) { FileId = fileId };
    }

    /// <summary>Creates a failed FileRetrievalResult with a custom error message.</summary>
    public static FileRetrievalResult FromError(string errorMessage, string errorCode, Guid? fileId = null, Exception? exception = null)
    {
        var error = exception != null ? Error.FromException(exception, errorCode) : new(errorMessage, errorCode);
        return new(false, null, [error]) { FileId = fileId };
    }
}