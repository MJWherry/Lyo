using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Audit.Postgres.Database;

/// <summary>PostgreSQL row that stores one audit event.</summary>
public sealed class AuditEventEntity : EntityRelationOptionalActorBase
{
    /// <summary>Primary key (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Event type or name.</summary>
    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>When the event took place.</summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>Optional message intended for people to read.</summary>
    [MaxLength(4000)]
    public string? Message { get; set; }

    /// <summary>Optional extra data stored as JSON.</summary>
    [Column(TypeName = "jsonb")]
    [MaxLength(8192)]
    public string? MetadataJson { get; set; }

    /// <summary>When this row was first inserted.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When this row was last written, if it has been updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}