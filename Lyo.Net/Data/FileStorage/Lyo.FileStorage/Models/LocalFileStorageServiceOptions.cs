namespace Lyo.FileStorage.Models;

public sealed class LocalFileStorageServiceOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "LocalFileStorageService";

    public string RootDirectoryPath { get; set; } = null!;

    /// <summary>Enable metrics collection for file storage operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>When true, <see cref="IFileStorageService.GetPreSignedReadUrlAsync" /> returns a file:// URI for local testing. Do not enable in untrusted environments.</summary>
    public bool AllowFileUriPresignedUrls { get; set; }
}