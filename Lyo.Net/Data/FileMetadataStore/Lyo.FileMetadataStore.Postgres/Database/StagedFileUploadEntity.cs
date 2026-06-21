using Lyo.FileStorage.Multipart;
using Lyo.FileStorage.Staged;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class StagedFileUploadEntity
{
    public Guid StageId { get; set; }

    public string? TenantId { get; set; }

    public Guid? OwnerId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    public StagedUploadStatus Status { get; set; }

    public string StorageLocation { get; set; } = null!;

    public string? PathPrefix { get; set; }

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public long DeclaredMaxSizeBytes { get; set; }

    public long? ObservedSizeBytes { get; set; }

    public byte[]? ContentHash { get; set; }

    public string? HashAlgorithm { get; set; }

    public MultipartUploadProviderKind ProviderKind { get; set; }

    public string ProviderState { get; set; } = "{}";

    public Guid? CommittedFileId { get; set; }

    public string? FailureReason { get; set; }
}
