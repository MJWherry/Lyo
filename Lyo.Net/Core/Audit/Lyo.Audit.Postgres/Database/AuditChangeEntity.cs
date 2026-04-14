using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Audit.Postgres.Database;

/// <summary>Entity for storing audit changes in PostgreSQL.</summary>
public sealed class AuditChangeEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the timestamp when the change was recorded.</summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the assembly-qualified full type name of the audited entity.</summary>
    [Required]
    [MaxLength(500)]
    public string TypeAssemblyFullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the old values as JSON (property name -> old value).</summary>
    [Column(TypeName = "jsonb")]
    public string OldValuesJson { get; set; } = "{}";

    /// <summary>Gets or sets the changed properties as JSON (property name -> new value).</summary>
    [Column(TypeName = "jsonb")]
    public string ChangedPropertiesJson { get; set; } = "{}";

    /// <summary>Gets or sets the timestamp when this record was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets the timestamp when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}