using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Recipe.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class UnitEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string Abbreviation { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <summary>Type name, id, and name.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Name}";
}