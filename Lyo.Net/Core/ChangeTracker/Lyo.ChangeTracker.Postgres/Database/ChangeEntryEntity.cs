using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Lyo.EntityReference.Postgres.Database;

namespace Lyo.ChangeTracker.Postgres.Database;

/// <summary>Entity for storing tracked changes in PostgreSQL.</summary>
public sealed class ChangeEntryEntity : EntityRefOptionalFromStringAssociationBase
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? ChangeType { get; set; }

    [MaxLength(4000)]
    public string? Message { get; set; }

    [Column(TypeName = "jsonb")]
    [MaxLength(32_768)]
    public string OldValuesJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    [MaxLength(32_768)]
    public string ChangedPropertiesJson { get; set; } = "{}";

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}
