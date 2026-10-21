using System.ComponentModel.DataAnnotations;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(8192)]
    public string? Description { get; set; }

    public int? Servings { get; set; }

    public int? PrepMinutes { get; set; }

    public int? CookMinutes { get; set; }

    public int Difficulty { get; set; }

    public int Status { get; set; }

    [MaxLength(300)]
    public string? SourceName { get; set; }

    [MaxLength(2000)]
    public string? SourceUrl { get; set; }

    [MaxLength(200)]
    public string? YieldLabel { get; set; }

    public Guid? CategoryId { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public RecipeCategoryEntity? Category { get; set; }

    public List<RecipeIngredientLineEntity> IngredientLines { get; set; } = [];

    public List<RecipeStepEntity> Steps { get; set; } = [];
}