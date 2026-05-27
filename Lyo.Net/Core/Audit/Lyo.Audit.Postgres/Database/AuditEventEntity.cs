using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Audit.Postgres.Database;

/// <summary>Entity for storing audit events in PostgreSQL.</summary>
public sealed class AuditEventEntity : EntityRefOptionalFromStringAssociationBase
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the event type or name.</summary>
    [Required]
    [MaxLength(200)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the event occurred.</summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>Gets or sets the optional human-readable message.</summary>
    [MaxLength(4000)]
    public string? Message { get; set; }

    /// <summary>Gets or sets the optional metadata as JSON.</summary>
    [Column(TypeName = "jsonb")]
    [MaxLength(8192)]
    public string? MetadataJson { get; set; }

    /// <summary>Gets or sets the timestamp when this record was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets the timestamp when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
