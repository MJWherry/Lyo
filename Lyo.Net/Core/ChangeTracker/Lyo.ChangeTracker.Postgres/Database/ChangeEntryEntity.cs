using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.ChangeTracker.Postgres.Database;

/// <summary>Entity for storing tracked changes in PostgreSQL.</summary>
public sealed class ChangeEntryEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(500)]
    public string ForEntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ForEntityId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FromEntityType { get; set; }

    [MaxLength(500)]
    public string? FromEntityId { get; set; }

    [MaxLength(200)]
    public string? ChangeType { get; set; }

    [MaxLength(4000)]
    public string? Message { get; set; }

    [Column(TypeName = "jsonb")]
    public string OldValuesJson { get; set; } = "{}";

    [Column(TypeName = "jsonb")]
    public string ChangedPropertiesJson { get; set; } = "{}";

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}