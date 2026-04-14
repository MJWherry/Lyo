namespace Lyo.FileStorage;

/// <summary>Error codes used by FileStorage services.</summary>
public static class FileStorageErrorCodes
{
    /// <summary>Failed to save file.</summary>
    public const string SaveFailed = "FILE_STORAGE_SAVE_FAILED";

    /// <summary>Failed to retrieve file.</summary>
    public const string RetrieveFailed = "FILE_STORAGE_RETRIEVE_FAILED";

    /// <summary>Failed to delete file.</summary>
    public const string DeleteFailed = "FILE_STORAGE_DELETE_FAILED";

    /// <summary>File not found.</summary>
    public const string FileNotFound = "FILE_STORAGE_FILE_NOT_FOUND";

    /// <summary>Operation was cancelled.</summary>
    public const string OperationCancelled = "FILE_STORAGE_OPERATION_CANCELLED";

    /// <summary>Invalid file path.</summary>
    public const string InvalidPath = "FILE_STORAGE_INVALID_PATH";

    /// <summary>Failed to get file metadata.</summary>
    public const string GetMetadataFailed = "FILE_STORAGE_GET_METADATA_FAILED";
}