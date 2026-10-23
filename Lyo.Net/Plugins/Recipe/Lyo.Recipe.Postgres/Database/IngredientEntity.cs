using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Recipe.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class IngredientEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? DefaultUnit { get; set; }

    [MaxLength(8192)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public List<RecipeIngredientLineEntity> Lines { get; set; } = [];

    /// <summary>Type name, id, and name.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Name}";
}