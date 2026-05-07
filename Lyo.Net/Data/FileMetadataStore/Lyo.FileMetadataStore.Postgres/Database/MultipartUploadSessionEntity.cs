using System.ComponentModel.DataAnnotations;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class MultipartUploadSessionEntity
{
    public Guid SessionId { get; set; }

    [MaxLength(256)]
    public string? TenantId { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime ExpiresUtc { get; set; }

    public Guid TargetFileId { get; set; }

    [MaxLength(500)]
    public string? PathPrefix { get; set; }

    public bool Compress { get; set; }

    public bool Encrypt { get; set; }

    [MaxLength(255)]
    public string? KeyId { get; set; }

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    [MaxLength(255)]
    public string? ContentType { get; set; }

    public int Status { get; set; }

    public int ProviderKind { get; set; }

    [Required]
    [MaxLength(8192)]
    public string ProviderState { get; set; } = "{}";

    public long? DeclaredContentLength { get; set; }

    public int PartSizeBytes { get; set; }
}