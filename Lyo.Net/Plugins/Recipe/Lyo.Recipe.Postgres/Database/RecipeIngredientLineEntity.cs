using System.ComponentModel.DataAnnotations;

namespace Lyo.Recipe.Postgres.Database;

public sealed class RecipeIngredientLineEntity
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>tenant scope. <see langword="null" /> means system / no tenant.</summary>
    public Guid? TenantId { get; set; }

    public Guid RecipeId { get; set; }

    public Guid? IngredientId { get; set; }

    [MaxLength(500)]
    public string? IngredientText { get; set; }

    public int SortOrder { get; set; }

    public decimal? Quantity { get; set; }

    [MaxLength(80)]
    public string? Unit { get; set; }

    [MaxLength(300)]
    public string? Preparation { get; set; }

    public bool IsOptional { get; set; }

    [MaxLength(200)]
    public string? GroupLabel { get; set; }

    public RecipeEntity? Recipe { get; set; }

    public IngredientEntity? Ingredient { get; set; }
}