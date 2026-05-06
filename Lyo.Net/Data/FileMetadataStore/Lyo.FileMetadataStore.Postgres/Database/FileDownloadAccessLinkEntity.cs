using System.ComponentModel.DataAnnotations;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class FileDownloadAccessLinkEntity
{
    public Guid Id { get; set; }

    public Guid FileId { get; set; }

    [Required]
    public byte[] TokenHash { get; set; } = null!;

    public DateTime CreatedUtc { get; set; }

    public DateTime? NotBeforeUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? WindowStartUtc { get; set; }

    public DateTime? WindowEndUtc { get; set; }

    public int? MaxDownloads { get; set; }

    public int DownloadCount { get; set; }

    public DateTime? LastConsumedUtc { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedUtc { get; set; }

    [MaxLength(256)]
    public string? TenantId { get; set; }
}
