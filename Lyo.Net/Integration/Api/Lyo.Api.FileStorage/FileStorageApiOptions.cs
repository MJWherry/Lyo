using Lyo.Exceptions;

namespace Lyo.Api.FileStorage;

/// <summary>Route and keyed-DI names for <see cref="Extensions.BuildFileStorageApi" />.</summary>
public sealed class FileStorageApiOptions
{
    /// <summary>Default MapGroup prefix (<c>FileStorage</c>).</summary>
    public const string DefaultRoute = "FileStorage";

    /// <summary>Default keyed name for <c>IFileStorageService</c> / multipart / staged / archive.</summary>
    public const string DefaultServiceKey = "gateway-filestorage";

    /// <summary>Default FileMetadata QueryProject route.</summary>
    public const string DefaultFileMetadataRoute = "FileStorage/FileMetadata";

    /// <summary>Default root-relative stream-upload path (<c>POST /upload/file</c>).</summary>
    public const string DefaultDirectUploadPath = "upload/file";

    /// <summary>MapGroup prefix (no leading slash).</summary>
    public string Route { get; set; } = DefaultRoute;

    /// <summary>Keyed DI name for storage, multipart, staged upload, and archive.</summary>
    public string ServiceKey { get; set; } = DefaultServiceKey;

    /// <summary>Lyo.Api Query/QueryProject route over <c>file_metadata</c>.</summary>
    public string FileMetadataRoute { get; set; } = DefaultFileMetadataRoute;

    /// <summary>Root-relative multipart stream upload (same contract as <c>{Route}/files/save-stream</c>). Empty skips mapping it.</summary>
    public string DirectUploadPath { get; set; } = DefaultDirectUploadPath;

    /// <summary>Throws when route or service key is blank.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Route);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ServiceKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(FileMetadataRoute);
    }
}
