using System.Text.Json.Serialization;

namespace Lyo.FileMetadataStore.Postgres.Database;

public sealed class FileAuditEventEntity
{
    public Guid Id { get; set; }

    [property: JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    [property: JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    public Guid? FileId { get; set; }

    public string? TenantId { get; set; }

    public string? ActorId { get; set; }

    public string? DataEncryptionKeyId { get; set; }

    public string? DataEncryptionKeyVersion { get; set; }

    [property: JsonPropertyName("outcome")]
    public string Outcome { get; set; } = null!;

    public string? Error { get; set; }

    public Guid? CorrelationId { get; set; }
}
