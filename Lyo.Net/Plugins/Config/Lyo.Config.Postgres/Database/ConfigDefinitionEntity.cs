using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Config.Postgres.Database;

/// <summary>Postgres row that holds config definitions in PostgreSQL.</summary>
public sealed class ConfigDefinitionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string SubjectEntityType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(1024)]
    public string ForValueType { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>If set, default and binding values are stored encrypted at rest on API hosts.</summary>
    public bool IsEncrypted { get; set; }

    [Column(TypeName = "jsonb")]
    [MaxLength(8192)]
    public string? DefaultValueJson { get; set; }

    /// <summary>
    /// Encrypted bytes for <see cref="DefaultValueJson" /> when <see cref="IsEncrypted" />. Empty array is the Job-style “encrypt this plaintext” marker on write.
    /// </summary>
    public byte[]? EncryptedDefaultValue { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }
}