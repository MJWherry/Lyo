namespace Lyo.FileStorage.Web.Components.Services;

/// <summary>Web client options: API route prefix and stream-upload path. Host DI for storage/keystore does not belong here.</summary>
public sealed class FileStorageWebOptions
{
    /// <summary>Default configuration section name (<c>FileStorage</c>).</summary>
    public const string SectionName = "FileStorage";

    /// <summary>The route prefix on the API used for the file-storage endpoints.</summary>
    public string ApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>
    /// Relative URI for multipart stream upload to the API (no <see cref="ApiRoutePrefix" />). Default <c>upload/file</c> matches <c>POST /upload/file</c>. Set to empty to use
    /// <c>{ApiRoutePrefix}/files/save-stream</c>.
    /// </summary>
    public string? StreamUploadRelativePath { get; set; } = "upload/file";

    /// <summary>Public origin for browser-opened URLs (download, access links). No trailing slash.</summary>
    public string? PublicBaseUrl { get; set; }
}
