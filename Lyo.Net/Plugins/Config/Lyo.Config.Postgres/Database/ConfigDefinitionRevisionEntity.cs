using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Config.Postgres.Database;

/// <summary>Insert-only snapshots of a definition (key metadata, default, encryption flag).</summary>
public sealed class ConfigDefinitionRevisionEntity
{
    [Required]
    public Guid DefinitionId { get; set; }

    /// <summary>1-based and unique within a definition.</summary>
    public int Revision { get; set; }

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(1024)]
    public string ForValueType { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public bool IsRequired { get; set; }

    public bool IsEncrypted { get; set; }

    [Column(TypeName = "jsonb")]
    [MaxLength(8192)]
    public string? DefaultValueJson { get; set; }

    public byte[]? EncryptedDefaultValue { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public ConfigDefinitionEntity Definition { get; set; } = null!;
}
