using System.ComponentModel.DataAnnotations;

namespace Lyo.Config.Postgres.Database;

/// <summary>
/// Links a <see cref="ConfigDefinitionEntity" /> to a concrete entity instance. JSON payloads live in <see cref="ConfigBindingRevisionEntity" />; <see cref="ValueType" />
/// mirrors <see cref="ConfigDefinitionEntity.ForValueType" /> for convenient querying.
/// </summary>
public sealed class ConfigBindingEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid DefinitionId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ForEntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>CLR type name for JSON values (same form as <see cref="ConfigDefinitionEntity.ForValueType" />); denormalized from the definition.</summary>
    [Required]
    [MaxLength(2048)]
    public string ValueType { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ConfigDefinitionEntity Definition { get; set; } = null!;
}