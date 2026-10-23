using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Recipe.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class RecipeStepEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    public Guid RecipeId { get; set; }

    public int SortOrder { get; set; }

    [Required]
    [MaxLength(8192)]
    public string Instruction { get; set; } = string.Empty;

    public int? DurationMinutes { get; set; }

    [MaxLength(200)]
    public string? GroupLabel { get; set; }

    public RecipeEntity? Recipe { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}