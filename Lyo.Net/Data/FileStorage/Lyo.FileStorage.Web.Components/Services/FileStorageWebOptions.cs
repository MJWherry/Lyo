namespace Lyo.FileStorage.Web.Components.Services;

/// <summary>Browser-side options: API route prefix and stream-upload path. Host DI for storage/keystore is not configured here.</summary>
public sealed class FileStorageWebOptions
{
    /// <summary>Configuration section name (<c>FileStorage</c>).</summary>
    public const string SectionName = "FileStorage";

    /// <summary>API route prefix for file-storage endpoints.</summary>
    public string ApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>
    /// Relative URI for multipart stream upload (no <see cref="ApiRoutePrefix" />). Starts as <c>upload/file</c> to match <c>POST /upload/file</c>. Empty uses
    /// <c>{ApiRoutePrefix}/files/save-stream</c>.
    /// </summary>
    public string? StreamUploadRelativePath { get; set; } = "upload/file";

    /// <summary>Public origin for download and access-link URLs. No trailing slash.</summary>
    public string? PublicBaseUrl { get; set; }
}
