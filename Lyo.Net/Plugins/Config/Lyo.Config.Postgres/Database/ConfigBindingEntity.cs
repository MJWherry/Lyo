using System.ComponentModel.DataAnnotations;

namespace Lyo.Config.Postgres.Database;

/// <summary>
/// Ties a <see cref="ConfigDefinitionEntity" /> to one entity instance. JSON lives on <see cref="ConfigBindingRevisionEntity" />; <see cref="ValueType" />
/// copies <see cref="ConfigDefinitionEntity.ForValueType" /> so queries stay simple.
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
    public string SubjectEntityType { get; set; } = string.Empty;

    /// <summary>Id of the target entity (string — supports composite keys, e.g. Config API app <c>kind:id</c>).</summary>
    [Required]
    [MaxLength(200)]
    public string SubjectEntityId { get; set; } = string.Empty;

    /// <summary>CLR type name for JSON values (same form as <see cref="ConfigDefinitionEntity.ForValueType" />); denormalized of the definition.</summary>
    [Required]
    [MaxLength(1024)]
    public string ValueType { get; set; } = string.Empty;

    /// <summary>Tenant scope. <see langword="null" /> means system / no tenant; non-null indicates a tenant-scoped binding.</summary>
    public Guid? TenantId { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ConfigDefinitionEntity Definition { get; set; } = null!;
}