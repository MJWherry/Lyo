using Lyo.FileStorage.Multipart;

namespace Lyo.FileStorage.Models;

/// <summary>Result of <see cref="Staged.IStagedFileUploadService.BeginAsync" />.</summary>
public sealed class StagedUploadBeginResult
{
    /// <summary>Primary key in <see cref="Staged.IStagedFileUploadStore" />.</summary>
    public required Guid StageId { get; init; }

    /// <summary>Client PUT target (S3 presigned URL, Azure SAS, or local API receive URL).</summary>
    public required string PresignedPutUrl { get; init; }

    /// <summary>UTC expiry of <see cref="PresignedPutUrl" />.</summary>
    public required DateTimeOffset UrlExpiresUtc { get; init; }

    /// <summary>Backend-specific staging key or relative path under <c>.stage/{stageId}/object</c>.</summary>
    public required string StorageLocation { get; init; }

    /// <summary>Headers the client must apply verbatim on PUT (SSE, content-type, <c>x-ms-blob-type</c>, etc.).</summary>
    public IReadOnlyDictionary<string, string>? RequiredPutHeaders { get; init; }

    /// <summary>Which backend issued the URL (Local, AwsS3, AzureBlob).</summary>
    public required MultipartUploadProviderKind ProviderKind { get; init; }
}
