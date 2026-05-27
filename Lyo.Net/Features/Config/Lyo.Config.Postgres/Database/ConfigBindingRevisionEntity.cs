using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Config.Postgres.Database;

/// <summary>Append-only value snapshots for a binding. Type is implied by <see cref="ConfigDefinitionEntity.ForValueType" />.</summary>
public sealed class ConfigBindingRevisionEntity
{
    [Required]
    public Guid BindingId { get; set; }

    /// <summary>1-based, unique per binding.</summary>
    public int Revision { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    [MaxLength(8192)]
    public string ValueJson { get; set; } = "null";

    /// <summary>Optional tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped revision.</summary>
    public Guid? TenantId { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public ConfigBindingEntity Binding { get; set; } = null!;
}