using System.ComponentModel.DataAnnotations;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeCategoryEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null"/> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    public Guid? ParentId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public RecipeCategoryEntity? Parent { get; set; }

    public List<RecipeCategoryEntity> Children { get; set; } = [];

    public List<RecipeEntity> Recipes { get; set; } = [];
}
