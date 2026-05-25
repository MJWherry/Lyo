using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage.Models;

public sealed class DiskFileStorageOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "DiskFileStorage";

    /// <summary>Legacy appsettings subsection; prefer <see cref="SectionName" />.</summary>
    public const string LegacySectionName = "LocalFileStorageService";

    public string RootDirectoryPath { get; set; } = null!;

    /// <summary>When true, <see cref="IFileStorageService.GetPreSignedReadUrlAsync" /> returns a file:// URI for local testing. Do not enable in untrusted environments.</summary>
    public bool AllowFileUriPresignedUrls { get; set; }

    /// <summary>
    /// Absolute base URL for a cooperating HTTP host that implements the plaintext direct-upload PUT route (typically <c>Lyo.TestApi</c> File Storage Workbench). When unset,
    /// <see cref="LocalFileStorageService.BeginDirectUploadAsync" /> behaves like other unsupported backends (<see cref="System.NotSupportedException" />).
    /// </summary>
    public string? DirectUploadReceiveBaseUri { get; set; }

    /// <summary>
    /// Slash-separated route prefix (no leading slash) appended after <see cref="DirectUploadReceiveBaseUri"/> to form <c>PUT …/{{file-id}}/put</c>. Defaults to the Workbench route
    /// group used by Test API (<c>Workbench/FileStorage/direct-upload</c>).
    /// </summary>
    public string DirectUploadPutRouteRelativePath { get; set; } = "Workbench/FileStorage/direct-upload";
}
