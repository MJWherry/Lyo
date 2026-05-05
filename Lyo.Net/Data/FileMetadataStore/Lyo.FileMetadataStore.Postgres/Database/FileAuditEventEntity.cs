using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class FileAuditEventEntity
{
    [Key]
    public Guid Id { get; set; }

    [property: JsonPropertyName("event_type")]
    [Required]
    [MaxLength(64)]
    public string EventType { get; set; } = null!;

    [property: JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    public Guid? FileId { get; set; }

    [MaxLength(256)]
    public string? TenantId { get; set; }

    [MaxLength(256)]
    public string? ActorId { get; set; }

    [MaxLength(255)]
    public string? DataEncryptionKeyId { get; set; }

    [MaxLength(255)]
    public string? DataEncryptionKeyVersion { get; set; }

    [property: JsonPropertyName("outcome")]
    [Required]
    [MaxLength(32)]
    public string Outcome { get; set; } = null!;

    [MaxLength(2000)]
    public string? Error { get; set; }

    public Guid? CorrelationId { get; set; }
}
