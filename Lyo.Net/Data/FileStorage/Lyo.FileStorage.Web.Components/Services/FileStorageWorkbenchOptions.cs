namespace Lyo.FileStorage.Web.Components.Services;

public sealed class FileStorageWorkbenchOptions
{
    public const string SectionName = "FileStorageWorkbench";

    /// <summary>When true, the host resolves the workbench through HTTP calls to the test API using the shared <see cref="Lyo.Api.Client.IApiClient" />.</summary>
    public bool UseTestApiServices { get; set; } = true;

    /// <summary>When true, the host auto-registers an AWS Secrets Manager keystore + S3 file storage stack from configuration.</summary>
    public bool AutoRegisterS3Services { get; set; }

    /// <summary>The route prefix on the API used for the workbench endpoints.</summary>
    public string ApiRoutePrefix { get; set; } = "Workbench/FileStorage";

    /// <summary>
    /// Relative URI for multipart stream upload to the API (no <see cref="ApiRoutePrefix" />). Default <c>upload/file</c> matches <c>POST /upload/file</c>.
    /// Set to empty to use the legacy path <c>{ApiRoutePrefix}/files/save-stream</c>.
    /// </summary>
    public string? StreamUploadRelativePath { get; set; } = "upload/file";

    /// <summary>The keyed service name used when resolving <see cref="Lyo.FileStorage.IFileStorageService" />.</summary>
    public string? FileStorageServiceKey { get; set; } = "gateway-filestorage";

    /// <summary>The keyed service name used when resolving <see cref="Lyo.Keystore.IKeyStore" />.</summary>
    public string? KeyStoreServiceKey { get; set; } = "gateway-filestorage";

    /// <summary>The keyed metadata store used when auto-registering the S3 file storage stack.</summary>
    public string MetadataStoreKey { get; set; } = "gateway-filestorage-metadata";

    /// <summary>The configuration section used for AWS Secrets Manager keystore settings.</summary>
    public string AwsKeyStoreConfigSection { get; set; } = "AwsKeyStore";

    /// <summary>The configuration section used for S3 file storage settings (binds to <c>S3FileStorageOptions</c>).</summary>
    public string S3FileStorageConfigSection { get; set; } = "S3FileStorageOptions";

    /// <summary>The configuration section used for the metadata store backing file metadata.</summary>
    public string MetadataStoreConfigSection { get; set; } = "PostgresFileMetadataStore";
}
