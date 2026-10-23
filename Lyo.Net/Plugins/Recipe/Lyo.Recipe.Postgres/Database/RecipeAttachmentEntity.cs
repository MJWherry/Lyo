using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Lyo.Recipe.Postgres.Database;

/// <summary>FileStorage file linked to a recipe subject.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RecipeAttachmentEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(80)]
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public Guid FileId { get; set; }

    public int Role { get; set; }

    [MaxLength(500)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}