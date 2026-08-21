namespace Lyo.FileStorage.Web.Components.Services;

/// <summary>Workbench client options: API route prefix and stream-upload path. Host DI for storage/keystore does not belong here.</summary>
public sealed class FileStorageWorkbenchOptions
{
    public const string SectionName = "FileStorageWorkbench";

    /// <summary>The route prefix on the API used for the workbench endpoints.</summary>
    public string ApiRoutePrefix { get; set; } = "Workbench/FileStorage";

    /// <summary>
    /// Relative URI for multipart stream upload to the API (no <see cref="ApiRoutePrefix" />). Default <c>upload/file</c> matches <c>POST /upload/file</c>. Set to empty to use
    /// <c>{ApiRoutePrefix}/files/save-stream</c>.
    /// </summary>
    public string? StreamUploadRelativePath { get; set; } = "upload/file";
}
