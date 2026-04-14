namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class MultipartUploadSessionEntity
{
    public Guid SessionId { get; set; }

    public string? TenantId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    public Guid TargetFileId { get; set; }

    public string? PathPrefix { get; set; }

    public bool Compress { get; set; }

    public bool Encrypt { get; set; }

    public string? KeyId { get; set; }

    public string? OriginalFileName { get; set; }

    public string? ContentType { get; set; }

    public int Status { get; set; }

    public int ProviderKind { get; set; }

    public string ProviderState { get; set; } = "{}";

    public long? DeclaredContentLength { get; set; }

    public int PartSizeBytes { get; set; }
}