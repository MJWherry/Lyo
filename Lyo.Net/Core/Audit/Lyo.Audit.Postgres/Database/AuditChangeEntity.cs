using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Audit.Postgres.Database;

/// <summary>PostgreSQL row that stores one audit change.</summary>
public sealed class AuditChangeEntity : EntityRelationOptionalActorBase
{
    /// <summary>Primary key (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>When the change was written.</summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Previous property values as JSON (name → prior value).</summary>
    [Column(TypeName = "jsonb")]
    [MaxLength(32_768)]
    public string OldValuesJson { get; set; } = "{}";

    /// <summary>Updated property values as JSON (name → new value).</summary>
    [Column(TypeName = "jsonb")]
    [MaxLength(32_768)]
    public string ChangedPropertiesJson { get; set; } = "{}";

    /// <summary>When this row was first inserted.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>When this row was last written, if it has been updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}