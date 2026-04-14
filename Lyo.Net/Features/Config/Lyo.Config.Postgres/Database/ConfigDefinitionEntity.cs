using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Config.Postgres.Database;

/// <summary>Entity for storing config definitions in PostgreSQL.</summary>
public sealed class ConfigDefinitionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string ForEntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    public string ForValueType { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    [Column(TypeName = "jsonb")]
    public string? DefaultValueJson { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}